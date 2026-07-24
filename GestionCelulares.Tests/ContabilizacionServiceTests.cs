using GestionCelulares.Application.Common;
using GestionCelulares.Application.Contabilidad;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class ContabilizacionServiceTests
{
    private static readonly DateTime Dia = new(2026, 3, 15);

    private static void AddCuenta(GestionCelularesContext db, int id, string cod, string nombre, string tipo, string naturaleza) =>
        db.CuentasContables.Add(new CuentaContable
        {
            CuentaContableId = id, Codigo = cod, Nombre = nombre, Tipo = tipo,
            Naturaleza = naturaleza, PermiteMovimiento = true, Activo = true
        });

    private static (GestionCelularesContext db, ContabilizacionService svc) Crear()
    {
        var db = TestDb.Crear();
        // Catálogo mínimo con los códigos que usa la contabilización de ventas
        AddCuenta(db, 1, "1.01", "Caja", "Activo", "Deudora");
        AddCuenta(db, 2, "1.02", "Bancos", "Activo", "Deudora");
        AddCuenta(db, 3, "1.03", "Cuentas por cobrar", "Activo", "Deudora");
        AddCuenta(db, 4, "1.04", "Inventario", "Activo", "Deudora");
        AddCuenta(db, 5, "2.02", "ITBIS por pagar", "Pasivo", "Acreedora");
        AddCuenta(db, 6, "3.02", "Resultados acumulados", "Capital", "Acreedora");
        AddCuenta(db, 7, "4.01", "Ventas", "Ingreso", "Acreedora");
        AddCuenta(db, 8, "5.01", "Costo de venta", "Costo", "Deudora");
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = 1, Nombre = "Efectivo" });
        var p = new Producto { ProductoId = 1, Nombre = "iPhone", Activo = true };
        p.Variantes.Add(new ProductoVariante { VarianteId = 1, ProductoId = 1, PrecioVenta = 1180m, PrecioCosto = 800m, Activo = true });
        db.Productos.Add(p);
        db.SaveChanges();
        return (db, new ContabilizacionService(db));
    }

    // Venta completada de 1180 (neto 1000 + ITBIS 180), costo del equipo 800.
    private static async Task SembrarVentaAsync(GestionCelularesContext db, bool credito)
    {
        var venta = new Venta
        {
            VentaId = 1, NumeroFactura = "F1", SucursalId = 1, UsuarioId = 1, Fecha = Dia,
            Subtotal = 1000m, Impuesto = 180m, Total = 1180m, EsCredito = credito, Estado = "Completada"
        };
        venta.Detalles.Add(new VentaDetalle { VentaDetalleId = 1, VentaId = 1, ImeiId = 10, VarianteId = 1, Cantidad = 1, PrecioUnitario = 1180m, Total = 1180m });
        if (!credito)
            venta.Pagos.Add(new VentaPago { VentaPagoId = 1, VentaId = 1, MetodoPagoId = 1, Monto = 1180m });
        db.Ventas.Add(venta);
        db.InventarioImeis.Add(new InventarioImei { ImeiId = 10, Imei = "IMEI1", VarianteId = 1, SucursalId = 1, Estado = "Vendido", PrecioCosto = 800m });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Contabiliza_venta_contado_con_asiento_balanceado()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: false);

        var r = await svc.ContabilizarPendientesAsync(Dia, Dia, 1);

        Assert.Equal(1, r.Ventas);
        var lineas = await db.AsientoDetalles.Include(x => x.Cuenta).Where(x => x.Asiento.Origen == "Venta").ToListAsync();
        decimal Deb(string cod) => lineas.Where(l => l.Cuenta.Codigo == cod).Sum(l => l.Debito);
        decimal Cre(string cod) => lineas.Where(l => l.Cuenta.Codigo == cod).Sum(l => l.Credito);

        Assert.Equal(lineas.Sum(l => l.Debito), lineas.Sum(l => l.Credito));   // partida doble cuadra
        Assert.Equal(1180m, Deb("1.01"));   // Caja recibe el total
        Assert.Equal(1000m, Cre("4.01"));   // Ventas por el neto
        Assert.Equal(180m, Cre("2.02"));    // ITBIS por pagar
        Assert.Equal(800m, Deb("5.01"));    // Costo de venta
        Assert.Equal(800m, Cre("1.04"));    // descarga de Inventario
    }

    [Fact]
    public async Task Contabiliza_venta_a_credito_usa_cuentas_por_cobrar()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: true);

        await svc.ContabilizarPendientesAsync(Dia, Dia, 1);

        var lineas = await db.AsientoDetalles.Include(x => x.Cuenta).Where(x => x.Asiento.Origen == "Venta").ToListAsync();
        Assert.Equal(1180m, lineas.Where(l => l.Cuenta.Codigo == "1.03").Sum(l => l.Debito));  // CxC
        Assert.Equal(0m, lineas.Where(l => l.Cuenta.Codigo == "1.01").Sum(l => l.Debito));      // Caja no
    }

    [Fact]
    public async Task Contabilizar_es_idempotente()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: false);

        await svc.ContabilizarPendientesAsync(Dia, Dia, 1);
        var segunda = await svc.ContabilizarPendientesAsync(Dia, Dia, 1);

        Assert.Equal(0, segunda.Ventas);                                           // no re-contabiliza
        Assert.Equal(1, await db.Asientos.CountAsync(a => a.Origen == "Venta"));    // un solo asiento
    }

    [Fact]
    public async Task Contabilizar_sin_cuenta_requerida_falla()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: false);
        db.CuentasContables.Remove(await db.CuentasContables.FirstAsync(c => c.Codigo == "4.01"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ContabilidadException>(() => svc.ContabilizarPendientesAsync(Dia, Dia, 1));
    }

    [Fact]
    public async Task Estado_resultados_refleja_ingreso_costo_y_utilidad()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: false);
        await svc.ContabilizarPendientesAsync(Dia, Dia, 1);

        var er = await svc.EstadoResultadosAsync(Dia, Dia);

        Assert.Equal(1000m, er.TotalIngresos);
        Assert.Equal(800m, er.TotalCostos);
        Assert.Equal(200m, er.UtilidadBruta);
    }

    [Fact]
    public async Task Balance_general_cuadra_tras_contabilizar()
    {
        var (db, svc) = Crear();
        await SembrarVentaAsync(db, credito: false);
        await svc.ContabilizarPendientesAsync(Dia, Dia, 1);

        var bg = await svc.BalanceGeneralAsync(Dia);

        Assert.True(bg.Cuadrado);   // Activos = Pasivos + Capital
    }
}
