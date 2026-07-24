using GestionCelulares.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GestionCelulares.Api;

/// <summary>Readiness: verifica que la API pueda conectarse a la base de datos.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly GestionCelularesContext _db;

    public DatabaseHealthCheck(GestionCelularesContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Base de datos accesible.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error consultando la base de datos.", ex);
        }
    }
}
