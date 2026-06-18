using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Ncf;

public class SecuenciaNcfDto
{
    public int SecuenciaNcfId { get; set; }
    public string TipoComprobante { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string Serie { get; set; } = null!;
    public int Secuencia { get; set; }
    public int Hasta { get; set; }
    public int Disponibles { get; set; }
    public string? Ejemplo { get; set; }
    public DateTime? Vencimiento { get; set; }
    public bool Activo { get; set; }
}

public class SecuenciaNcfConfigDto
{
    [Required, StringLength(2)] public string Serie { get; set; } = "B";
    /// <summary>Próximo número a asignar (inicio del rango autorizado).</summary>
    [Range(1, int.MaxValue)] public int Secuencia { get; set; } = 1;
    /// <summary>Último número autorizado.</summary>
    [Range(0, int.MaxValue)] public int Hasta { get; set; }
    public DateTime? Vencimiento { get; set; }
    public bool Activo { get; set; }
}
