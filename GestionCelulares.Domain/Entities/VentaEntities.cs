namespace GestionCelulares.Domain.Entities;

public class MetodoPago
{
    public int MetodoPagoId { get; set; }
    public string Nombre { get; set; } = null!;
}

public class Venta
{
    public int VentaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Ncf { get; set; }
    public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? SesionCajaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public bool EsCredito { get; set; }
    public string Estado { get; set; } = "Completada";

    public Sucursal Sucursal { get; set; } = null!;
    public Cliente? Cliente { get; set; }
    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    public ICollection<VentaPago> Pagos { get; set; } = new List<VentaPago>();
}

public class VentaDetalle
{
    public int VentaDetalleId { get; set; }
    public int VentaId { get; set; }
    public int? ImeiId { get; set; }
    public int VarianteId { get; set; }
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    public Venta Venta { get; set; } = null!;
    public InventarioImei? Imei { get; set; }
    public ProductoVariante Variante { get; set; } = null!;
}

public class VentaPago
{
    public int VentaPagoId { get; set; }
    public int VentaId { get; set; }
    public int MetodoPagoId { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }

    public Venta Venta { get; set; } = null!;
    public MetodoPago MetodoPago { get; set; } = null!;
}
