using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Garantias;

public class GarantiaCrearDto
{
    public int? VentaId { get; set; }
    public int? VentaDetalleId { get; set; }
    public int? ImeiId { get; set; }
    public int? ClienteId { get; set; }
    /// <summary>Si no se envía, inicia hoy.</summary>
    public DateTime? FechaInicio { get; set; }
    [Range(1, 60)] public int MesesCobertura { get; set; } = 3;
    [StringLength(400)] public string? Condiciones { get; set; }
}

public class GarantiaDto
{
    public int GarantiaId { get; set; }
    public int? VentaId { get; set; }
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public int? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public DateTime FechaInicio { get; set; }
    public int MesesCobertura { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? Condiciones { get; set; }
    public string Estado { get; set; } = null!;
    public bool Vigente { get; set; }
}

/// <summary>Fila de la vista vw_GarantiaVigentePorImei.</summary>
public class GarantiaVigenteDto
{
    public int GarantiaId { get; set; }
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public int? ClienteId { get; set; }
    public int? VentaId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int MesesCobertura { get; set; }
    public string Estado { get; set; } = null!;
    public int Vigente { get; set; }
}

public class CasoCrearDto
{
    public int? GarantiaId { get; set; }
    public int? ImeiId { get; set; }
    public int? ClienteId { get; set; }
    public int? OrdenTallerId { get; set; }
    [StringLength(30)] public string? NumeroCaso { get; set; }
    [StringLength(120)] public string? TipoFalla { get; set; }
    [StringLength(500)] public string? DescripcionFalla { get; set; }
}

public class CasoResolverDto
{
    /// <summary>Reparacion, Reemplazo, NotaCredito o Rechazado.</summary>
    [Required, StringLength(15)] public string TipoResolucion { get; set; } = null!;
    /// <summary>Requerido cuando la resolución es Reemplazo (IMEI disponible que se entrega).</summary>
    public int? ImeiReemplazoId { get; set; }
    [StringLength(500)] public string? Notas { get; set; }
}

public class CasoDto
{
    public int CasoGarantiaId { get; set; }
    public string? NumeroCaso { get; set; }
    public int? GarantiaId { get; set; }
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public int? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public int? OrdenTallerId { get; set; }
    public DateTime FechaApertura { get; set; }
    public string? TipoFalla { get; set; }
    public string? DescripcionFalla { get; set; }
    public string? TipoResolucion { get; set; }
    public int? ImeiReemplazoId { get; set; }
    public string? ImeiReemplazo { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime? FechaCierre { get; set; }
    public string? Notas { get; set; }
}

/// <summary>Fila de la vista vw_IndiceFallasPorModelo.</summary>
public class IndiceFallaDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public int TotalCasos { get; set; }
}
