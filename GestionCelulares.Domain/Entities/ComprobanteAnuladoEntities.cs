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
