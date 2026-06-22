using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Contabilidad;

public class AsientoDetalleCrearDto
{
    [Required] public int CuentaContableId { get; set; }
    [Range(0, double.MaxValue)] public decimal Debito { get; set; }
    [Range(0, double.MaxValue)] public decimal Credito { get; set; }
    [StringLength(200)] public string? Descripcion { get; set; }
}

public class AsientoCrearDto
{
    [Required] public DateTime Fecha { get; set; }
    [Required, StringLength(300)] public string Concepto { get; set; } = null!;
    [MinLength(2)] public List<AsientoDetalleCrearDto> Detalles { get; set; } = new();
}

public class AsientoDetalleDto
{
    public int AsientoDetalleId { get; set; }
    public int CuentaContableId { get; set; }
    public string CuentaCodigo { get; set; } = null!;
    public string CuentaNombre { get; set; } = null!;
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public string? Descripcion { get; set; }
}

public class AsientoDto
{
    public int AsientoContableId { get; set; }
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = null!;
    public string Origen { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public decimal TotalDebito { get; set; }
    public decimal TotalCredito { get; set; }
    public List<AsientoDetalleDto> Detalles { get; set; } = new();
}

public class AsientoResumenDto
{
    public int AsientoContableId { get; set; }
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = null!;
    public string Origen { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public decimal Total { get; set; }
}

/// <summary>Una línea del balance de comprobación.</summary>
public class BalanceLineaDto
{
    public int CuentaContableId { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public decimal SaldoDeudor { get; set; }
    public decimal SaldoAcreedor { get; set; }
}

public class BalanceComprobacionDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalDebito { get; set; }
    public decimal TotalCredito { get; set; }
    public decimal TotalSaldoDeudor { get; set; }
    public decimal TotalSaldoAcreedor { get; set; }
    public bool Cuadrado { get; set; }
    public List<BalanceLineaDto> Lineas { get; set; } = new();
}
