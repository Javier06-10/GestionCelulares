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
    public string? Notas { get; set; }

    public Proveedor Proveedor { get; set; } = null!;
    public Sucursal Sucursal { get; set; } = null!;
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
