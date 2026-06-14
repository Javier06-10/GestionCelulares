namespace GestionCelulares.Domain.Entities;

/// <summary>Producto que falta / hay que reponer (lista agregada manualmente).</summary>
public class Faltante
{
    public int FaltanteId { get; set; }
    public string Descripcion { get; set; } = null!;
    public int? VarianteId { get; set; }
    public int CantidadDeseada { get; set; } = 1;
    public string? Notas { get; set; }
    public bool Resuelto { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ProductoVariante? Variante { get; set; }
}
