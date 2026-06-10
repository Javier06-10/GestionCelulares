namespace GestionCelulares.Domain.Entities;

public class Marca
{
    public int MarcaId { get; set; }
    public string Nombre { get; set; } = null!;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Categoria
{
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Producto
{
    public int ProductoId { get; set; }
    public int? MarcaId { get; set; }
    public int? CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Serializado { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Marca? Marca { get; set; }
    public Categoria? Categoria { get; set; }
    public ICollection<ProductoVariante> Variantes { get; set; } = new List<ProductoVariante>();
}

public class ProductoVariante
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
    public bool Activo { get; set; }

    public Producto Producto { get; set; } = null!;
    public ICollection<InventarioImei> Imeis { get; set; } = new List<InventarioImei>();
}
