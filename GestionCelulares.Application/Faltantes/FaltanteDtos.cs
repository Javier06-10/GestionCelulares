using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Faltantes;

/// <summary>Producto agotado detectado automáticamente (stock 0).</summary>
public class AgotadoDto
{
    public string Tipo { get; set; } = null!;           // "Dispositivo" | "Accesorio"
    public int VarianteId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public string? Variante { get; set; }                // color · almacenamiento · condición
    public int Stock { get; set; }                        // 0 (o stock no serializado)
}

public class FaltanteDto
{
    public int FaltanteId { get; set; }
    public string Descripcion { get; set; } = null!;
    public int? VarianteId { get; set; }
    public int CantidadDeseada { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class FaltanteCrearDto
{
    [Required, StringLength(200)] public string Descripcion { get; set; } = null!;
    public int? VarianteId { get; set; }
    [Range(1, int.MaxValue)] public int CantidadDeseada { get; set; } = 1;
    [StringLength(300)] public string? Notas { get; set; }
}

/// <summary>Respuesta combinada: agotados automáticos + lista manual.</summary>
public class FaltantesResumenDto
{
    public List<AgotadoDto> Agotados { get; set; } = new();
    public List<FaltanteDto> Manuales { get; set; } = new();
}
