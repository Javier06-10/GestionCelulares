namespace GestionCelulares.Domain.Entities;

/// <summary>Rango de NCF autorizado por la DGII para un tipo de comprobante.</summary>
public class SecuenciaNcf
{
    public int SecuenciaNcfId { get; set; }
    public string TipoComprobante { get; set; } = null!;  // 01 Crédito Fiscal, 02 Consumo, 04 Nota de Crédito
    public string Serie { get; set; } = "B";
    public int Secuencia { get; set; } = 1;               // próximo número a asignar
    public int Hasta { get; set; }                        // límite autorizado
    public DateTime? Vencimiento { get; set; }
    public bool Activo { get; set; }
}
