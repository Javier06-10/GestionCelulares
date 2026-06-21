namespace GestionCelulares.Domain.Entities;

/// <summary>Cuenta del catálogo contable (plan de cuentas).</summary>
public class CuentaContable
{
    public int CuentaContableId { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;          // Activo, Pasivo, Capital, Ingreso, Costo, Gasto
    public int? CuentaPadreId { get; set; }
    public string Naturaleza { get; set; } = null!;     // Deudora / Acreedora
    public bool PermiteMovimiento { get; set; } = true;
    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;

    public CuentaContable? CuentaPadre { get; set; }
    public ICollection<CuentaContable> Hijas { get; set; } = new List<CuentaContable>();
}
