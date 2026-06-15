using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Nomina;

/// <summary>Empleado disponible para registrarle un pago.</summary>
public class EmpleadoDto
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string Rol { get; set; } = null!;
}

public class PagoEmpleadoDto
{
    public int PagoEmpleadoId { get; set; }
    public int EmpleadoId { get; set; }
    public string Empleado { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public decimal Monto { get; set; }
    public string? Periodo { get; set; }
    public string? Notas { get; set; }
    public DateTime Fecha { get; set; }
}

public class PagoEmpleadoCrearDto
{
    [Required] public int EmpleadoId { get; set; }
    [Required, StringLength(20)] public string Tipo { get; set; } = "Salario";
    [Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    [StringLength(40)] public string? Periodo { get; set; }
    [StringLength(300)] public string? Notas { get; set; }
}

/// <summary>Lista de pagos + total del período mostrado.</summary>
public class NominaResumenDto
{
    public List<PagoEmpleadoDto> Pagos { get; set; } = new();
    public decimal TotalPagado { get; set; }
}
