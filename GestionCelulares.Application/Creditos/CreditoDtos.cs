using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Creditos;

public class CreditoCrearDto
{
    [Required] public int ClienteId { get; set; }
    /// <summary>Principal a financiar (sin la inicial).</summary>
    [Range(0.01, double.MaxValue)] public decimal MontoFinanciado { get; set; }
    [Range(1, 120)] public int NumeroCuotas { get; set; }
    [Required] public DateTime FechaPrimerVencimiento { get; set; }
    public int? VentaId { get; set; }
    [Range(0, double.MaxValue)] public decimal Inicial { get; set; }
    /// <summary>Tasa de interés simple mensual en porcentaje (ej. 5 = 5%).</summary>
    [Range(0, 100)] public decimal TasaInteresMensual { get; set; }
}

public class CuotaDto
{
    public int CuotaId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = null!;
}

public class CreditoDto
{
    public int CreditoId { get; set; }
    public int? VentaId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public decimal MontoFinanciado { get; set; }
    public decimal Inicial { get; set; }
    public decimal TasaInteres { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public List<CuotaDto> Cuotas { get; set; } = new();
}

public class CreditoResumenDto
{
    public int CreditoId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public decimal MontoTotal { get; set; }
    public decimal Saldo { get; set; }
    public int NumeroCuotas { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
}

public class AbonoRegistroDto
{
    [Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public int? SesionCajaId { get; set; }
    /// <summary>Cuota específica a abonar; null = distribuir empezando por la más antigua.</summary>
    public int? CuotaId { get; set; }
    [StringLength(30)] public string? NumeroRecibo { get; set; }
}

/// <summary>Resultado del abono: pago generado y estado del crédito.</summary>
public class AbonoResultadoDto
{
    public int PagoCreditoId { get; set; }
    public int CreditoId { get; set; }
    public decimal MontoAbonado { get; set; }
    public decimal SaldoCredito { get; set; }
    public string EstadoCredito { get; set; } = null!;
}

public class PagoCreditoDto
{
    public int PagoCreditoId { get; set; }
    public int CreditoId { get; set; }
    public int? CuotaId { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public string? NumeroRecibo { get; set; }
    public DateTime Fecha { get; set; }
}

/// <summary>Fila de la vista vw_CuotasVencidas (morosidad en tiempo real).</summary>
public class CuotaVencidaDto
{
    public int CuotaId { get; set; }
    public int CreditoId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public string? Telefono { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal Saldo { get; set; }
    public int DiasVencido { get; set; }
}

/// <summary>Resumen de la corrida de usp_Creditos_ActualizarMora.</summary>
public class MoraResultadoDto
{
    public DateTime FechaCorte { get; set; }
    public int CuotasMarcadasVencidas { get; set; }
    public int CreditosPuestosEnMora { get; set; }
    public int CreditosReactivados { get; set; }
    public int ClientesMorososActualizados { get; set; }
}
