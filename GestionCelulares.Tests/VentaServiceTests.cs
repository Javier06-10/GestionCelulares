using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class VentaServiceTests
{
    // --- Stubs de los procedimientos (el SP real no corre en la BD in-memory) ---
    private sealed class NcfStub : INcfProcedures
    {
        public Task<string?> SiguienteNcfAsync(string tipo) => Task.FromResult<string?>($"B{tipo}00000007");
    }
    private sealed class VentaProcStub : IVentaProcedures
    {
        public Task<int> RegistrarAsync(VentaRegistroDto dto, int usuarioId, int? sesionCajaId) => Task.FromResult(99);
        public Task<string> ProximoNumeroFacturaAsync() => Task.FromResult("FC-0001");
    }

    private static (GestionCelularesContext db, VentaService svc) Crear(bool cajaAbierta = false, int stock = 5)
    {
        var db = TestDb.Crear();
        db.Empresas.Add(new Empresa { EmpresaId = 1, Nombre = "Tienda", RNC = "131", PorcentajeItbis = 18m });
        db.Sucursales.Add(new Sucursal { SucursalId = 1, Nombre = "Principal" });
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = 1, Nombre = "Efectivo" });
        var p = new Producto { ProductoId = 1, Nombre = "Producto", Activo = true };
        p.Variantes.Add(new ProductoVariante { VarianteId = 1, ProductoId = 1, PrecioVenta = 100m, PrecioCosto = 60m, StockNoSerial = stock, Activo = true });
        db.Productos.Add(p);
        if (cajaAbierta)
            db.SesionesCaja.Add(new SesionCaja { SucursalId = 1, UsuarioApertura = 1, MontoApertura = 0m, Estado = "Abierta", FechaApertura = DateTime.Now });
        db.SaveChanges();
        return (db, new VentaService(db, new VentaProcStub(), new NcfStub()));
    }

    private static VentaRegistroDto Dto(int? metodo = 1, bool credito = false, int? cliente = null, int cantidad = 1) => new()
    {
        SucursalId = 1, EsCredito = credito, ClienteId = cliente, MetodoPagoId = metodo,
        Detalles = new() { new VentaDetalleRegistroDto { VarianteId = 1, Cantidad = cantidad, PrecioUnitario = 100m } }
    };

    // Siembra una venta con un equipo (IMEI) vendido, lista para anular/devolver.
    private static async Task SembrarVentaConImeiAsync(GestionCelularesContext db, string estado, string? ncf)
    {
        var venta = new Venta
        {
            VentaId = 1, SucursalId = 1, UsuarioId = 1, Fecha = DateTime.Now,
            Subtotal = 1000m, Descuento = 0m, Impuesto = 180m, Total = 1180m, Estado = estado, Ncf = ncf
        };
        venta.Detalles.Add(new VentaDetalle { VentaDetalleId = 1, VentaId = 1, ImeiId = 10, VarianteId = 1, Cantidad = 1, PrecioUnitario = 1180m, Total = 1180m });
        db.Ventas.Add(venta);
        db.InventarioImeis.Add(new InventarioImei { ImeiId = 10, Imei = "IMEI1", VarianteId = 1, SucursalId = 1, Estado = "Vendido", PrecioCosto = 800m });
        await db.SaveChangesAsync();
    }

    // ---------- Validaciones de RegistrarAsync (antes del SP) ----------

    [Fact]
    public async Task Registrar_sin_detalles_falla()
    {
        var (_, svc) = Crear(cajaAbierta: true);
        var dto = new VentaRegistroDto { SucursalId = 1, MetodoPagoId = 1, Detalles = new() };
        await Assert.ThrowsAsync<VentaException>(() => svc.RegistrarAsync(dto, 1));
    }

    [Fact]
    public async Task Registrar_sin_caja_abierta_falla()
    {
        var (_, svc) = Crear(cajaAbierta: false);
        await Assert.ThrowsAsync<VentaException>(() => svc.RegistrarAsync(Dto(), 1));
    }

    [Fact]
    public async Task Registrar_credito_sin_cliente_falla()
    {
        var (_, svc) = Crear(cajaAbierta: true);
        await Assert.ThrowsAsync<VentaException>(() => svc.RegistrarAsync(Dto(metodo: null, credito: true), 1));
    }

    [Fact]
    public async Task Registrar_contado_sin_metodo_falla()
    {
        var (_, svc) = Crear(cajaAbierta: true);
        await Assert.ThrowsAsync<VentaException>(() => svc.RegistrarAsync(Dto(metodo: null), 1));
    }

    [Fact]
    public async Task Registrar_stock_insuficiente_falla()
    {
        var (_, svc) = Crear(cajaAbierta: true, stock: 1);
        await Assert.ThrowsAsync<VentaException>(() => svc.RegistrarAsync(Dto(cantidad: 5), 1));
    }

    // ---------- AnularAsync ----------

    [Fact]
    public async Task Anular_completada_revierte_imei_y_registra_608()
    {
        var (db, svc) = Crear();
        await SembrarVentaConImeiAsync(db, "Completada", "B0200000001");

        var res = await svc.AnularAsync(1, new AnularVentaDto { TipoAnulacion = "02", Motivo = "prueba" }, 1);

        Assert.Equal("Anulada", res.Estado);
        Assert.Equal("Disponible", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == 10)).Estado);
        var ca = await db.ComprobantesAnulados.SingleAsync();
        Assert.Equal("B0200000001", ca.Ncf);      // alimenta el 608
        Assert.Equal("02", ca.TipoAnulacion);
    }

    [Fact]
    public async Task Anular_repone_stock_de_accesorio()
    {
        var (db, svc) = Crear(stock: 1);
        var venta = new Venta { VentaId = 1, SucursalId = 1, UsuarioId = 1, Fecha = DateTime.Now, Total = 200m, Estado = "Completada" };
        venta.Detalles.Add(new VentaDetalle { VentaDetalleId = 1, VentaId = 1, VarianteId = 1, Cantidad = 2, PrecioUnitario = 100m, Total = 200m });
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();

        await svc.AnularAsync(1, new AnularVentaDto { TipoAnulacion = "02" }, 1);

        Assert.Equal(3, (await db.ProductoVariantes.FirstAsync(v => v.VarianteId == 1)).StockNoSerial); // 1 + 2 repuestos
    }

    [Fact]
    public async Task Anular_venta_ya_anulada_falla()
    {
        var (db, svc) = Crear();
        await SembrarVentaConImeiAsync(db, "Anulada", "B0200000001");
        await Assert.ThrowsAsync<VentaException>(() => svc.AnularAsync(1, new AnularVentaDto { TipoAnulacion = "02" }, 1));
    }

    // ---------- DevolverAsync ----------

    [Fact]
    public async Task Devolver_completada_crea_nota_credito_y_estado_devuelta()
    {
        var (db, svc) = Crear();
        await SembrarVentaConImeiAsync(db, "Completada", "B0200000001");

        var res = await svc.DevolverAsync(1, "defectuoso", 1);

        Assert.Equal("Devuelta", res.Estado);   // sigue en el 607
        Assert.Equal("Disponible", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == 10)).Estado);
        var nc = await db.NotasCredito.SingleAsync();
        Assert.Equal("B0200000001", nc.NcfModificado);   // referencia a la venta original
        Assert.Equal("B0400000007", nc.Ncf);             // NCF de nota de crédito (04)
        Assert.Equal(1000m, nc.Monto);                   // Subtotal - Descuento
        Assert.Equal(180m, nc.Itbis);
        Assert.Equal(1180m, nc.Total);
    }

    [Fact]
    public async Task Devolver_sin_ncf_falla()
    {
        var (db, svc) = Crear();
        await SembrarVentaConImeiAsync(db, "Completada", null);
        await Assert.ThrowsAsync<VentaException>(() => svc.DevolverAsync(1, null, 1));
    }

    [Fact]
    public async Task Devolver_venta_no_completada_falla()
    {
        var (db, svc) = Crear();
        await SembrarVentaConImeiAsync(db, "Anulada", "B0200000001");
        await Assert.ThrowsAsync<VentaException>(() => svc.DevolverAsync(1, null, 1));
    }
}
