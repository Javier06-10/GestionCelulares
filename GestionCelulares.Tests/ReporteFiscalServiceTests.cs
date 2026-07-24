using GestionCelulares.Application.Reportes;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Xunit;

namespace GestionCelulares.Tests;

public class ReporteFiscalServiceTests
{
    private const int Anio = 2026;
    private const int Mes = 3;
    private static readonly DateTime EnPeriodo = new(Anio, Mes, 15);

    private static GestionCelularesContext ConEmpresa()
    {
        var db = TestDb.Crear();
        db.Empresas.Add(new Empresa { EmpresaId = 1, Nombre = "Mi Tienda", RNC = "131000001", PorcentajeItbis = 18m });
        return db;
    }

    [Fact]
    public async Task Reporte607_incluye_ventas_y_notas_de_credito_y_neto()
    {
        var db = ConEmpresa();
        db.Clientes.Add(new Cliente { ClienteId = 1, Nombre = "Juan", Cedula = "00112345678" });
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = 1, Nombre = "Efectivo" });

        var venta = new Venta
        {
            VentaId = 1, Ncf = "B0200000001", SucursalId = 1, ClienteId = 1, UsuarioId = 1,
            Fecha = EnPeriodo, Subtotal = 1000m, Descuento = 0m, Impuesto = 180m, Total = 1180m,
            EsCredito = false, Estado = "Completada"
        };
        venta.Pagos.Add(new VentaPago { VentaPagoId = 1, VentaId = 1, MetodoPagoId = 1, Monto = 1180m });
        db.Ventas.Add(venta);

        db.NotasCredito.Add(new NotaCredito
        {
            NotaCreditoId = 1, VentaId = 1, Ncf = "B0400000001", NcfModificado = "B0200000001",
            Fecha = EnPeriodo, Monto = 1000m, Itbis = 180m, Total = 1180m, FechaRegistro = EnPeriodo
        });
        await db.SaveChangesAsync();

        var r = await new ReporteFiscalService(db).Generar607Async(Anio, Mes);

        Assert.Equal(2, r.Cantidad);                 // 1 venta + 1 nota de crédito
        Assert.Equal(1, r.NotasCredito);
        Assert.Contains("B0400000001", r.ContenidoTxt);  // NCF de la nota
        Assert.Contains("B0200000001", r.ContenidoTxt);  // NCF modificado (original)
        Assert.Equal(0m, r.TotalMontoFacturado);     // la nota revierte la base → neto 0
        Assert.Equal(0m, r.TotalItbis);
    }

    [Fact]
    public async Task Reporte607_incluye_ventas_devueltas()
    {
        // Una venta devuelta SIGUE declarada en el 607 (la nota de crédito la corrige).
        var db = ConEmpresa();
        db.Ventas.Add(new Venta
        {
            VentaId = 3, Ncf = "B0200000003", SucursalId = 1, UsuarioId = 1, Fecha = EnPeriodo,
            Subtotal = 800m, Impuesto = 144m, Total = 944m, Estado = "Devuelta"
        });
        await db.SaveChangesAsync();

        var r = await new ReporteFiscalService(db).Generar607Async(Anio, Mes);

        Assert.Equal(1, r.Cantidad);
        Assert.Contains("B0200000003", r.ContenidoTxt);
    }

    [Fact]
    public async Task Reporte607_excluye_ventas_anuladas()
    {
        var db = ConEmpresa();
        db.Ventas.Add(new Venta
        {
            VentaId = 2, Ncf = "B0200000009", SucursalId = 1, UsuarioId = 1, Fecha = EnPeriodo,
            Subtotal = 500m, Impuesto = 90m, Total = 590m, Estado = "Anulada"
        });
        await db.SaveChangesAsync();

        var r = await new ReporteFiscalService(db).Generar607Async(Anio, Mes);

        Assert.Equal(0, r.Cantidad);
    }

    [Fact]
    public async Task Reporte608_lista_los_comprobantes_anulados_del_periodo()
    {
        var db = ConEmpresa();
        db.ComprobantesAnulados.Add(new ComprobanteAnulado
        {
            ComprobanteAnuladoId = 1, Ncf = "B0200000001", FechaComprobante = EnPeriodo,
            TipoAnulacion = "02", FechaRegistro = EnPeriodo
        });
        await db.SaveChangesAsync();

        var r = await new ReporteFiscalService(db).Generar608Async(Anio, Mes);

        Assert.Equal(1, r.Cantidad);
        Assert.Contains("B0200000001", r.ContenidoTxt);
    }
}
