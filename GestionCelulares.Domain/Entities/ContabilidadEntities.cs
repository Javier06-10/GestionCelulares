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

/// <summary>Asiento contable (encabezado del libro diario).</summary>
public class AsientoContable
{
    public int AsientoContableId { get; set; }
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = null!;
    public string Origen { get; set; } = "Manual";   // Manual, Venta, Compra, Caja, Nomina, Ajuste
    public int? ReferenciaId { get; set; }
    public string Estado { get; set; } = "Registrado"; // Registrado, Anulado
    public int? UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }

    public ICollection<AsientoDetalle> Detalles { get; set; } = new List<AsientoDetalle>();
}

/// <summary>Línea de un asiento (débito o crédito a una cuenta).</summary>
public class AsientoDetalle
{
    public int AsientoDetalleId { get; set; }
    public int AsientoContableId { get; set; }
    public int CuentaContableId { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public string? Descripcion { get; set; }

    public AsientoContable Asiento { get; set; } = null!;
    public CuentaContable Cuenta { get; set; } = null!;
}
