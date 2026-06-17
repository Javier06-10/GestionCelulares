using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Apartados;

/// <summary>Equipo serializado disponible para apartar.</summary>
public class EquipoApartableDto
{
    public int ImeiId { get; set; }
    public string Imei { get; set; } = null!;
    public int VarianteId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public string? Variante { get; set; }
    public decimal PrecioVenta { get; set; }
}

public class ApartadoCrearDto
{
    [Required] public int ClienteId { get; set; }
    [Required] public int ImeiId { get; set; }
    /// <summary>Precio total acordado que pagará el cliente (con impuestos incluidos).</summary>
    [Range(0.01, double.MaxValue)] public decimal PrecioTotal { get; set; }
    /// <summary>Abono inicial opcional.</summary>
    [Range(0, double.MaxValue)] public decimal AbonoInicial { get; set; }
    public int? MetodoPagoId { get; set; }
    [StringLength(300)] public string? Notas { get; set; }
}

public class AbonoApartadoCrearDto
{
    [Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
}

public class CambiarEquipoDto
{
    [Required] public int ImeiId { get; set; }
    /// <summary>Nuevo precio acordado (si cambia respecto al equipo anterior).</summary>
    [Range(0.01, double.MaxValue)] public decimal PrecioTotal { get; set; }
}

public class CancelarApartadoDto
{
    /// <summary>Monto a devolver en efectivo (egreso de caja). Solo el administrador puede usarlo.</summary>
    [Range(0, double.MaxValue)] public decimal DevolverMonto { get; set; }
    [StringLength(200)] public string? Motivo { get; set; }
}

public class AbonoApartadoDto
{
    public int AbonoApartadoId { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string Tipo { get; set; } = null!;
    public DateTime Fecha { get; set; }
}

public class ApartadoDto
{
    public int ApartadoId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public int VarianteId { get; set; }
    public string Equipo { get; set; } = null!;
    public decimal PrecioTotal { get; set; }
    public decimal TotalAbonado { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = null!;
    public int? VentaId { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public List<AbonoApartadoDto> Abonos { get; set; } = new();
}

public class ApartadoResumenDto
{
    public int ApartadoId { get; set; }
    public string Cliente { get; set; } = null!;
    public string Equipo { get; set; } = null!;
    public decimal PrecioTotal { get; set; }
    public decimal TotalAbonado { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
}
