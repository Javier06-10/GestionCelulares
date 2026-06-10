namespace GestionCelulares.Domain.Entities;

public class InventarioImei
{
    public int ImeiId { get; set; }
    public string Imei { get; set; } = null!;
    public int VarianteId { get; set; }
    public int SucursalId { get; set; }
    public int? CompraId { get; set; }
    public decimal PrecioCosto { get; set; }
    public string Estado { get; set; } = "Disponible";
    public DateTime FechaIngreso { get; set; }

    public ProductoVariante Variante { get; set; } = null!;
    public Sucursal Sucursal { get; set; } = null!;
}

public class MovimientoInventario
{
    public long MovimientoId { get; set; }
    public int ImeiId { get; set; }
    public string Tipo { get; set; } = null!;
    public int? SucursalOrigen { get; set; }
    public int? SucursalDestino { get; set; }
    public string? Referencia { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime Fecha { get; set; }

    public InventarioImei Imei { get; set; } = null!;
}
