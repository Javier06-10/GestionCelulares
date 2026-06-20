using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Ventas;

public class VentaDetalleRegistroDto
{
    /// <summary>Id del equipo en inventario; null si es un accesorio no serializado.</summary>
    public int? ImeiId { get; set; }
    [Required] public int VarianteId { get; set; }
    [Range(1, int.MaxValue)] public int Cantidad { get; set; } = 1;
    [Range(0, double.MaxValue)] public decimal PrecioUnitario { get; set; }
    [Range(0, double.MaxValue)] public decimal Descuento { get; set; }
}

public class VentaRegistroDto
{
    [Required] public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public bool EsCredito { get; set; }
    /// <summary>Requerido cuando la venta es de contado.</summary>
    public int? MetodoPagoId { get; set; }
    /// <summary>Tipo de comprobante: 01 Crédito Fiscal, 02 Consumo. Si es null se infiere por el cliente.</summary>
    [StringLength(2)] public string? TipoComprobante { get; set; }
    [StringLength(30)] public string? NumeroFactura { get; set; }
    [MinLength(1)] public List<VentaDetalleRegistroDto> Detalles { get; set; } = new();
}

public class VentaDetalleDto
{
    public int VentaDetalleId { get; set; }
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public int VarianteId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Color { get; set; }
    public string? Almacenamiento { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
}

public class VentaPagoDto
{
    public int MetodoPagoId { get; set; }
    public string MetodoPago { get; set; } = null!;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}

public class VentaDto
{
    public int VentaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Ncf { get; set; }
    public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public int UsuarioId { get; set; }
    public int? SesionCajaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public bool EsCredito { get; set; }
    public string Estado { get; set; } = null!;
    public List<VentaDetalleDto> Detalles { get; set; } = new();
    public List<VentaPagoDto> Pagos { get; set; } = new();
}

/// <summary>Cabecera resumida para listados del POS.</summary>
public class VentaResumenDto
{
    public int VentaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Ncf { get; set; }
    public int SucursalId { get; set; }
    public string? Cliente { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public bool EsCredito { get; set; }
    public string Estado { get; set; } = null!;
}

public class MetodoPagoDto
{
    public int MetodoPagoId { get; set; }
    public string Nombre { get; set; } = null!;
}

public class AnularVentaDto
{
    /// <summary>Código de tipo de anulación DGII (01-09).</summary>
    [Required, StringLength(2)] public string TipoAnulacion { get; set; } = null!;
    [StringLength(200)] public string? Motivo { get; set; }
}
