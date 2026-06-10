using GestionCelulares.Application.Inventario;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Common.Interfaces;

/// <summary>
/// Abstracción del acceso a datos para la capa de aplicación.
/// La implementación concreta (DbContext de EF Core) vive en Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Sucursal> Sucursales { get; }
    DbSet<ProductoVariante> ProductoVariantes { get; }
    DbSet<InventarioImei> InventarioImeis { get; }
    DbSet<MovimientoInventario> MovimientosInventario { get; }
    DbSet<InventarioDisponibleDto> InventarioDisponible { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
