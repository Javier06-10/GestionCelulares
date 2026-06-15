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
    public string? UsuarioAperturaNombre { get; set; }
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

/// <summary>Desglose de un método de pago dentro del turno.</summary>
public class ResumenMetodoDto
{
    public string Metodo { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Monto { get; set; }
}

/// <summary>Resumen de todo lo que hizo el empleado durante el turno (sesión de caja).</summary>
public class ResumenTurnoDto
{
    public int SesionCajaId { get; set; }
    public string? Empleado { get; set; }
    public DateTime FechaApertura { get; set; }
    public decimal MontoApertura { get; set; }

    public int VentasCantidad { get; set; }
    public decimal VentasTotal { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal VentasOtros { get; set; }

    public int AbonosCantidad { get; set; }
    public decimal AbonosTotal { get; set; }
    public decimal AbonosEfectivo { get; set; }

    public int TallerRecepciones { get; set; }
    public decimal TallerAnticipos { get; set; }

    public decimal TotalIngresos { get; set; }   // movimientos manuales
    public decimal TotalEgresos { get; set; }

    /// <summary>Efectivo que debería haber en caja (apertura + cobros en efectivo + anticipos + ingresos - egresos).</summary>
    public decimal EfectivoEsperado { get; set; }

    public List<ResumenMetodoDto> VentasPorMetodo { get; set; } = new();
}
