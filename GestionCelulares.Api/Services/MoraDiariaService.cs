using GestionCelulares.Application.Common.Interfaces;

namespace GestionCelulares.Api.Services;

/// <summary>
/// Job en segundo plano que ejecuta el proceso de mora (usp_Creditos_ActualizarMora)
/// una vez al día a la hora configurada (Mora:HoraDiaria, por defecto 01:00).
/// Hace además una corrida de "puesta al día" poco después de arrancar la API.
/// El procedimiento es idempotente, así que ejecutarlo de más es seguro.
/// </summary>
public class MoraDiariaService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MoraDiariaService> _logger;
    private readonly TimeOnly _hora;
    private readonly bool _habilitado;

    public MoraDiariaService(IServiceScopeFactory scopeFactory, ILogger<MoraDiariaService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _habilitado = config.GetValue("Mora:Habilitado", true);
        _hora = TimeOnly.TryParse(config["Mora:HoraDiaria"], out var h) ? h : new TimeOnly(1, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado)
        {
            _logger.LogInformation("Job de mora diaria deshabilitado (Mora:Habilitado = false).");
            return;
        }

        // Puesta al día tras el arranque (por si la API estuvo apagada a la hora programada)
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (TaskCanceledException) { return; }
        await EjecutarAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = ProximaEjecucion() - DateTime.Now;
            if (espera < TimeSpan.Zero) espera = TimeSpan.Zero;
            _logger.LogInformation("Próxima corrida de mora en {Horas:0.0} h.", espera.TotalHours);

            try { await Task.Delay(espera, stoppingToken); }
            catch (TaskCanceledException) { break; }

            if (!stoppingToken.IsCancellationRequested)
                await EjecutarAsync(stoppingToken);
        }
    }

    private DateTime ProximaEjecucion()
    {
        var ahora = DateTime.Now;
        var hoy = ahora.Date.Add(_hora.ToTimeSpan());
        return ahora < hoy ? hoy : hoy.AddDays(1);
    }

    private async Task EjecutarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var procedures = scope.ServiceProvider.GetRequiredService<ICreditoProcedures>();
            var r = await procedures.ActualizarMoraAsync(null);
            _logger.LogInformation(
                "Mora diaria ejecutada ({Fecha:yyyy-MM-dd}): {Cuotas} cuotas vencidas, {EnMora} créditos en mora, {Reactivados} reactivados, {Clientes} clientes actualizados.",
                r.FechaCorte, r.CuotasMarcadasVencidas, r.CreditosPuestosEnMora, r.CreditosReactivados, r.ClientesMorososActualizados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el proceso de mora diaria.");
        }
    }
}
