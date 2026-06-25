using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Faltantes;

public interface IFaltanteService
{
    Task<FaltantesResumenDto> ListarAsync();
    Task<FaltanteDto> AgregarAsync(FaltanteCrearDto dto);
    Task ResolverAsync(int id);
}

public class FaltanteService : IFaltanteService
{
    private readonly IApplicationDbContext _db;

    public FaltanteService(IApplicationDbContext db) => _db = db;

    /// <summary>Une los rasgos no vacíos de una variante con " · " (en memoria).</summary>
    private static string Unir(params string?[] partes)
        => string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));

    public async Task<FaltantesResumenDto> ListarAsync()
    {
        // Accesorios agotados (no serializados, activos, stock <= 0).
        // Se proyectan los campos crudos y el texto de la variante se arma en
        // memoria, porque EF no puede traducir string.Join dentro del Select.
        var accesorios = (await _db.ProductoVariantes.AsNoTracking()
            .Where(v => v.Activo && v.Producto.Activo && !v.Producto.Serializado && v.StockNoSerial <= 0)
            .Select(v => new
            {
                v.VarianteId,
                Producto = v.Producto.Nombre,
                Marca = v.Producto.Marca!.Nombre,
                v.Color,
                v.Almacenamiento,
                v.StockNoSerial
            })
            .ToListAsync())
            .Select(v => new AgotadoDto
            {
                Tipo = "Accesorio",
                VarianteId = v.VarianteId,
                Producto = v.Producto,
                Marca = v.Marca,
                Variante = Unir(v.Color, v.Almacenamiento),
                Stock = v.StockNoSerial
            })
            .ToList();

        // Dispositivos agotados (serializados, activos, sin IMEI disponible)
        var dispositivos = (await _db.ProductoVariantes.AsNoTracking()
            .Where(v => v.Activo && v.Producto.Activo && v.Producto.Serializado
                && !_db.InventarioImeis.Any(i => i.VarianteId == v.VarianteId && i.Estado == "Disponible"))
            .Select(v => new
            {
                v.VarianteId,
                Producto = v.Producto.Nombre,
                Marca = v.Producto.Marca!.Nombre,
                v.Color,
                v.Almacenamiento,
                v.Condicion
            })
            .ToListAsync())
            .Select(v => new AgotadoDto
            {
                Tipo = "Dispositivo",
                VarianteId = v.VarianteId,
                Producto = v.Producto,
                Marca = v.Marca,
                Variante = Unir(v.Color, v.Almacenamiento, v.Condicion),
                Stock = 0
            })
            .ToList();

        var manuales = await _db.Faltantes.AsNoTracking()
            .Where(f => !f.Resuelto)
            .OrderByDescending(f => f.FechaCreacion)
            .Select(f => new FaltanteDto
            {
                FaltanteId = f.FaltanteId,
                Descripcion = f.Descripcion,
                VarianteId = f.VarianteId,
                CantidadDeseada = f.CantidadDeseada,
                Notas = f.Notas,
                FechaCreacion = f.FechaCreacion
            })
            .ToListAsync();

        return new FaltantesResumenDto
        {
            Agotados = dispositivos.Concat(accesorios).OrderBy(a => a.Producto).ToList(),
            Manuales = manuales
        };
    }

    public async Task<FaltanteDto> AgregarAsync(FaltanteCrearDto dto)
    {
        if (dto.VarianteId.HasValue &&
            !await _db.ProductoVariantes.AnyAsync(v => v.VarianteId == dto.VarianteId.Value))
            throw new FaltanteException("La variante indicada no existe.");

        var faltante = new Faltante
        {
            Descripcion = dto.Descripcion.Trim(),
            VarianteId = dto.VarianteId,
            CantidadDeseada = dto.CantidadDeseada,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            Resuelto = false,
            FechaCreacion = DateTime.Now
        };
        _db.Faltantes.Add(faltante);
        await _db.SaveChangesAsync();

        return new FaltanteDto
        {
            FaltanteId = faltante.FaltanteId,
            Descripcion = faltante.Descripcion,
            VarianteId = faltante.VarianteId,
            CantidadDeseada = faltante.CantidadDeseada,
            Notas = faltante.Notas,
            FechaCreacion = faltante.FechaCreacion
        };
    }

    public async Task ResolverAsync(int id)
    {
        var faltante = await _db.Faltantes.FirstOrDefaultAsync(f => f.FaltanteId == id)
            ?? throw new FaltanteException("El faltante no existe.");
        faltante.Resuelto = true;
        await _db.SaveChangesAsync();
    }
}
