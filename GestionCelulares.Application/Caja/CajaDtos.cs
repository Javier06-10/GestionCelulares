using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Caja;

public class AperturaCajaDto
{
    [Required] public int SucursalId { get; set; }
    [Range(0, double.MaxValue)] public decimal MontoApertura { get; set; }
}

public class SesionCajaDto
{
    public int SesionCajaId { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioApertura { get; set; }
    public int? UsuarioCierre { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal? MontoCierre { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalEgresos { get; set; }
}

public class MovimientoCajaRegistroDto
{
    /// <summary>"Ingreso" o "Egreso".</summary>
    [Required, StringLength(10)] public string Tipo { get; set; } = null!;
    [Required, StringLength(150)] public string Concepto { get; set; } = null!;
    [Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    [StringLength(100)] public string? Referencia { get; set; }
}

public class MovimientoCajaDto
{
    public long MovimientoCajaId { get; set; }
    public int SesionCajaId { get; set; }
    public string Tipo { get; set; } = null!;
    public string Concepto { get; set; } = null!;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; }
}

public class CierreCajaDto
{
    [Range(0, double.MaxValue)] public decimal MontoContado { get; set; }
}

/// <summary>Resultado del arqueo devuelto por usp_Caja_Cerrar.</summary>
public class CierreCajaResultadoDto
{
    public decimal MontoEsperado { get; set; }
    public decimal MontoContado { get; set; }
    public decimal Diferencia { get; set; }
}
