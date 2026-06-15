using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Taller;

public class OrdenCrearDto
{
    [Required] public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    /// <summary>Equipo del inventario propio; null si el cliente trae su equipo.</summary>
    public int? ImeiId { get; set; }
    [StringLength(200)] public string? EquipoDescripcion { get; set; }
    [StringLength(500)] public string? Diagnostico { get; set; }
    public int? TecnicoId { get; set; }
    [StringLength(30)] public string? NumeroOrden { get; set; }
    [Range(0, double.MaxValue)] public decimal Anticipo { get; set; }
    [Range(0, double.MaxValue)] public decimal CostoEstimado { get; set; }
}

public class OrdenActualizarDto
{
    [StringLength(200)] public string? EquipoDescripcion { get; set; }
    [StringLength(500)] public string? Diagnostico { get; set; }
    public int? TecnicoId { get; set; }
    [StringLength(30)] public string? NumeroOrden { get; set; }
    [Range(0, double.MaxValue)] public decimal Anticipo { get; set; }
    [Range(0, double.MaxValue)] public decimal CostoEstimado { get; set; }
}

public class CambioEstadoDto
{
    /// <summary>Recibido, EnReparacion, Reparado, Entregado o Cancelado.</summary>
    [Required, StringLength(15)] public string Estado { get; set; } = null!;
    /// <summary>Requerido al pasar a Entregado (si no se envía, se usa el costo estimado).</summary>
    [Range(0, double.MaxValue)] public decimal? CostoFinal { get; set; }
    [Range(0, double.MaxValue)] public decimal? ComisionTecnico { get; set; }
}

public class RepuestoAgregarDto
{
    /// <summary>Variante del catálogo (descuenta stock no serializado); null para repuesto externo.</summary>
    public int? VarianteId { get; set; }
    [Required, StringLength(200)] public string Descripcion { get; set; } = null!;
    [Range(1, int.MaxValue)] public int Cantidad { get; set; } = 1;
    [Range(0, double.MaxValue)] public decimal Costo { get; set; }
}

public class FotoAgregarDto
{
    [Required, StringLength(400)] public string Url { get; set; } = null!;
}

public class RepuestoDto
{
    public int Id { get; set; }
    public int? VarianteId { get; set; }
    public string Descripcion { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
}

public class FotoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public DateTime Fecha { get; set; }
}

public class OrdenDto
{
    public int OrdenTallerId { get; set; }
    public string? NumeroOrden { get; set; }
    public int SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public int? ImeiId { get; set; }
    public string? Imei { get; set; }
    public string? EquipoDescripcion { get; set; }
    public string? Diagnostico { get; set; }
    public int? TecnicoId { get; set; }
    public string? Tecnico { get; set; }
    public string Estado { get; set; } = null!;
    public decimal Anticipo { get; set; }
    public decimal CostoEstimado { get; set; }
    public decimal? CostoFinal { get; set; }
    public decimal ComisionTecnico { get; set; }
    public decimal TotalRepuestos { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public List<RepuestoDto> Repuestos { get; set; } = new();
    public List<FotoDto> Fotos { get; set; } = new();
}

/// <summary>Tarjeta resumida para el panel Kanban del taller.</summary>
public class OrdenResumenDto
{
    public int OrdenTallerId { get; set; }
    public string? NumeroOrden { get; set; }
    public string? Cliente { get; set; }
    public string? Equipo { get; set; }
    public string? Tecnico { get; set; }
    public string Estado { get; set; } = null!;
    public decimal CostoEstimado { get; set; }
    public DateTime FechaRecepcion { get; set; }
}

/// <summary>Comisión acumulada de un técnico (sobre órdenes entregadas).</summary>
public class ComisionTecnicoDto
{
    public int TecnicoId { get; set; }
    public string Tecnico { get; set; } = null!;
    public int OrdenesEntregadas { get; set; }
    public decimal TotalComision { get; set; }
}
