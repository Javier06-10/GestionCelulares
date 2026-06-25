using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Inventario;

public class ImeiRegistroDto
{
    [Required, StringLength(20)] public string Imei { get; set; } = null!;
    [Required] public int VarianteId { get; set; }
    [Required] public int SucursalId { get; set; }
    public int? CompraId { get; set; }
    public decimal PrecioCosto { get; set; }
}

public class ImeiDto
{
    public int ImeiId { get; set; }
    public string Imei { get; set; } = null!;
    public int VarianteId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public string? Color { get; set; }
    public string? Almacenamiento { get; set; }
    public int SucursalId { get; set; }
    public string Estado { get; set; } = null!;
    public decimal PrecioCosto { get; set; }
    public DateTime FechaIngreso { get; set; }
}

public class TransferenciaDto
{
    [Required] public int SucursalDestinoId { get; set; }
}

// ---- Ingreso por lote (varios IMEI de un golpe) ----

public class LoteItemDto
{
    [Required, StringLength(20)] public string Imei { get; set; } = null!;
    [Required] public int VarianteId { get; set; }
}

public class LoteRegistroDto
{
    [Required] public int SucursalId { get; set; }
    public decimal PrecioCosto { get; set; }
    public int? CompraId { get; set; }
    [Required, MinLength(1)] public List<LoteItemDto> Items { get; set; } = new();
}

public class LoteResultadoDto
{
    public int Registrados { get; set; }
    public List<string> Duplicados { get; set; } = new();   // IMEI ya existentes o repetidos dentro del lote
    public List<string> Invalidos { get; set; } = new();    // IMEI vacío o variante inexistente
}

/// <summary>Modelo de solo lectura mapeado a la vista vw_InventarioDisponible.</summary>
public class InventarioDisponibleDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public int VarianteId { get; set; }
    public string? Color { get; set; }
    public string? Almacenamiento { get; set; }
    public string? Condicion { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockMinimo { get; set; }
    public int SucursalId { get; set; }
    public int Disponibles { get; set; }
}
