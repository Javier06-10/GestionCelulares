namespace GestionCelulares.Domain.Entities;

/// <summary>Pago a un empleado (salario, adelanto, bono o comisión).</summary>
public class PagoEmpleado
{
    public int PagoEmpleadoId { get; set; }
    public int EmpleadoId { get; set; }
    public string Tipo { get; set; } = "Salario";
    public decimal Monto { get; set; }
    public string? Periodo { get; set; }
    public string? Notas { get; set; }
    public int? RegistradoPor { get; set; }
    public DateTime Fecha { get; set; }

    public Usuario Empleado { get; set; } = null!;
}
