namespace GestionCelulares.Domain.Entities;

public class Credito
{
    public int CreditoId { get; set; }
    public int? VentaId { get; set; }
    public int ClienteId { get; set; }
    public decimal MontoFinanciado { get; set; }
    public decimal Inicial { get; set; }
    public decimal TasaInteres { get; set; }
    public int NumeroCuotas { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = "Activo";
    public DateTime FechaInicio { get; set; }

    public Venta? Venta { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    public ICollection<PagoCredito> Pagos { get; set; } = new List<PagoCredito>();
}

public class Cuota
{
    public int CuotaId { get; set; }
    public int CreditoId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = "Pendiente";

    public Credito Credito { get; set; } = null!;
}

public class PagoCredito
{
    public int PagoCreditoId { get; set; }
    public int CreditoId { get; set; }
    public int? CuotaId { get; set; }
    public int? MetodoPagoId { get; set; }
    public int? SesionCajaId { get; set; }
    public int? UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public string? NumeroRecibo { get; set; }
    public DateTime Fecha { get; set; }

    public Credito Credito { get; set; } = null!;
    public Cuota? Cuota { get; set; }
    public MetodoPago? MetodoPago { get; set; }
}
