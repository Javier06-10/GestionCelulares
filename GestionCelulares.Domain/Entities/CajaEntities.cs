namespace GestionCelulares.Domain.Entities;

public class SesionCaja
{
    public int SesionCajaId { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioApertura { get; set; }
    public int? UsuarioCierre { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal? MontoCierre { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = "Abierta";
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }

    public Sucursal Sucursal { get; set; } = null!;
    public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
}

public class MovimientoCaja
{
    public long MovimientoCajaId { get; set; }
    public int SesionCajaId { get; set; }
    public string Tipo { get; set; } = null!;
    public string Concepto { get; set; } = null!;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; }

    public SesionCaja Sesion { get; set; } = null!;
}
