using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Contabilidad;

public class CuentaDto
{
    public int CuentaContableId { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int? CuentaPadreId { get; set; }
    public string Naturaleza { get; set; } = null!;
    public bool PermiteMovimiento { get; set; }
    public bool EsSistema { get; set; }
    public bool Activo { get; set; }
    public int Nivel { get; set; }   // profundidad en el árbol (para la indentación)
}

public class CuentaCrearDto
{
    [Required, StringLength(20)] public string Codigo { get; set; } = null!;
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    /// <summary>Cuenta padre. Si es null, debe indicarse el Tipo (cuenta raíz).</summary>
    public int? CuentaPadreId { get; set; }
    /// <summary>Requerido solo si no hay padre (la sub-cuenta hereda el tipo del padre).</summary>
    [StringLength(15)] public string? Tipo { get; set; }
    [StringLength(10)] public string? Naturaleza { get; set; }
    public bool PermiteMovimiento { get; set; } = true;
}

public class CuentaActualizarDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    public bool PermiteMovimiento { get; set; } = true;
    public bool Activo { get; set; } = true;
}
