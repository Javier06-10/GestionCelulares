using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Common;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class CajaServiceTests
{
    private const int Sucursal = 1;
    private const int Usuario = 7;

    private static async Task<(int sesionId, GestionCelulares.Infrastructure.Persistence.GestionCelularesContext db)> SesionAbiertaAsync()
    {
        var db = TestDb.Crear();
        db.Sucursales.Add(new Sucursal { SucursalId = Sucursal, Nombre = "Principal" });
        var sesion = new SesionCaja
        {
            SucursalId = Sucursal, UsuarioApertura = Usuario, MontoApertura = 1000m,
            Estado = "Abierta", FechaApertura = DateTime.Now
        };
        db.SesionesCaja.Add(sesion);
        await db.SaveChangesAsync();
        return (sesion.SesionCajaId, db);
    }

    [Fact]
    public async Task Abrir_con_sesion_ya_abierta_lanza_excepcion()
    {
        var (_, db) = await SesionAbiertaAsync();
        var svc = new CajaService(db, new CajaProceduresStub());

        await Assert.ThrowsAsync<CajaException>(() =>
            svc.AbrirAsync(new AperturaCajaDto { SucursalId = Sucursal, MontoApertura = 500m }, Usuario));
    }

    [Fact]
    public async Task Movimiento_tipo_invalido_lanza_excepcion()
    {
        var (sesionId, db) = await SesionAbiertaAsync();
        var svc = new CajaService(db, new CajaProceduresStub());

        await Assert.ThrowsAsync<CajaException>(() =>
            svc.RegistrarMovimientoAsync(sesionId, new MovimientoCajaRegistroDto
            {
                Tipo = "Retiro", Concepto = "x", Monto = 100m
            }));
    }

    [Fact]
    public async Task Movimiento_en_sesion_cerrada_lanza_excepcion()
    {
        var (sesionId, db) = await SesionAbiertaAsync();
        var sesion = await db.SesionesCaja.FirstAsync(s => s.SesionCajaId == sesionId);
        sesion.Estado = "Cerrada";
        await db.SaveChangesAsync();
        var svc = new CajaService(db, new CajaProceduresStub());

        await Assert.ThrowsAsync<CajaException>(() =>
            svc.RegistrarMovimientoAsync(sesionId, new MovimientoCajaRegistroDto
            {
                Tipo = "Ingreso", Concepto = "x", Monto = 100m
            }));
    }

    [Fact]
    public async Task Totales_de_sesion_suman_ingresos_y_egresos()
    {
        var (sesionId, db) = await SesionAbiertaAsync();
        var svc = new CajaService(db, new CajaProceduresStub());

        await svc.RegistrarMovimientoAsync(sesionId, new MovimientoCajaRegistroDto { Tipo = "Ingreso", Concepto = "Venta", Monto = 300m });
        await svc.RegistrarMovimientoAsync(sesionId, new MovimientoCajaRegistroDto { Tipo = "Ingreso", Concepto = "Abono", Monto = 200m });
        await svc.RegistrarMovimientoAsync(sesionId, new MovimientoCajaRegistroDto { Tipo = "Egreso", Concepto = "Compra", Monto = 120m });

        var sesion = await svc.PorIdAsync(sesionId);

        Assert.NotNull(sesion);
        Assert.Equal(500m, sesion!.TotalIngresos);
        Assert.Equal(120m, sesion.TotalEgresos);
    }
}
