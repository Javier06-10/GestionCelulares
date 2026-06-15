using GestionCelulares.Application.Common;
using GestionCelulares.Application.Creditos;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Tests.Infra;
using GestionCelulares.Infrastructure.Persistence;
using Xunit;

namespace GestionCelulares.Tests;

public class CreditoServiceTests
{
    private static Cliente Cliente(bool bloqueado) => new()
    {
        ClienteId = 10, Nombre = "Cliente Demo", Bloqueado = bloqueado
    };

    private static CreditoCrearDto DtoValido() => new()
    {
        ClienteId = 10,
        MontoFinanciado = 12000m,
        NumeroCuotas = 6,
        FechaPrimerVencimiento = DateTime.Today.AddDays(30),
        TasaInteresMensual = 5m
    };

    [Fact]
    public async Task Cliente_bloqueado_no_puede_recibir_financiamiento()
    {
        var db = TestDb.Crear();
        db.Clientes.Add(Cliente(bloqueado: true));
        await db.SaveChangesAsync();
        var procs = new CreditoProceduresStub();
        var svc = new CreditoService(db, procs);

        await Assert.ThrowsAsync<CreditoException>(() => svc.CrearAsync(DtoValido()));
        Assert.Equal(0, procs.VecesCreado);   // ni siquiera se llama al SP
    }

    [Fact]
    public async Task Primer_vencimiento_en_el_pasado_es_rechazado()
    {
        var db = TestDb.Crear();
        db.Clientes.Add(Cliente(bloqueado: false));
        await db.SaveChangesAsync();
        var svc = new CreditoService(db, new CreditoProceduresStub());

        var dto = DtoValido();
        dto.FechaPrimerVencimiento = DateTime.Today.AddDays(-1);

        await Assert.ThrowsAsync<CreditoException>(() => svc.CrearAsync(dto));
    }

    [Fact]
    public async Task Venta_que_no_es_a_credito_es_rechazada()
    {
        var db = TestDb.Crear();
        db.Clientes.Add(Cliente(bloqueado: false));
        db.Ventas.Add(new Venta { VentaId = 99, EsCredito = false });
        await db.SaveChangesAsync();
        var svc = new CreditoService(db, new CreditoProceduresStub());

        var dto = DtoValido();
        dto.VentaId = 99;

        await Assert.ThrowsAsync<CreditoException>(() => svc.CrearAsync(dto));
    }

    [Fact]
    public async Task Venta_con_credito_existente_es_rechazada()
    {
        var db = TestDb.Crear();
        db.Clientes.Add(Cliente(bloqueado: false));
        db.Ventas.Add(new Venta { VentaId = 99, EsCredito = true });
        db.Creditos.Add(new Credito { CreditoId = 1, VentaId = 99, ClienteId = 10, FechaInicio = DateTime.Now });
        await db.SaveChangesAsync();
        var svc = new CreditoService(db, new CreditoProceduresStub());

        var dto = DtoValido();
        dto.VentaId = 99;

        await Assert.ThrowsAsync<CreditoException>(() => svc.CrearAsync(dto));
    }

    [Fact]
    public async Task Credito_valido_invoca_el_SP_y_devuelve_el_credito_creado()
    {
        var db = TestDb.Crear();
        db.Clientes.Add(Cliente(bloqueado: false));
        // El SP está simulado: sembramos el crédito que "crearía" para que PorIdAsync lo devuelva.
        db.Creditos.Add(new Credito
        {
            CreditoId = 55, ClienteId = 10, MontoFinanciado = 12000m, NumeroCuotas = 6,
            MontoTotal = 13800m, Saldo = 13800m, Estado = "Activo", FechaInicio = DateTime.Now
        });
        await db.SaveChangesAsync();
        var procs = new CreditoProceduresStub { CreadoId = 55 };
        var svc = new CreditoService(db, procs);

        var creado = await svc.CrearAsync(DtoValido());

        Assert.Equal(1, procs.VecesCreado);
        Assert.Equal(55, creado.CreditoId);
        Assert.Equal("Cliente Demo", creado.Cliente);
    }
}
