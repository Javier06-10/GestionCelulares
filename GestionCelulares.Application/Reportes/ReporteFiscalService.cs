using System.Globalization;
using System.Text;
using GestionCelulares.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Reportes;

public interface IReporteFiscalService
{
    /// <summary>Genera el formato 607 (ventas) de la DGII para un período mensual.</summary>
    Task<Reporte607Dto> Generar607Async(int anio, int mes);

    /// <summary>Genera el formato 606 (compras) de la DGII para un período mensual.</summary>
    Task<Reporte606Dto> Generar606Async(int anio, int mes);

    /// <summary>Genera el formato 608 (comprobantes anulados) de la DGII para un período mensual.</summary>
    Task<Reporte608Dto> Generar608Async(int anio, int mes);
}

public class ReporteFiscalService : IReporteFiscalService
{
    private readonly IApplicationDbContext _db;

    public ReporteFiscalService(IApplicationDbContext db) => _db = db;

    public async Task<Reporte607Dto> Generar607Async(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1);
        var periodo = $"{anio:0000}{mes:00}";

        var rnc = SoloDigitos(await _db.Empresas.Select(e => e.RNC).FirstOrDefaultAsync());

        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Completada" && v.Fecha >= desde && v.Fecha < hasta)
            .OrderBy(v => v.Fecha)
            .Select(v => new VentaLinea
            {
                NumeroFactura = v.Ncf ?? v.NumeroFactura,
                Fecha = v.Fecha,
                Subtotal = v.Subtotal,
                Descuento = v.Descuento,
                Impuesto = v.Impuesto,
                Total = v.Total,
                EsCredito = v.EsCredito,
                Cedula = v.Cliente != null ? v.Cliente.Cedula : null,
                Pagos = v.Pagos.Select(p => new PagoLinea { Metodo = p.MetodoPago.Nombre, Monto = p.Monto }).ToList()
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append("607|").Append(rnc).Append('|').Append(periodo).Append('|').Append(ventas.Count).Append('\n');

        decimal totalBase = 0, totalItbis = 0;
        int sinComprobante = 0;

        foreach (var v in ventas)
        {
            var ced = SoloDigitos(v.Cedula);
            var tipoId = ced.Length == 9 ? "1" : ced.Length == 11 ? "2" : "";
            var ncf = (v.NumeroFactura ?? "").Trim();
            if (string.IsNullOrEmpty(ncf)) sinComprobante++;

            var baseImponible = v.Subtotal - v.Descuento;
            totalBase += baseImponible;
            totalItbis += v.Impuesto;

            // Formas de pago (la columna a crédito aplica si la venta es a crédito)
            decimal efectivo = 0, chequeTransf = 0, tarjeta = 0, credito = 0, otras = 0;
            if (v.EsCredito)
            {
                credito = v.Total;
            }
            else
            {
                foreach (var p in v.Pagos)
                {
                    switch (p.Metodo)
                    {
                        case "Efectivo": efectivo += p.Monto; break;
                        case "Tarjeta": tarjeta += p.Monto; break;
                        case "Transferencia": chequeTransf += p.Monto; break;
                        default: otras += p.Monto; break;   // Cheque, Apartado, etc.
                    }
                }
            }

            // 23 columnas del formato 607
            sb.Append(ced).Append('|')                          // 1 RNC/Cédula
              .Append(tipoId).Append('|')                       // 2 Tipo identificación
              .Append(ncf).Append('|')                          // 3 NCF
              .Append('|')                                      // 4 NCF modificado
              .Append("01").Append('|')                         // 5 Tipo de ingreso
              .Append(v.Fecha.ToString("yyyyMMdd")).Append('|') // 6 Fecha comprobante
              .Append('|')                                      // 7 Fecha de retención
              .Append(M(baseImponible)).Append('|')             // 8 Monto facturado
              .Append(M(v.Impuesto)).Append('|')                // 9 ITBIS facturado
              .Append('|')                                      // 10 ITBIS retenido
              .Append('|')                                      // 11 ITBIS percibido
              .Append('|')                                      // 12 Retención renta
              .Append('|')                                      // 13 ISR percibido
              .Append('|')                                      // 14 Impuesto selectivo al consumo
              .Append('|')                                      // 15 Otros impuestos/tasas
              .Append('|')                                      // 16 Monto propina legal
              .Append(M(efectivo)).Append('|')                  // 17 Efectivo
              .Append(M(chequeTransf)).Append('|')              // 18 Cheque/Transferencia/Depósito
              .Append(M(tarjeta)).Append('|')                   // 19 Tarjeta débito/crédito
              .Append(M(credito)).Append('|')                   // 20 Venta a crédito
              .Append('|')                                      // 21 Bonos o certificados de regalo
              .Append('|')                                      // 22 Permuta
              .Append(M(otras))                                 // 23 Otras formas de venta
              .Append('\n');
        }

        return new Reporte607Dto
        {
            Periodo = periodo,
            Rnc = rnc,
            Cantidad = ventas.Count,
            TotalMontoFacturado = totalBase,
            TotalItbis = totalItbis,
            SinComprobante = sinComprobante,
            NombreArchivo = $"607{rnc}{periodo}.txt",
            ContenidoTxt = sb.ToString()
        };
    }

    public async Task<Reporte606Dto> Generar606Async(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1);
        var periodo = $"{anio:0000}{mes:00}";

        var rnc = SoloDigitos(await _db.Empresas.Select(e => e.RNC).FirstOrDefaultAsync());

        var compras = await _db.Compras.AsNoTracking()
            .Where(c => c.Fecha >= desde && c.Fecha < hasta)
            .OrderBy(c => c.Fecha)
            .Select(c => new CompraLinea
            {
                NumeroFactura = c.NumeroFactura,
                Fecha = c.Fecha,
                Total = c.Total,
                Subtotal = c.Subtotal,
                Itbis = c.Itbis,
                TipoBienServicio = c.TipoBienServicio,
                RncProveedor = c.Proveedor.RNC,
                Contado = _db.PagosProveedor.Any(p => p.CompraId == c.CompraId)
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append("606|").Append(rnc).Append('|').Append(periodo).Append('|').Append(compras.Count).Append('\n');

        decimal totalBase = 0, totalItbis = 0;
        int sinRnc = 0;

        foreach (var c in compras)
        {
            var rncProv = SoloDigitos(c.RncProveedor);
            var tipoId = rncProv.Length == 9 ? "1" : rncProv.Length == 11 ? "2" : "";
            if (string.IsNullOrEmpty(rncProv)) sinRnc++;

            var itbis = c.Itbis ?? 0;
            var baseImponible = c.Subtotal ?? (c.Total - itbis);
            totalBase += baseImponible;
            totalItbis += itbis;

            var tipoBS = string.IsNullOrWhiteSpace(c.TipoBienServicio) ? "09" : c.TipoBienServicio;
            var formaPago = c.Contado ? "01" : "04";   // 01 efectivo (contado) / 04 a crédito

            // 14 columnas usadas del formato 606 (las demás van vacías)
            sb.Append(rncProv).Append('|')                       // 1 RNC/Cédula proveedor
              .Append(tipoId).Append('|')                        // 2 Tipo identificación
              .Append(tipoBS).Append('|')                        // 3 Tipo de bienes y servicios comprados
              .Append((c.NumeroFactura ?? "").Trim()).Append('|')// 4 NCF
              .Append('|')                                       // 5 NCF o documento modificado
              .Append(c.Fecha.ToString("yyyyMMdd")).Append('|')  // 6 Fecha comprobante
              .Append(c.Contado ? c.Fecha.ToString("yyyyMMdd") : "").Append('|') // 7 Fecha de pago
              .Append("0.00").Append('|')                        // 8 Monto facturado en servicios
              .Append(M(baseImponible)).Append('|')              // 9 Monto facturado en bienes
              .Append(M(baseImponible)).Append('|')              // 10 Total monto facturado
              .Append(M(itbis)).Append('|')                      // 11 ITBIS facturado
              .Append('|')                                       // 12 ITBIS retenido
              .Append('|')                                       // 13 ITBIS sujeto a proporcionalidad
              .Append('|')                                       // 14 ITBIS llevado al costo
              .Append(M(itbis)).Append('|')                      // 15 ITBIS por adelantar
              .Append('|')                                       // 16 ITBIS percibido en compras
              .Append('|')                                       // 17 Tipo de retención en ISR
              .Append('|')                                       // 18 Monto retención renta
              .Append('|')                                       // 19 ISR percibido en compras
              .Append('|')                                       // 20 Impuesto selectivo al consumo
              .Append('|')                                       // 21 Otros impuestos/tasas
              .Append('|')                                       // 22 Monto propina legal
              .Append(formaPago)                                 // 23 Forma de pago
              .Append('\n');
        }

        return new Reporte606Dto
        {
            Periodo = periodo,
            Rnc = rnc,
            Cantidad = compras.Count,
            TotalMontoFacturado = totalBase,
            TotalItbis = totalItbis,
            SinRnc = sinRnc,
            NombreArchivo = $"606{rnc}{periodo}.txt",
            ContenidoTxt = sb.ToString()
        };
    }

    public async Task<Reporte608Dto> Generar608Async(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1);
        var periodo = $"{anio:0000}{mes:00}";
        var rnc = SoloDigitos(await _db.Empresas.Select(e => e.RNC).FirstOrDefaultAsync());

        var anulados = await _db.ComprobantesAnulados.AsNoTracking()
            .Where(a => a.FechaRegistro >= desde && a.FechaRegistro < hasta)
            .OrderBy(a => a.FechaComprobante)
            .Select(a => new { a.Ncf, a.FechaComprobante, a.TipoAnulacion })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append("608|").Append(rnc).Append('|').Append(periodo).Append('|').Append(anulados.Count).Append('\n');

        foreach (var a in anulados)
        {
            // 3 columnas del formato 608
            sb.Append((a.Ncf ?? "").Trim()).Append('|')         // 1 NCF
              .Append(a.FechaComprobante.ToString("yyyyMMdd")).Append('|') // 2 Fecha comprobante
              .Append(a.TipoAnulacion)                          // 3 Tipo de anulación
              .Append('\n');
        }

        return new Reporte608Dto
        {
            Periodo = periodo,
            Rnc = rnc,
            Cantidad = anulados.Count,
            NombreArchivo = $"608{rnc}{periodo}.txt",
            ContenidoTxt = sb.ToString()
        };
    }

    private static string M(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    private static string SoloDigitos(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    private sealed class VentaLinea
    {
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public bool EsCredito { get; set; }
        public string? Cedula { get; set; }
        public List<PagoLinea> Pagos { get; set; } = new();
    }

    private sealed class PagoLinea
    {
        public string Metodo { get; set; } = null!;
        public decimal Monto { get; set; }
    }

    private sealed class CompraLinea
    {
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Itbis { get; set; }
        public string? TipoBienServicio { get; set; }
        public string? RncProveedor { get; set; }
        public bool Contado { get; set; }
    }
}
