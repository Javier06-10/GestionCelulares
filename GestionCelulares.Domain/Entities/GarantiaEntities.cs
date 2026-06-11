namespace GestionCelulares.Domain.Entities;

public class Garantia
{
    public int GarantiaId { get; set; }
    public int? VentaId { get; set; }
    public int? VentaDetalleId { get; set; }
    public int? ImeiId { get; set; }
    public int? ClienteId { get; set; }
    public DateTime FechaInicio { get; set; }
    public int MesesCobertura { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? Condiciones { get; set; }
    public string Estado { get; set; } = "Vigente";
    public DateTime FechaCreacion { get; set; }

    public Venta? Venta { get; set; }
    public InventarioImei? Imei { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<CasoGarantia> Casos { get; set; } = new List<CasoGarantia>();
}

public class CasoGarantia
{
    public int CasoGarantiaId { get; set; }
    public string? NumeroCaso { get; set; }
    public int? GarantiaId { get; set; }
    public int? ImeiId { get; set; }
    public int? ClienteId { get; set; }
    public int? OrdenTallerId { get; set; }
    public DateTime FechaApertura { get; set; }
    public string? TipoFalla { get; set; }
    public string? DescripcionFalla { get; set; }
    public string? TipoResolucion { get; set; }
    public int? ImeiReemplazoId { get; set; }
    public string Estado { get; set; } = "Abierto";
    public DateTime? FechaCierre { get; set; }
    public string? Notas { get; set; }

    public Garantia? Garantia { get; set; }
    public InventarioImei? Imei { get; set; }
    public Cliente? Cliente { get; set; }
    public OrdenTaller? OrdenTaller { get; set; }
    public InventarioImei? ImeiReemplazo { get; set; }
}
