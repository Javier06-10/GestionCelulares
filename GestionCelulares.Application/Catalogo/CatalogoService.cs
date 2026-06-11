using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Catalogo;

public interface ICatalogoService
{
    // Marcas y categorías
    Task<IReadOnlyList<MarcaDto>> MarcasAsync();
    Task<MarcaDto> CrearMarcaAsync(NombreDto dto);
    Task<IReadOnlyList<CategoriaDto>> CategoriasAsync();
    Task<CategoriaDto> CrearCategoriaAsync(NombreDto dto);

    // Productos y variantes
    Task<IReadOnlyList<ProductoDto>> BuscarProductosAsync(string? termino, bool? activos);
    Task<ProductoDto?> ProductoPorIdAsync(int id);
    Task<ProductoDto> CrearProductoAsync(ProductoCrearDto dto);
    Task<ProductoDto> ActualizarProductoAsync(int id, ProductoActualizarDto dto);
    Task<VarianteDto> AgregarVarianteAsync(int productoId, VarianteCrearDto dto);
    Task<VarianteDto> ActualizarVarianteAsync(int varianteId, VarianteActualizarDto dto);
}

public class CatalogoService : ICatalogoService
{
    private readonly IApplicationDbContext _db;

    public CatalogoService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcaDto>> MarcasAsync()
        => await _db.Marcas.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto { MarcaId = m.MarcaId, Nombre = m.Nombre })
            .ToListAsync();

    public async Task<MarcaDto> CrearMarcaAsync(NombreDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (await _db.Marcas.AnyAsync(m => m.Nombre == nombre))
            throw new CatalogoException($"Ya existe la marca '{nombre}'.");

        var marca = new Marca { Nombre = nombre };
        _db.Marcas.Add(marca);
        await _db.SaveChangesAsync();
        return new MarcaDto { MarcaId = marca.MarcaId, Nombre = marca.Nombre };
    }

    public async Task<IReadOnlyList<CategoriaDto>> CategoriasAsync()
        => await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto { CategoriaId = c.CategoriaId, Nombre = c.Nombre })
            .ToListAsync();

    public async Task<CategoriaDto> CrearCategoriaAsync(NombreDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (await _db.Categorias.AnyAsync(c => c.Nombre == nombre))
            throw new CatalogoException($"Ya existe la categoría '{nombre}'.");

        var categoria = new Categoria { Nombre = nombre };
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();
        return new CategoriaDto { CategoriaId = categoria.CategoriaId, Nombre = categoria.Nombre };
    }

    public async Task<IReadOnlyList<ProductoDto>> BuscarProductosAsync(string? termino, bool? activos)
    {
        var q = _db.Productos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            termino = termino.Trim();
            q = q.Where(p =>
                p.Nombre.Contains(termino) ||
                (p.Marca != null && p.Marca.Nombre.Contains(termino)));
        }

        if (activos.HasValue)
            q = q.Where(p => p.Activo == activos.Value);

        return await q.OrderBy(p => p.Nombre).Select(Proyeccion).ToListAsync();
    }

    public async Task<ProductoDto?> ProductoPorIdAsync(int id)
        => await _db.Productos.AsNoTracking()
            .Where(p => p.ProductoId == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

    public async Task<ProductoDto> CrearProductoAsync(ProductoCrearDto dto)
    {
        await ValidarReferenciasAsync(dto.MarcaId, dto.CategoriaId);

        var producto = new Producto
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = Normalizar(dto.Descripcion),
            MarcaId = dto.MarcaId,
            CategoriaId = dto.CategoriaId,
            Serializado = dto.Serializado,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        foreach (var v in dto.Variantes)
            producto.Variantes.Add(NuevaVariante(v));

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return (await ProductoPorIdAsync(producto.ProductoId))!;
    }

    public async Task<ProductoDto> ActualizarProductoAsync(int id, ProductoActualizarDto dto)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.ProductoId == id)
            ?? throw new CatalogoException("El producto no existe.");

        await ValidarReferenciasAsync(dto.MarcaId, dto.CategoriaId);

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = Normalizar(dto.Descripcion);
        producto.MarcaId = dto.MarcaId;
        producto.CategoriaId = dto.CategoriaId;
        producto.Serializado = dto.Serializado;
        producto.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return (await ProductoPorIdAsync(id))!;
    }

    public async Task<VarianteDto> AgregarVarianteAsync(int productoId, VarianteCrearDto dto)
    {
        if (!await _db.Productos.AnyAsync(p => p.ProductoId == productoId))
            throw new CatalogoException("El producto no existe.");

        var variante = NuevaVariante(dto);
        variante.ProductoId = productoId;
        _db.ProductoVariantes.Add(variante);
        await _db.SaveChangesAsync();

        return ProyectarVariante(variante);
    }

    public async Task<VarianteDto> ActualizarVarianteAsync(int varianteId, VarianteActualizarDto dto)
    {
        var variante = await _db.ProductoVariantes.FirstOrDefaultAsync(v => v.VarianteId == varianteId)
            ?? throw new CatalogoException("La variante no existe.");

        variante.Color = Normalizar(dto.Color);
        variante.Almacenamiento = Normalizar(dto.Almacenamiento);
        variante.Condicion = Normalizar(dto.Condicion);
        variante.CodigoBarras = Normalizar(dto.CodigoBarras);
        variante.PrecioVenta = dto.PrecioVenta;
        variante.PrecioCosto = dto.PrecioCosto;
        variante.StockNoSerial = dto.StockNoSerial;
        variante.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return ProyectarVariante(variante);
    }

    private async Task ValidarReferenciasAsync(int? marcaId, int? categoriaId)
    {
        if (marcaId.HasValue && !await _db.Marcas.AnyAsync(m => m.MarcaId == marcaId.Value))
            throw new CatalogoException("La marca indicada no existe.");

        if (categoriaId.HasValue && !await _db.Categorias.AnyAsync(c => c.CategoriaId == categoriaId.Value))
            throw new CatalogoException("La categoría indicada no existe.");
    }

    private static ProductoVariante NuevaVariante(VarianteCrearDto v) => new()
    {
        Color = Normalizar(v.Color),
        Almacenamiento = Normalizar(v.Almacenamiento),
        Condicion = Normalizar(v.Condicion),
        CodigoBarras = Normalizar(v.CodigoBarras),
        PrecioVenta = v.PrecioVenta,
        PrecioCosto = v.PrecioCosto,
        StockNoSerial = v.StockNoSerial,
        Activo = true
    };

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    // Expresión (no método) para que EF Core la traduzca a SQL y cargue marca, categoría y variantes
    private static readonly System.Linq.Expressions.Expression<Func<Producto, ProductoDto>> Proyeccion = p => new ProductoDto
    {
        ProductoId = p.ProductoId,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        MarcaId = p.MarcaId,
        Marca = p.Marca == null ? null : p.Marca.Nombre,
        CategoriaId = p.CategoriaId,
        Categoria = p.Categoria == null ? null : p.Categoria.Nombre,
        Serializado = p.Serializado,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        Variantes = p.Variantes
            .OrderBy(v => v.VarianteId)
            .Select(v => new VarianteDto
            {
                VarianteId = v.VarianteId,
                ProductoId = v.ProductoId,
                Color = v.Color,
                Almacenamiento = v.Almacenamiento,
                Condicion = v.Condicion,
                CodigoBarras = v.CodigoBarras,
                PrecioVenta = v.PrecioVenta,
                PrecioCosto = v.PrecioCosto,
                StockNoSerial = v.StockNoSerial,
                Activo = v.Activo
            })
            .ToList()
    };

    private static VarianteDto ProyectarVariante(ProductoVariante v) => new()
    {
        VarianteId = v.VarianteId,
        ProductoId = v.ProductoId,
        Color = v.Color,
        Almacenamiento = v.Almacenamiento,
        Condicion = v.Condicion,
        CodigoBarras = v.CodigoBarras,
        PrecioVenta = v.PrecioVenta,
        PrecioCosto = v.PrecioCosto,
        StockNoSerial = v.StockNoSerial,
        Activo = v.Activo
    };
}
