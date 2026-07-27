using GestionCelulares.Application.Common;
using GestionCelulares.Application.Taller;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class TallerServiceTests
{
    private const int Orden = 1, Variante = 1;

    // Orden editable + una pieza (accesorio) con 5 en stock.
    private static async Task<(GestionCelularesContext db, TallerService svc)> SembrarAsync(int stock = 5)
    {
        var db = TestDb.Crear();
        db.Sucursales.Add(new Sucursal { SucursalId = 1, Nombre = "Principal" });
        var p = new Producto { ProductoId = 1, Nombre = "Pantalla X", Serializado = false, Activo = true };
        p.Variantes.Add(new ProductoVariante { VarianteId = Variante, ProductoId = 1, PrecioVenta = 1500m, PrecioCosto = 900m, StockNoSerial = stock, Activo = true });
        db.Productos.Add(p);
        db.OrdenesTaller.Add(new OrdenTaller { OrdenTallerId = Orden, SucursalId = 1, Estado = "Recibido", FechaRecepcion = DateTime.Now });
        await db.SaveChangesAsync();
        return (db, new TallerService(db));
    }

    private static int StockDe(GestionCelularesContext db) =>
        db.ProductoVariantes.First(v => v.VarianteId == Variante).StockNoSerial;

    [Fact]
    public async Task Agregar_repuesto_del_inventario_descuenta_stock()
    {
        var (db, svc) = await SembrarAsync(stock: 5);

        await svc.AgregarRepuestoAsync(Orden, new RepuestoAgregarDto { VarianteId = Variante, Descripcion = "Pantalla", Cantidad = 2, Costo = 900m });

        Assert.Equal(3, StockDe(db));   // 5 - 2
    }

    [Fact]
    public async Task Agregar_repuesto_sin_stock_suficiente_falla_y_no_descuenta()
    {
        var (db, svc) = await SembrarAsync(stock: 1);

        await Assert.ThrowsAsync<TallerException>(() =>
            svc.AgregarRepuestoAsync(Orden, new RepuestoAgregarDto { VarianteId = Variante, Descripcion = "Pantalla", Cantidad = 3, Costo = 900m }));

        Assert.Equal(1, StockDe(db));   // intacto
    }

    [Fact]
    public async Task Quitar_repuesto_del_inventario_repone_stock()
    {
        var (db, svc) = await SembrarAsync(stock: 5);
        var orden = await svc.AgregarRepuestoAsync(Orden, new RepuestoAgregarDto { VarianteId = Variante, Descripcion = "Pantalla", Cantidad = 2, Costo = 900m });
        Assert.Equal(3, StockDe(db));
        var repuestoId = orden.Repuestos.Single().Id;

        await svc.EliminarRepuestoAsync(Orden, repuestoId);

        Assert.Equal(5, StockDe(db));   // repuesto: 3 + 2
    }

    [Fact]
    public async Task Repuesto_manual_sin_variante_no_toca_el_stock()
    {
        var (db, svc) = await SembrarAsync(stock: 5);

        await svc.AgregarRepuestoAsync(Orden, new RepuestoAgregarDto { VarianteId = null, Descripcion = "Mano de obra", Cantidad = 1, Costo = 500m });

        Assert.Equal(5, StockDe(db));   // sin variante = no descuenta
    }

    [Fact]
    public async Task No_permite_mas_de_tres_fotos()
    {
        var (_, svc) = await SembrarAsync();
        for (var i = 0; i < 3; i++)
            await svc.AgregarFotoAsync(Orden, new FotoAgregarDto { Url = $"https://x/{i}.jpg" });

        await Assert.ThrowsAsync<TallerException>(() =>
            svc.AgregarFotoAsync(Orden, new FotoAgregarDto { Url = "https://x/4.jpg" }));
    }
}
