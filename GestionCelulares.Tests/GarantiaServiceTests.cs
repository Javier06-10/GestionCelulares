using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Garantias;
using GestionCelulares.Application.Ventas;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class GarantiaServiceTests
{
    private const int Sucursal = 1, Usuario = 7;

    /// <summary>Stub de NCF: devuelve un correlativo fijo por tipo.</summary>
    private sealed class NcfStub : INcfProcedures
    {
        public Task<string?> SiguienteNcfAsync(string tipo) => Task.FromResult<string?>($"B{tipo}00000001");
    }

    /// <summary>Stub de procedimientos de venta: solo se usa ProximoNumeroFactura aquí.</summary>
    private sealed class VentaProcStub : IVentaProcedures
    {
        public Task<int> RegistrarAsync(VentaRegistroDto dto, int usuarioId, int? sesionCajaId) => Task.FromResult(0);
        public Task<string> ProximoNumeroFacturaAsync() => Task.FromResult("FC-0001");
    }

    private static async Task<(GestionCelularesContext db, int casoId, int reemplazoId, int metodoId)> SembrarAsync(bool cajaAbierta = true)
    {
        var db = TestDb.Crear();
        db.Empresas.Add(new Empresa { EmpresaId = 1, Nombre = "Tienda", RNC = "131", PorcentajeItbis = 18m });
        db.Sucursales.Add(new Sucursal { SucursalId = Sucursal, Nombre = "Principal" });
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = 1, Nombre = "Efectivo" });
        if (cajaAbierta)
            db.SesionesCaja.Add(new SesionCaja { SucursalId = Sucursal, UsuarioApertura = Usuario, MontoApertura = 0m, Estado = "Abierta", FechaApertura = DateTime.Now });

        var prod = new Producto { ProductoId = 1, Nombre = "Equipo", Serializado = true, Activo = true };
        prod.Variantes.Add(new ProductoVariante { VarianteId = 1, ProductoId = 1, PrecioVenta = 12000m, PrecioCosto = 9000m, Activo = true });
        db.Productos.Add(prod);

        // Equipo defectuoso (del caso) y equipo de reemplazo disponible
        db.InventarioImeis.Add(new InventarioImei { ImeiId = 10, Imei = "IMEI-DEF", VarianteId = 1, SucursalId = Sucursal, Estado = "Vendido", PrecioCosto = 9000m });
        db.InventarioImeis.Add(new InventarioImei { ImeiId = 20, Imei = "IMEI-REP", VarianteId = 1, SucursalId = Sucursal, Estado = "Disponible", PrecioCosto = 10000m });

        db.CasosGarantia.Add(new CasoGarantia { CasoGarantiaId = 100, ImeiId = 10, Estado = "EnProceso", FechaApertura = DateTime.Now });
        await db.SaveChangesAsync();
        return (db, 100, 20, 1);
    }

    private static GarantiaService Servicio(GestionCelularesContext db) => new(db, new NcfStub(), new VentaProcStub());

    [Fact]
    public async Task Reemplazo_con_diferencia_genera_venta_en_caja_y_607()
    {
        var (db, casoId, reemplazoId, metodoId) = await SembrarAsync();
        var svc = Servicio(db);

        var res = await svc.ResolverCasoAsync(casoId, new CasoResolverDto
        {
            TipoResolucion = "Reemplazo", ImeiReemplazoId = reemplazoId,
            MontoDiferencia = 1180m, MetodoPagoId = metodoId
        }, Usuario);

        Assert.NotNull(res.VentaDiferenciaId);
        Assert.Equal(1180m, res.MontoDiferencia);

        var venta = await db.Ventas.Include(v => v.Detalles).Include(v => v.Pagos).SingleAsync();
        Assert.Equal("Completada", venta.Estado);
        Assert.Equal(1000m, venta.Subtotal);         // base sin ITBIS
        Assert.Equal(180m, venta.Impuesto);          // 18%
        Assert.Equal(1180m, venta.Total);
        Assert.NotNull(venta.SesionCajaId);          // entra a la caja del turno
        Assert.Equal("B0200000001", venta.Ncf);      // NCF de consumo (sin cédula)
        Assert.Null(venta.Detalles.Single().ImeiId); // dinero puro: no descuenta inventario
        Assert.Equal(1180m, venta.Pagos.Single().Monto);

        var reemplazo = await db.InventarioImeis.FirstAsync(i => i.ImeiId == reemplazoId);
        Assert.Equal("Vendido", reemplazo.Estado);
    }

    [Fact]
    public async Task Reemplazo_a_igual_valor_no_genera_venta()
    {
        var (db, casoId, reemplazoId, _) = await SembrarAsync();
        var svc = Servicio(db);

        var res = await svc.ResolverCasoAsync(casoId, new CasoResolverDto
        {
            TipoResolucion = "Reemplazo", ImeiReemplazoId = reemplazoId, MontoDiferencia = 0m
        }, Usuario);

        Assert.Null(res.VentaDiferenciaId);
        Assert.False(await db.Ventas.AnyAsync());
        Assert.Equal("Vendido", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == reemplazoId)).Estado);
    }

    [Fact]
    public async Task Reemplazo_con_diferencia_sin_metodo_de_pago_falla()
    {
        var (db, casoId, reemplazoId, _) = await SembrarAsync();
        var svc = Servicio(db);

        await Assert.ThrowsAsync<GarantiaException>(() => svc.ResolverCasoAsync(casoId, new CasoResolverDto
        {
            TipoResolucion = "Reemplazo", ImeiReemplazoId = reemplazoId, MontoDiferencia = 500m
        }, Usuario));
    }

    [Fact]
    public async Task Reemplazo_con_diferencia_sin_caja_abierta_falla()
    {
        var (db, casoId, reemplazoId, metodoId) = await SembrarAsync(cajaAbierta: false);
        var svc = Servicio(db);

        await Assert.ThrowsAsync<GarantiaException>(() => svc.ResolverCasoAsync(casoId, new CasoResolverDto
        {
            TipoResolucion = "Reemplazo", ImeiReemplazoId = reemplazoId, MontoDiferencia = 500m, MetodoPagoId = metodoId
        }, Usuario));
    }
}
