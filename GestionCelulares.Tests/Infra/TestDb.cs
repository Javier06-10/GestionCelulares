using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Creditos;
using Microsoft.EntityFrameworkCore;
using GestionCelulares.Infrastructure.Persistence;

namespace GestionCelulares.Tests.Infra;

/// <summary>Helpers para montar el contexto en memoria y stubs de los procedimientos almacenados.</summary>
public static class TestDb
{
    /// <summary>Crea un contexto EF Core InMemory con una BD aislada por test.</summary>
    public static GestionCelularesContext Crear()
    {
        var options = new DbContextOptionsBuilder<GestionCelularesContext>()
            .UseInMemoryDatabase($"gc-test-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;
        return new GestionCelularesContext(options);
    }
}

/// <summary>Stub de los SP de caja: solo se invoca en CerrarAsync (no usado en estas pruebas).</summary>
public sealed class CajaProceduresStub : ICajaProcedures
{
    public Func<int, int, decimal, CierreCajaResultadoDto>? AlCerrar { get; set; }

    public Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, int usuarioCierre, decimal montoContado)
        => Task.FromResult(AlCerrar?.Invoke(sesionCajaId, usuarioCierre, montoContado)
            ?? new CierreCajaResultadoDto());
}

/// <summary>Stub de los SP de crédito: permite simular la creación sin SQL Server.</summary>
public sealed class CreditoProceduresStub : ICreditoProcedures
{
    public int CreadoId { get; set; } = 1;
    public int VecesCreado { get; private set; }

    public Task<int> CrearAsync(CreditoCrearDto dto)
    {
        VecesCreado++;
        return Task.FromResult(CreadoId);
    }

    public Task<int> RegistrarAbonoAsync(int creditoId, AbonoRegistroDto dto, int? usuarioId)
        => Task.FromResult(1);

    public Task<MoraResultadoDto> ActualizarMoraAsync(DateTime? fechaCorte)
        => Task.FromResult(new MoraResultadoDto());
}
