using System.Text;
using System.Threading.RateLimiting;
using GestionCelulares.Api;
using GestionCelulares.Api.Services;
using GestionCelulares.Application;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Logging estructurado (Serilog): consola + archivo rotativo, configurable
//     por la sección "Serilog" de appsettings.{Entorno}.json ---
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- Capas de la aplicación (Clean Architecture) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Job diario de mora (actualiza cuotas vencidas y clientes morosos) ---
builder.Services.AddHostedService<MoraDiariaService>();

// --- Autenticación JWT (composición en el host web) ---
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32 || jwt.Key.Contains("CAMBIA"))
    throw new InvalidOperationException(
        "Jwt:Key inválida: configura una clave secreta de al menos 32 caracteres " +
        "(appsettings.{Entorno}.json, user-secrets o variable de entorno) antes de iniciar la API.");
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        // El access token viaja en una cookie HttpOnly (no accesible desde JS): se lee de ahí.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("gc_access", out var cookie) && !string.IsNullOrEmpty(cookie))
                    ctx.Token = cookie;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// --- CORS para el frontend Angular ---
const string CorsPolicy = "Frontend";
// Orígenes permitidos configurables por entorno (Cors:Origins). Por defecto, el dev de Angular.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
    p.WithOrigins(corsOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));   // las cookies de auth requieren credenciales (orígenes específicos)

// --- Rate limiting: frena la fuerza bruta contra el login (por IP) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// --- Health checks: liveness (proceso) y readiness (base de datos) ---
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

// --- Controllers + Swagger con soporte JWT ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gestión Celulares API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT (sin el prefijo 'Bearer ')."
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// --- Manejo global de excepciones (red de seguridad + contrato uniforme) ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Debe ir primero para envolver todo el pipeline.
app.UseExceptionHandler();

// Una línea estructurada por request: método, ruta, estado y tiempo.
app.UseSerilogRequestLogging();

// --- Cabeceras de seguridad en las respuestas de la API (seguras para Swagger/JSON) ---
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // En desarrollo no redirigimos a HTTPS: el front Angular llama por HTTP
    // y el redirect 307 rompería el preflight CORS.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Siembra del admin: en cualquier entorno. En producción usa Seed:AdminPassword;
// en desarrollo cae a una clave por defecto para no frenar el trabajo local.
await SeedAdminAsync(app);

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Endpoints de salud (anónimos, para monitoreo/orquestador) ---
// /health/live  : el proceso responde (sin tocar dependencias).
// /health/ready : la base de datos es accesible (readiness).
// /health       : resumen JSON de todos los checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = EscribirSaludJson });

app.Run();

// Respuesta JSON compacta con el estado global y el detalle por check.
static Task EscribirSaludJson(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    return ctx.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        durationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    });
}

// --- Siembra del admin: deja un usuario 'admin' con contraseña utilizable ---
// Producción: la contraseña sale de Seed:AdminPassword (variable de entorno
// Seed__AdminPassword). Sin esa clave, en producción no se toca nada.
// Desarrollo: si no se define, cae a 'Admin123*' para no frenar el trabajo local.
// Crea el admin si no existe (requiere el rol 'Admin', que trae el bootstrap de datos)
// o le fija la contraseña si quedó con el placeholder 'CAMBIAR_HASH'.
static async Task SeedAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<GestionCelularesContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var seedPassword = config["Seed:AdminPassword"];
        var esDesarrollo = app.Environment.IsDevelopment();
        var password = string.IsNullOrWhiteSpace(seedPassword) && esDesarrollo ? "Admin123*" : seedPassword;
        var origen = string.IsNullOrWhiteSpace(seedPassword) && esDesarrollo ? "desarrollo (Admin123*)" : "Seed:AdminPassword";

        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == "admin");

        if (admin is null)
        {
            // Sin contraseña definida no hay nada que crear (producción sin Seed:AdminPassword).
            if (string.IsNullOrWhiteSpace(password)) return;

            var rolAdminId = await db.Roles.Where(r => r.Nombre == "Admin")
                .Select(r => (int?)r.RolId).FirstOrDefaultAsync();
            if (rolAdminId is null)
            {
                logger.LogWarning("No existe el rol 'Admin'; no se puede crear el usuario admin. Ejecuta el bootstrap de datos iniciales.");
                return;
            }
            var sucursalId = await db.Sucursales.OrderBy(s => s.SucursalId)
                .Select(s => (int?)s.SucursalId).FirstOrDefaultAsync();

            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = "admin",
                NombreCompleto = "Administrador",
                HashContrasena = hasher.Hash(password),
                RolId = rolAdminId.Value,
                SucursalId = sucursalId,
                Activo = true,
                FechaCreacion = DateTime.Now
            });
            await db.SaveChangesAsync();
            logger.LogWarning("Usuario 'admin' creado con la contraseña de {Origen}. Cámbiala tras el primer acceso.", origen);
            return;
        }

        // El admin existe con el hash placeholder: fíjale la contraseña si la tenemos.
        if (admin.HashContrasena == "CAMBIAR_HASH" && !string.IsNullOrWhiteSpace(password))
        {
            admin.HashContrasena = hasher.Hash(password);
            if (!admin.Activo) admin.Activo = true;
            await db.SaveChangesAsync();
            logger.LogWarning("Contraseña del usuario 'admin' inicializada desde {Origen}. Cámbiala.", origen);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo ejecutar la siembra del admin (¿BD accesible?).");
    }
}
