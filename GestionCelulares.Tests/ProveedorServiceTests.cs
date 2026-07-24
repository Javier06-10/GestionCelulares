using GestionCelulares.Application.Common;
using GestionCelulares.Application.Proveedores;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class ProveedorServiceTests
{
    private const int Sucursal = 1;

    private static async Task<(int proveedorId, GestionCelulares.Infrastructure.Persistence.GestionCelularesContext db)> SembrarAsync()
    {
        var db = TestDb.Crear();
        db.Sucursales.Add(new Sucursal { SucursalId = Sucursal, Nombre = "Principal" });
        var prov = new Proveedor { Nombre = "Distribuidora X", Balance = 0, Activo = true, FechaCreacion = DateTime.Now };
        db.Proveedores.Add(prov);
        await db.SaveChangesAsync();
        return (prov.ProveedorId, db);
    }

    [Fact]
    public async Task Compra_al_contado_no_afecta_el_balance_y_registra_pago()
    {
        var (id, db) = await SembrarAsync();
        var svc = new ProveedorService(db);

        await svc.RegistrarCompraAsync(id, new CompraRegistroDto
        {
            SucursalId = Sucursal, Total = 1500m, Contado = true
        });

        var prov = await db.Proveedores.FirstAsync(p => p.ProveedorId == id);
        Assert.Equal(0m, prov.Balance);                                   // contado: no queda deuda
        var pago = await db.PagosProveedor.SingleAsync(p => p.ProveedorId == id);
        Assert.Equal(1500m, pago.Monto);                                  // se deja rastro del pago
        Assert.NotNull(pago.CompraId);                                    // vinculado a la compra
    }

    [Fact]
    public async Task Compra_a_credito_aumenta_el_balance_adeudado()
    {
        var (id, db) = await SembrarAsync();
        var svc = new ProveedorService(db);

        await svc.RegistrarCompraAsync(id, new CompraRegistroDto
        {
            SucursalId = Sucursal, Total = 2000m, Contado = false
        });

        var prov = await db.Proveedores.FirstAsync(p => p.ProveedorId == id);
        Assert.Equal(2000m, prov.Balance);
        Assert.False(await db.PagosProveedor.AnyAsync(p => p.ProveedorId == id));
    }

    [Fact]
    public async Task ComprasAsync_marca_contado_segun_exista_pago()
    {
        var (id, db) = await SembrarAsync();
        var svc = new ProveedorService(db);
        await svc.RegistrarCompraAsync(id, new CompraRegistroDto { SucursalId = Sucursal, Total = 500m, Contado = true });
        await svc.RegistrarCompraAsync(id, new CompraRegistroDto { SucursalId = Sucursal, Total = 700m, Contado = false });

        var compras = await svc.ComprasAsync(id);

        Assert.Equal(2, compras.Count);
        Assert.True(compras.Single(c => c.Total == 500m).Contado);
        Assert.False(compras.Single(c => c.Total == 700m).Contado);
    }

    [Fact]
    public async Task Compra_a_credito_fija_vencimiento_segun_dias_del_proveedor()
    {
        var db = TestDb.Crear();
        db.Sucursales.Add(new Sucursal { SucursalId = Sucursal, Nombre = "Principal" });
        var prov = new Proveedor { Nombre = "Mayorista", Activo = true, FechaCreacion = DateTime.Now, CondicionPago = "Credito", DiasCredito = 30 };
        db.Proveedores.Add(prov);
        await db.SaveChangesAsync();
        var svc = new ProveedorService(db);

        await svc.RegistrarCompraAsync(prov.ProveedorId, new CompraRegistroDto { SucursalId = Sucursal, Total = 3000m, Contado = false });
        await svc.RegistrarCompraAsync(prov.ProveedorId, new CompraRegistroDto { SucursalId = Sucursal, Total = 500m, Contado = true });

        var credito = await db.Compras.FirstAsync(c => c.Total == 3000m);
        var contado = await db.Compras.FirstAsync(c => c.Total == 500m);
        Assert.Equal(credito.Fecha.Date.AddDays(30), credito.FechaVencimiento);   // crédito: vence a 30 días
        Assert.Null(contado.FechaVencimiento);                                     // contado: sin vencimiento
    }

    [Fact]
    public async Task Pago_reduce_el_balance()
    {
        var (id, db) = await SembrarAsync();
        var svc = new ProveedorService(db);
        await svc.RegistrarCompraAsync(id, new CompraRegistroDto { SucursalId = Sucursal, Total = 1000m, Contado = false });

        var res = await svc.RegistrarPagoAsync(id, new PagoProveedorRegistroDto { Monto = 400m });

        Assert.Equal(600m, res.BalanceProveedor);
        var prov = await db.Proveedores.FirstAsync(p => p.ProveedorId == id);
        Assert.Equal(600m, prov.Balance);
    }

    [Fact]
    public async Task Pago_mayor_al_balance_lanza_excepcion()
    {
        var (id, db) = await SembrarAsync();
        var svc = new ProveedorService(db);
        await svc.RegistrarCompraAsync(id, new CompraRegistroDto { SucursalId = Sucursal, Total = 1000m, Contado = false });

        await Assert.ThrowsAsync<ProveedorException>(() =>
            svc.RegistrarPagoAsync(id, new PagoProveedorRegistroDto { Monto = 1500m }));
    }
}
