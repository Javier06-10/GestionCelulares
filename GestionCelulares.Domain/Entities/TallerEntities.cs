namespace GestionCelulares.Domain.Entities;

public class OrdenTaller
{
    public int OrdenTallerId { get; set; }
    public string? NumeroOrden { get; set; }
    public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public int? ImeiId { get; set; }
    public string? EquipoDescripcion { get; set; }
    public string? Diagnostico { get; set; }
    public int? TecnicoId { get; set; }
    public string Estado { get; set; } = "Recibido";
    public decimal Anticipo { get; set; }
    public decimal CostoEstimado { get; set; }
    public decimal? CostoFinal { get; set; }
    public decimal ComisionTecnico { get; set; }
    public int? SesionCajaId { get; set; }
    public int? UsuarioRecepcion { get; set; }
    public int? SesionCajaEntrega { get; set; }
    public int? MetodoPagoAnticipoId { get; set; }
    public int? MetodoPagoEntregaId { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEntrega { get; set; }

    public Sucursal Sucursal { get; set; } = null!;
    public Cliente? Cliente { get; set; }
    public InventarioImei? Imei { get; set; }
    public Usuario? Tecnico { get; set; }
    public Usuario? Recepcionista { get; set; }
    public MetodoPago? MetodoPagoAnticipo { get; set; }
    public MetodoPago? MetodoPagoEntrega { get; set; }
    public ICollection<OrdenTallerFoto> Fotos { get; set; } = new List<OrdenTallerFoto>();
    public ICollection<OrdenTallerRepuesto> Repuestos { get; set; } = new List<OrdenTallerRepuesto>();
}

public class OrdenTallerFoto
{
    public int Id { get; set; }
    public int OrdenTallerId { get; set; }
    public string Url { get; set; } = null!;
    public DateTime Fecha { get; set; }

    public OrdenTaller Orden { get; set; } = null!;
}

public class OrdenTallerRepuesto
{
    public int Id { get; set; }
    public int OrdenTallerId { get; set; }
    public int? VarianteId { get; set; }
    public string Descripcion { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }

    public OrdenTaller Orden { get; set; } = null!;
    public ProductoVariante? Variante { get; set; }
}
