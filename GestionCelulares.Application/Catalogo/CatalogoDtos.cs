using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Catalogo;

public class NombreDto
{
    [Required, StringLength(100)] public string Nombre { get; set; } = null!;
}

public class MarcaDto
{
    public int MarcaId { get; set; }
    public string Nombre { get; set; } = null!;
}

public class CategoriaDto
{
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
}

public class ProductoCrearDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(500)] public string? Descripcion { get; set; }
    public int? MarcaId { get; set; }
    public int? CategoriaId { get; set; }
    /// <summary>true = se controla por IMEI; false = accesorio con stock agregado.</summary>
    public bool Serializado { get; set; } = true;
    public List<VarianteCrearDto> Variantes { get; set; } = new();
}

public class ProductoActualizarDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(500)] public string? Descripcion { get; set; }
    public int? MarcaId { get; set; }
    public int? CategoriaId { get; set; }
    public bool Serializado { get; set; } = true;
    public bool Activo { get; set; } = true;
}

public class VarianteCrearDto
{
    [StringLength(50)] public string? Color { get; set; }
    [StringLength(50)] public string? Almacenamiento { get; set; }
    [StringLength(50)] public string? Condicion { get; set; }
    [StringLength(50)] public string? CodigoBarras { get; set; }
    [Range(0, double.MaxValue)] public decimal PrecioVenta { get; set; }
    [Range(0, double.MaxValue)] public decimal PrecioCosto { get; set; }
    public int StockNoSerial { get; set; }
    public int StockMinimo { get; set; }
}

public class VarianteActualizarDto
{
    [StringLength(50)] public string? Color { get; set; }
    [StringLength(50)] public string? Almacenamiento { get; set; }
    [StringLength(50)] public string? Condicion { get; set; }
    [StringLength(50)] public string? CodigoBarras { get; set; }
    [Range(0, double.MaxValue)] public decimal PrecioVenta { get; set; }
    [Range(0, double.MaxValue)] public decimal PrecioCosto { get; set; }
    public int StockNoSerial { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; } = true;
}

public class VarianteDto
{
    public int VarianteId { get; set; }
    public int ProductoId { get; set; }
    public string? Color { get; set; }
    public string? Almacenamiento { get; set; }
    public string? Condicion { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public int StockNoSerial { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Resultado de la generación masiva de códigos de barras.</summary>
public class GeneracionCodigosDto
{
    public int Generados { get; set; }
}

public class ProductoDto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int? MarcaId { get; set; }
    public string? Marca { get; set; }
    public int? CategoriaId { get; set; }
    public string? Categoria { get; set; }
    public bool Serializado { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<VarianteDto> Variantes { get; set; } = new();
}
