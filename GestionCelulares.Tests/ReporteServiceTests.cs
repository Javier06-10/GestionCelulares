using GestionCelulares.Application.Reportes;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Xunit;

namespace GestionCelulares.Tests;

public class ReporteServiceTests
{
    private static readonly DateTime Dia = new(2026, 3, 15);

    /// <summary>
    /// Un producto provisional (venta rápida) con costo 0 NO puede contar su precio
    /// completo como ganancia: su línea se excluye del total de ganancia y se divulga aparte.
    /// </summary>
    [Fact]
    public async Task Ventas_excluye_de_la_ganancia_las_lineas_provisionales_sin_costo()
    {
        var db = TestDb.Crear();

        // Producto normal: costo 60, precio 100 -> margen 40
        var normal = new Producto { ProductoId = 1, Nombre = "Cargador", Activo = true, Provisional = false };
        normal.Variantes.Add(new ProductoVariante { VarianteId = 1, ProductoId = 1, PrecioVenta = 100m, PrecioCosto = 60m, Activo = true });

        // Producto provisional sin costo: precio 50, costo 0
        var prov = new Producto { ProductoId = 2, Nombre = "Accesorio X", Activo = true, Provisional = true };
        prov.Variantes.Add(new ProductoVariante { VarianteId = 2, ProductoId = 2, PrecioVenta = 50m, PrecioCosto = 0m, Activo = true });

        db.Productos.AddRange(normal, prov);

        var venta = new Venta { VentaId = 1, SucursalId = 1, UsuarioId = 1, Fecha = Dia, Total = 150m, Estado = "Completada" };
        venta.Detalles.Add(new VentaDetalle { VentaDetalleId = 1, VentaId = 1, VarianteId = 1, Cantidad = 1, PrecioUnitario = 100m, Total = 100m });
        venta.Detalles.Add(new VentaDetalle { VentaDetalleId = 2, VentaId = 1, VarianteId = 2, Cantidad = 1, PrecioUnitario = 50m, Total = 50m });
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();

        var r = await new ReporteService(db).VentasAsync(Dia, Dia, null);

        Assert.Equal(40m, r.Ganancia);          // solo el margen del producto normal
        Assert.Equal(1, r.LineasSinCosto);      // la línea provisional
        Assert.Equal(50m, r.MontoSinCosto);     // su precio, divulgado aparte
    }
}
