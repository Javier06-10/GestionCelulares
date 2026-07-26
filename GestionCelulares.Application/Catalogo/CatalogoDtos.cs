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
    /// <summary>URL de la imagen del producto (subida a un store externo, p. ej. Cloudinary).</summary>
    [StringLength(500)] public string? ImagenUrl { get; set; }
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
    [StringLength(500)] public string? ImagenUrl { get; set; }
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
    public bool Provisional { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<VarianteDto> Variantes { get; set; } = new();
}

/// <summary>Creación rápida desde el POS: solo nombre, precio y (opcional) código de barras.</summary>
public class ProductoRapidoDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [Range(0, double.MaxValue)] public decimal PrecioVenta { get; set; }
    [StringLength(50)] public string? CodigoBarras { get; set; }
    [Range(1, 9999)] public int Cantidad { get; set; } = 1;
}

/// <summary>Resultado de la creación rápida (para agregar al carrito del POS).</summary>
public class ProductoRapidoResultadoDto
{
    public int ProductoId { get; set; }
    public int VarianteId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioVenta { get; set; }
}
