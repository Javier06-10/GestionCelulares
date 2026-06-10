using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Inventario;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Infrastructure.Persistence;

public class GestionCelularesContext : DbContext, IApplicationDbContext
{
    public GestionCelularesContext(DbContextOptions<GestionCelularesContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<PagoProveedor> PagosProveedor => Set<PagoProveedor>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoVariante> ProductoVariantes => Set<ProductoVariante>();
    public DbSet<InventarioImei> InventarioImeis => Set<InventarioImei>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<InventarioDisponibleDto> InventarioDisponible => Set<InventarioDisponibleDto>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Tablas (los DbSet son plurales; la BD usa el singular)
        mb.Entity<Empresa>().ToTable("Empresa").HasKey(e => e.EmpresaId);
        mb.Entity<Sucursal>().ToTable("Sucursal").HasKey(e => e.SucursalId);
        mb.Entity<Rol>().ToTable("Rol").HasKey(e => e.RolId);
        mb.Entity<Permiso>().ToTable("Permiso").HasKey(e => e.PermisoId);
        mb.Entity<Usuario>().ToTable("Usuario").HasKey(e => e.UsuarioId);
        mb.Entity<Cliente>().ToTable("Cliente").HasKey(e => e.ClienteId);
        mb.Entity<Proveedor>().ToTable("Proveedor").HasKey(e => e.ProveedorId);
        mb.Entity<Compra>().ToTable("Compra").HasKey(e => e.CompraId);
        mb.Entity<PagoProveedor>().ToTable("PagoProveedor").HasKey(e => e.PagoProveedorId);
        mb.Entity<RefreshToken>().ToTable("RefreshToken").HasKey(e => e.RefreshTokenId);
        mb.Entity<Marca>().ToTable("Marca").HasKey(e => e.MarcaId);
        mb.Entity<Categoria>().ToTable("Categoria").HasKey(e => e.CategoriaId);
        mb.Entity<Producto>().ToTable("Producto").HasKey(e => e.ProductoId);
        mb.Entity<ProductoVariante>().ToTable("ProductoVariante").HasKey(e => e.VarianteId);
        mb.Entity<InventarioImei>().ToTable("InventarioImei").HasKey(e => e.ImeiId);
        mb.Entity<MovimientoInventario>().ToTable("MovimientoInventario").HasKey(e => e.MovimientoId);

        // Clave compuesta y relaciones de RolPermiso
        mb.Entity<RolPermiso>().ToTable("RolPermiso").HasKey(x => new { x.RolId, x.PermisoId });
        mb.Entity<RolPermiso>().HasOne(x => x.Rol).WithMany(r => r.RolPermisos).HasForeignKey(x => x.RolId);
        mb.Entity<RolPermiso>().HasOne(x => x.Permiso).WithMany(p => p.RolPermisos).HasForeignKey(x => x.PermisoId);

        mb.Entity<Usuario>().HasOne(u => u.Rol).WithMany(r => r.Usuarios).HasForeignKey(u => u.RolId);
        mb.Entity<Usuario>().HasOne(u => u.Sucursal).WithMany(s => s.Usuarios).HasForeignKey(u => u.SucursalId);

        mb.Entity<RefreshToken>().HasOne(t => t.Usuario).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UsuarioId);

        mb.Entity<Producto>().HasOne(p => p.Marca).WithMany(m => m.Productos).HasForeignKey(p => p.MarcaId);
        mb.Entity<Producto>().HasOne(p => p.Categoria).WithMany(c => c.Productos).HasForeignKey(p => p.CategoriaId);
        mb.Entity<ProductoVariante>().HasOne(v => v.Producto).WithMany(p => p.Variantes).HasForeignKey(v => v.ProductoId);

        mb.Entity<InventarioImei>().HasOne(i => i.Variante).WithMany(v => v.Imeis).HasForeignKey(i => i.VarianteId);
        mb.Entity<InventarioImei>().HasOne(i => i.Sucursal).WithMany(s => s.Inventario).HasForeignKey(i => i.SucursalId);
        mb.Entity<InventarioImei>().HasIndex(i => i.Imei).IsUnique();

        mb.Entity<MovimientoInventario>().HasOne(m => m.Imei).WithMany().HasForeignKey(m => m.ImeiId);

        mb.Entity<Compra>().HasOne(c => c.Proveedor).WithMany(p => p.Compras).HasForeignKey(c => c.ProveedorId);
        mb.Entity<Compra>().HasOne(c => c.Sucursal).WithMany().HasForeignKey(c => c.SucursalId);
        mb.Entity<PagoProveedor>().HasOne(p => p.Proveedor).WithMany(x => x.Pagos).HasForeignKey(p => p.ProveedorId);
        mb.Entity<PagoProveedor>().HasOne(p => p.Compra).WithMany().HasForeignKey(p => p.CompraId);

        // Vista de solo lectura
        mb.Entity<InventarioDisponibleDto>().HasNoKey().ToView("vw_InventarioDisponible");

        // Precisión de decimales (la BD ya existe)
        foreach (var prop in mb.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            prop.SetColumnType("decimal(18,2)");
        }
        mb.Entity<Empresa>().Property(e => e.PorcentajeItbis).HasColumnType("decimal(9,4)");

        base.OnModelCreating(mb);
    }
}
