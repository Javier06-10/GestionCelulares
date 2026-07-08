namespace GestionCelulares.Domain.Entities;

public class Proveedor
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? RNC { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public decimal Balance { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Condiciones de pago acordadas con el proveedor
    public string CondicionPago { get; set; } = "Contado";  // Contado | Credito | Acuerdo
    public int DiasCredito { get; set; }                     // días de crédito (si Credito)
    public string? NotaCondicion { get; set; }               // detalle del acuerdo (si Acuerdo)

    // Contacto directo (persona de la empresa proveedora)
    public string? ContactoNombre { get; set; }
    public string? ContactoCargo { get; set; }
    public string? ContactoTelefono { get; set; }
    public string? ContactoEmail { get; set; }

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<PagoProveedor> Pagos { get; set; } = new List<PagoProveedor>();
}

public class Compra
{
    public int CompraId { get; set; }
    public int ProveedorId { get; set; }
    public int SucursalId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal? Subtotal { get; set; }            // base sin ITBIS (para el 606)
    public decimal? Itbis { get; set; }               // ITBIS de la compra (para el 606)
    public string? TipoBienServicio { get; set; }     // código DGII 01-11 (default 09)
    public int? MetodoPagoId { get; set; }            // método de pago si fue al contado (para el 606)
    public string? Notas { get; set; }

    public Proveedor Proveedor { get; set; } = null!;
    public Sucursal Sucursal { get; set; } = null!;
    public MetodoPago? MetodoPago { get; set; }
}

public class PagoProveedor
{
    public int PagoProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public int? CompraId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }

    public Proveedor Proveedor { get; set; } = null!;
    public Compra? Compra { get; set; }
}
