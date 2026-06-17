namespace GestionCelulares.Domain.Entities;

/// <summary>Equipo apartado (layaway): el cliente abona poco a poco y se lleva el equipo al pagarlo.</summary>
public class Apartado
{
    public int ApartadoId { get; set; }
    public int ClienteId { get; set; }
    public int? ImeiId { get; set; }
    public int VarianteId { get; set; }
    public int SucursalId { get; set; }
    public int? UsuarioId { get; set; }
    public decimal PrecioTotal { get; set; }
    public decimal TotalAbonado { get; set; }
    public string Estado { get; set; } = "Activo";  // Activo, Completado, Cancelado
    public int? VentaId { get; set; }                 // venta formal generada al completar
    public string? Notas { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public ProductoVariante Variante { get; set; } = null!;
    public InventarioImei? Imei { get; set; }
    public ICollection<AbonoApartado> Abonos { get; set; } = new List<AbonoApartado>();
}

public class AbonoApartado
{
    public int AbonoApartadoId { get; set; }
    public int ApartadoId { get; set; }
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public int? SesionCajaId { get; set; }
    public int? UsuarioId { get; set; }
    public string Tipo { get; set; } = "Abono";  // Abono o Devolucion
    public DateTime Fecha { get; set; }

    public Apartado Apartado { get; set; } = null!;
    public MetodoPago? MetodoPago { get; set; }
}
