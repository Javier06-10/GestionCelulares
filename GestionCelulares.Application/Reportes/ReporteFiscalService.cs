using System.Globalization;
using System.Text;
using GestionCelulares.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Reportes;

public interface IReporteFiscalService
{
    /// <summary>Genera el formato 607 (ventas) de la DGII para un período mensual.</summary>
    Task<Reporte607Dto> Generar607Async(int anio, int mes);
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
                NumeroFactura = v.NumeroFactura,
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
}
