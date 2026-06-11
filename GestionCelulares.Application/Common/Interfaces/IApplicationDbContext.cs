using GestionCelulares.Application.Creditos;
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
    DbSet<Rol> Roles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Sucursal> Sucursales { get; }
    DbSet<Cliente> Clientes { get; }
    DbSet<Proveedor> Proveedores { get; }
    DbSet<Compra> Compras { get; }
    DbSet<PagoProveedor> PagosProveedor { get; }
    DbSet<Marca> Marcas { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Producto> Productos { get; }
    DbSet<ProductoVariante> ProductoVariantes { get; }
    DbSet<InventarioImei> InventarioImeis { get; }
    DbSet<MovimientoInventario> MovimientosInventario { get; }
    DbSet<SesionCaja> SesionesCaja { get; }
    DbSet<MovimientoCaja> MovimientosCaja { get; }
    DbSet<MetodoPago> MetodosPago { get; }
    DbSet<Venta> Ventas { get; }
    DbSet<VentaDetalle> VentaDetalles { get; }
    DbSet<VentaPago> VentaPagos { get; }
    DbSet<OrdenTaller> OrdenesTaller { get; }
    DbSet<OrdenTallerFoto> OrdenTallerFotos { get; }
    DbSet<OrdenTallerRepuesto> OrdenTallerRepuestos { get; }
    DbSet<Credito> Creditos { get; }
    DbSet<Cuota> Cuotas { get; }
    DbSet<PagoCredito> PagosCredito { get; }
    DbSet<CuotaVencidaDto> CuotasVencidas { get; }
    DbSet<InventarioDisponibleDto> InventarioDisponible { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
