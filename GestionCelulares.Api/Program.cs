using System.Text;
using System.Threading.RateLimiting;
using GestionCelulares.Api;
using GestionCelulares.Api.Services;
using GestionCelulares.Application;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Infrastructure;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Infrastructure.Settings;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
     .AllowAnyMethod()));

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
    await SeedDevPasswordAsync(app);
}
else
{
    // En desarrollo no redirigimos a HTTPS: el front Angular llama por HTTP
    // y el redirect 307 rompería el preflight CORS.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Siembra de desarrollo: asigna una contraseña real al admin si quedó el placeholder ---
static async Task SeedDevPasswordAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<GestionCelularesContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == "admin");
        if (admin is not null && admin.HashContrasena == "CAMBIAR_HASH")
        {
            admin.HashContrasena = hasher.Hash("Admin123*");
            await db.SaveChangesAsync();
            logger.LogWarning("Contraseña del usuario 'admin' inicializada a 'Admin123*' (solo desarrollo). Cámbiala.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo ejecutar la siembra de desarrollo (¿BD accesible?).");
    }
}
