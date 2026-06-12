using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Inventario;

public interface IInventarioService
{
    Task<IReadOnlyList<InventarioDisponibleDto>> DisponiblesAsync(int? sucursalId);
    Task<IReadOnlyList<ImeiDto>> ImeisDisponiblesAsync(int varianteId, int? sucursalId);
    Task<ImeiDto?> PorImeiAsync(string imei);
    Task<ImeiDto?> PorIdAsync(int id);
    Task<ImeiDto> RegistrarAsync(ImeiRegistroDto dto, int? usuarioId);
    Task TransferirAsync(int imeiId, int sucursalDestinoId, int? usuarioId);
}

public class InventarioService : IInventarioService
{
    private readonly IApplicationDbContext _db;

    public InventarioService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<InventarioDisponibleDto>> DisponiblesAsync(int? sucursalId)
    {
        var q = _db.InventarioDisponible.AsNoTracking();
        if (sucursalId.HasValue)
            q = q.Where(x => x.SucursalId == sucursalId.Value);
        return await q.OrderBy(x => x.Producto).ToListAsync();
    }

    public async Task<IReadOnlyList<ImeiDto>> ImeisDisponiblesAsync(int varianteId, int? sucursalId)
    {
        return await _db.InventarioImeis.AsNoTracking()
            .Where(i => i.VarianteId == varianteId && i.Estado == "Disponible"
                && (!sucursalId.HasValue || i.SucursalId == sucursalId.Value))
            .OrderBy(i => i.FechaIngreso)
            .Select(i => new ImeiDto
            {
                ImeiId = i.ImeiId,
                Imei = i.Imei,
                VarianteId = i.VarianteId,
                Producto = i.Variante.Producto.Nombre,
                Marca = i.Variante.Producto.Marca!.Nombre,
                Color = i.Variante.Color,
                Almacenamiento = i.Variante.Almacenamiento,
                SucursalId = i.SucursalId,
                Estado = i.Estado,
                PrecioCosto = i.PrecioCosto,
                FechaIngreso = i.FechaIngreso
            })
            .ToListAsync();
    }

    public Task<ImeiDto?> PorImeiAsync(string imei) => ProyectarAsync(i => i.Imei == imei);

    public Task<ImeiDto?> PorIdAsync(int id) => ProyectarAsync(i => i.ImeiId == id);

    public async Task<ImeiDto> RegistrarAsync(ImeiRegistroDto dto, int? usuarioId)
    {
        if (await _db.InventarioImeis.AnyAsync(i => i.Imei == dto.Imei))
            throw new InventarioException($"El IMEI {dto.Imei} ya existe en el inventario.");

        if (!await _db.ProductoVariantes.AnyAsync(v => v.VarianteId == dto.VarianteId))
            throw new InventarioException("La variante de producto indicada no existe.");

        if (!await _db.Sucursales.AnyAsync(s => s.SucursalId == dto.SucursalId))
            throw new InventarioException("La sucursal indicada no existe.");

        var equipo = new InventarioImei
        {
            Imei = dto.Imei,
            VarianteId = dto.VarianteId,
            SucursalId = dto.SucursalId,
            CompraId = dto.CompraId,
            PrecioCosto = dto.PrecioCosto,
            Estado = "Disponible",
            FechaIngreso = DateTime.Now
        };
        _db.InventarioImeis.Add(equipo);
        await _db.SaveChangesAsync();

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            ImeiId = equipo.ImeiId,
            Tipo = "Entrada",
            SucursalDestino = equipo.SucursalId,
            Referencia = dto.CompraId.HasValue ? $"Compra #{dto.CompraId}" : "Ingreso manual",
            UsuarioId = usuarioId,
            Fecha = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return (await PorIdAsync(equipo.ImeiId))!;
    }

    public async Task TransferirAsync(int imeiId, int sucursalDestinoId, int? usuarioId)
    {
        var equipo = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == imeiId)
            ?? throw new InventarioException("El equipo (IMEI) no existe.");

        if (equipo.Estado != "Disponible")
            throw new InventarioException($"El equipo no se puede transferir porque está '{equipo.Estado}'.");

        if (!await _db.Sucursales.AnyAsync(s => s.SucursalId == sucursalDestinoId))
            throw new InventarioException("La sucursal destino no existe.");

        if (equipo.SucursalId == sucursalDestinoId)
            throw new InventarioException("El equipo ya está en esa sucursal.");

        var origen = equipo.SucursalId;
        equipo.SucursalId = sucursalDestinoId;

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            ImeiId = equipo.ImeiId,
            Tipo = "Transferencia",
            SucursalOrigen = origen,
            SucursalDestino = sucursalDestinoId,
            Referencia = $"Transferencia {origen} -> {sucursalDestinoId}",
            UsuarioId = usuarioId,
            Fecha = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    private async Task<ImeiDto?> ProyectarAsync(Expression<Func<InventarioImei, bool>> filtro)
    {
        return await _db.InventarioImeis.AsNoTracking()
            .Where(filtro)
            .Select(i => new ImeiDto
            {
                ImeiId = i.ImeiId,
                Imei = i.Imei,
                VarianteId = i.VarianteId,
                Producto = i.Variante.Producto.Nombre,
                Marca = i.Variante.Producto.Marca!.Nombre,
                Color = i.Variante.Color,
                Almacenamiento = i.Variante.Almacenamiento,
                SucursalId = i.SucursalId,
                Estado = i.Estado,
                PrecioCosto = i.PrecioCosto,
                FechaIngreso = i.FechaIngreso
            })
            .FirstOrDefaultAsync();
    }
}
