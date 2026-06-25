namespace GestionCelulares.Domain.Entities;

/// <summary>NCF anulado (alimenta el formato 608 de la DGII).</summary>
public class ComprobanteAnulado
{
    public int ComprobanteAnuladoId { get; set; }
    public string Ncf { get; set; } = null!;
    public DateTime FechaComprobante { get; set; }
    public string TipoAnulacion { get; set; } = null!;  // 01-09 según DGII
    public string? Motivo { get; set; }
    public int? VentaId { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }

    public Venta? Venta { get; set; }
}

/// <summary>Nota de Crédito (tipo 04): comprobante que revierte una factura ya
/// emitida. Tiene NCF propio (04) y referencia el NCF original. Se reporta en el 607.</summary>
public class NotaCredito
{
    public int NotaCreditoId { get; set; }
    public int VentaId { get; set; }
    public string? Ncf { get; set; }            // NCF 04 emitido
    public string? NcfModificado { get; set; }  // NCF original (01/02)
    public DateTime Fecha { get; set; }         // fecha del comprobante original
    public decimal Monto { get; set; }          // base imponible revertida
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }

    public Venta? Venta { get; set; }
}
