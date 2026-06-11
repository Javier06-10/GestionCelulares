using System.Text;
using GestionCelulares.Application;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Infrastructure;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Capas de la aplicación (Clean Architecture) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()));

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

var app = builder.Build();

// Swagger siempre disponible (API de uso local; el frontend Angular y las pruebas lo usan)
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    await SeedDevPasswordAsync(app);
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
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
