using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Proveedores;

public class ProveedorCrearDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(20)] public string? RNC { get; set; }
    [StringLength(30)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
    [StringLength(20)] public string? CondicionPago { get; set; }
    [Range(0, 365)] public int DiasCredito { get; set; }
    [StringLength(300)] public string? NotaCondicion { get; set; }
}

public class ProveedorActualizarDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(20)] public string? RNC { get; set; }
    [StringLength(30)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    [StringLength(20)] public string? CondicionPago { get; set; }
    [Range(0, 365)] public int DiasCredito { get; set; }
    [StringLength(300)] public string? NotaCondicion { get; set; }
}

public class ProveedorDto
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
    public string CondicionPago { get; set; } = "Contado";
    public int DiasCredito { get; set; }
    public string? NotaCondicion { get; set; }
}

public class CompraRegistroDto
{
    [Required] public int SucursalId { get; set; }
    [StringLength(50)] public string? NumeroFactura { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Total { get; set; }
    /// <summary>ITBIS incluido en el total (para el reporte 606). 0 si la compra no lo desglosa.</summary>
    [Range(0, double.MaxValue)] public decimal Itbis { get; set; }
    /// <summary>Tipo de bien/servicio DGII (01-11). Default 09 = compras del costo de venta.</summary>
    [StringLength(2)] public string? TipoBienServicio { get; set; }
    /// <summary>Método de pago de la compra al contado (Efectivo, Tarjeta, Transferencia). Para el 606.</summary>
    public int? MetodoPagoId { get; set; }
    [StringLength(300)] public string? Notas { get; set; }
    /// <summary>true = pago inmediato completo (no afecta el balance); false = a crédito (aumenta lo adeudado).</summary>
    public bool Contado { get; set; }
}

public class CompraDto
{
    public int CompraId { get; set; }
    public int ProveedorId { get; set; }
    public int SucursalId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal? Itbis { get; set; }
    public string? Notas { get; set; }
    public bool Contado { get; set; }
}

public class PagoProveedorRegistroDto
{
    [Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    public int? CompraId { get; set; }
    [StringLength(100)] public string? Referencia { get; set; }
}

public class PagoProveedorDto
{
    public int PagoProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public int? CompraId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }
    public decimal BalanceProveedor { get; set; }
}
