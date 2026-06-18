using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Creditos;
using GestionCelulares.Application.Garantias;
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
    public DbSet<Faltante> Faltantes => Set<Faltante>();
    public DbSet<PagoEmpleado> PagosEmpleado => Set<PagoEmpleado>();
    public DbSet<Apartado> Apartados => Set<Apartado>();
    public DbSet<AbonoApartado> AbonosApartado => Set<AbonoApartado>();
    public DbSet<SecuenciaNcf> SecuenciasNcf => Set<SecuenciaNcf>();
    public DbSet<InventarioImei> InventarioImeis => Set<InventarioImei>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<VentaPago> VentaPagos => Set<VentaPago>();
    public DbSet<OrdenTaller> OrdenesTaller => Set<OrdenTaller>();
    public DbSet<OrdenTallerFoto> OrdenTallerFotos => Set<OrdenTallerFoto>();
    public DbSet<OrdenTallerRepuesto> OrdenTallerRepuestos => Set<OrdenTallerRepuesto>();
    public DbSet<Credito> Creditos => Set<Credito>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<PagoCredito> PagosCredito => Set<PagoCredito>();
    public DbSet<CuotaVencidaDto> CuotasVencidas => Set<CuotaVencidaDto>();
    public DbSet<Garantia> Garantias => Set<Garantia>();
    public DbSet<CasoGarantia> CasosGarantia => Set<CasoGarantia>();
    public DbSet<GarantiaVigenteDto> GarantiasVigentes => Set<GarantiaVigenteDto>();
    public DbSet<IndiceFallaDto> IndiceFallas => Set<IndiceFallaDto>();
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
        mb.Entity<Faltante>().ToTable("Faltante").HasKey(e => e.FaltanteId);
        mb.Entity<PagoEmpleado>().ToTable("PagoEmpleado").HasKey(e => e.PagoEmpleadoId);
        mb.Entity<Apartado>().ToTable("Apartado").HasKey(e => e.ApartadoId);
        mb.Entity<AbonoApartado>().ToTable("AbonoApartado").HasKey(e => e.AbonoApartadoId);
        mb.Entity<SecuenciaNcf>().ToTable("SecuenciaNcf").HasKey(e => e.SecuenciaNcfId);
        mb.Entity<InventarioImei>().ToTable("InventarioImei").HasKey(e => e.ImeiId);
        mb.Entity<MovimientoInventario>().ToTable("MovimientoInventario").HasKey(e => e.MovimientoId);
        mb.Entity<SesionCaja>().ToTable("SesionCaja").HasKey(e => e.SesionCajaId);
        mb.Entity<MovimientoCaja>().ToTable("MovimientoCaja").HasKey(e => e.MovimientoCajaId);
        mb.Entity<MetodoPago>().ToTable("MetodoPago").HasKey(e => e.MetodoPagoId);
        mb.Entity<Venta>().ToTable("Venta").HasKey(e => e.VentaId);
        mb.Entity<VentaDetalle>().ToTable("VentaDetalle").HasKey(e => e.VentaDetalleId);
        mb.Entity<VentaPago>().ToTable("VentaPago").HasKey(e => e.VentaPagoId);
        mb.Entity<OrdenTaller>().ToTable("OrdenTaller").HasKey(e => e.OrdenTallerId);
        mb.Entity<OrdenTallerFoto>().ToTable("OrdenTallerFoto").HasKey(e => e.Id);
        mb.Entity<OrdenTallerRepuesto>().ToTable("OrdenTallerRepuesto").HasKey(e => e.Id);
        mb.Entity<Garantia>().ToTable("Garantia").HasKey(e => e.GarantiaId);
        mb.Entity<CasoGarantia>().ToTable("CasoGarantia").HasKey(e => e.CasoGarantiaId);
        mb.Entity<Credito>().ToTable("Credito").HasKey(e => e.CreditoId);
        mb.Entity<Cuota>().ToTable("Cuota").HasKey(e => e.CuotaId);
        mb.Entity<PagoCredito>().ToTable("PagoCredito").HasKey(e => e.PagoCreditoId);

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
        mb.Entity<Faltante>().HasOne(f => f.Variante).WithMany().HasForeignKey(f => f.VarianteId);
        mb.Entity<PagoEmpleado>().HasOne(p => p.Empleado).WithMany().HasForeignKey(p => p.EmpleadoId);

        mb.Entity<Apartado>().HasOne(a => a.Cliente).WithMany().HasForeignKey(a => a.ClienteId);
        mb.Entity<Apartado>().HasOne(a => a.Variante).WithMany().HasForeignKey(a => a.VarianteId);
        mb.Entity<Apartado>().HasOne(a => a.Imei).WithMany().HasForeignKey(a => a.ImeiId);
        mb.Entity<AbonoApartado>().HasOne(a => a.Apartado).WithMany(p => p.Abonos).HasForeignKey(a => a.ApartadoId);
        mb.Entity<AbonoApartado>().HasOne(a => a.MetodoPago).WithMany().HasForeignKey(a => a.MetodoPagoId);

        mb.Entity<InventarioImei>().HasOne(i => i.Variante).WithMany(v => v.Imeis).HasForeignKey(i => i.VarianteId);
        mb.Entity<InventarioImei>().HasOne(i => i.Sucursal).WithMany(s => s.Inventario).HasForeignKey(i => i.SucursalId);
        mb.Entity<InventarioImei>().HasIndex(i => i.Imei).IsUnique();

        mb.Entity<MovimientoInventario>().HasOne(m => m.Imei).WithMany().HasForeignKey(m => m.ImeiId);

        mb.Entity<Compra>().HasOne(c => c.Proveedor).WithMany(p => p.Compras).HasForeignKey(c => c.ProveedorId);
        mb.Entity<Compra>().HasOne(c => c.Sucursal).WithMany().HasForeignKey(c => c.SucursalId);
        mb.Entity<PagoProveedor>().HasOne(p => p.Proveedor).WithMany(x => x.Pagos).HasForeignKey(p => p.ProveedorId);
        mb.Entity<PagoProveedor>().HasOne(p => p.Compra).WithMany().HasForeignKey(p => p.CompraId);

        mb.Entity<SesionCaja>().HasOne(s => s.Sucursal).WithMany().HasForeignKey(s => s.SucursalId);
        mb.Entity<MovimientoCaja>().HasOne(m => m.Sesion).WithMany(s => s.Movimientos).HasForeignKey(m => m.SesionCajaId);

        mb.Entity<Venta>().HasOne(v => v.Sucursal).WithMany().HasForeignKey(v => v.SucursalId);
        mb.Entity<Venta>().HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId);
        mb.Entity<VentaDetalle>().HasOne(d => d.Venta).WithMany(v => v.Detalles).HasForeignKey(d => d.VentaId);
        mb.Entity<VentaDetalle>().HasOne(d => d.Imei).WithMany().HasForeignKey(d => d.ImeiId);
        mb.Entity<VentaDetalle>().HasOne(d => d.Variante).WithMany().HasForeignKey(d => d.VarianteId);
        mb.Entity<VentaPago>().HasOne(p => p.Venta).WithMany(v => v.Pagos).HasForeignKey(p => p.VentaId);
        mb.Entity<VentaPago>().HasOne(p => p.MetodoPago).WithMany().HasForeignKey(p => p.MetodoPagoId);

        mb.Entity<OrdenTaller>().HasOne(o => o.Sucursal).WithMany().HasForeignKey(o => o.SucursalId);
        mb.Entity<OrdenTaller>().HasOne(o => o.Cliente).WithMany().HasForeignKey(o => o.ClienteId);
        mb.Entity<OrdenTaller>().HasOne(o => o.Imei).WithMany().HasForeignKey(o => o.ImeiId);
        mb.Entity<OrdenTaller>().HasOne(o => o.Tecnico).WithMany().HasForeignKey(o => o.TecnicoId);
        mb.Entity<OrdenTaller>().HasOne(o => o.Recepcionista).WithMany().HasForeignKey(o => o.UsuarioRecepcion);
        mb.Entity<OrdenTaller>().HasOne(o => o.MetodoPagoAnticipo).WithMany().HasForeignKey(o => o.MetodoPagoAnticipoId);
        mb.Entity<OrdenTaller>().HasOne(o => o.MetodoPagoEntrega).WithMany().HasForeignKey(o => o.MetodoPagoEntregaId);
        mb.Entity<OrdenTallerFoto>().HasOne(f => f.Orden).WithMany(o => o.Fotos).HasForeignKey(f => f.OrdenTallerId);
        mb.Entity<OrdenTallerRepuesto>().HasOne(r => r.Orden).WithMany(o => o.Repuestos).HasForeignKey(r => r.OrdenTallerId);
        mb.Entity<OrdenTallerRepuesto>().HasOne(r => r.Variante).WithMany().HasForeignKey(r => r.VarianteId);

        mb.Entity<Garantia>().HasOne(g => g.Venta).WithMany().HasForeignKey(g => g.VentaId);
        mb.Entity<Garantia>().HasOne(g => g.Imei).WithMany().HasForeignKey(g => g.ImeiId);
        mb.Entity<Garantia>().HasOne(g => g.Cliente).WithMany().HasForeignKey(g => g.ClienteId);
        mb.Entity<CasoGarantia>().HasOne(c => c.Garantia).WithMany(g => g.Casos).HasForeignKey(c => c.GarantiaId);
        mb.Entity<CasoGarantia>().HasOne(c => c.Imei).WithMany().HasForeignKey(c => c.ImeiId);
        mb.Entity<CasoGarantia>().HasOne(c => c.Cliente).WithMany().HasForeignKey(c => c.ClienteId);
        mb.Entity<CasoGarantia>().HasOne(c => c.OrdenTaller).WithMany().HasForeignKey(c => c.OrdenTallerId);
        mb.Entity<CasoGarantia>().HasOne(c => c.ImeiReemplazo).WithMany().HasForeignKey(c => c.ImeiReemplazoId);

        mb.Entity<Credito>().HasOne(c => c.Cliente).WithMany().HasForeignKey(c => c.ClienteId);
        mb.Entity<Credito>().HasOne(c => c.Venta).WithMany().HasForeignKey(c => c.VentaId);
        mb.Entity<Cuota>().HasOne(q => q.Credito).WithMany(c => c.Cuotas).HasForeignKey(q => q.CreditoId);
        mb.Entity<PagoCredito>().HasOne(p => p.Credito).WithMany(c => c.Pagos).HasForeignKey(p => p.CreditoId);
        mb.Entity<PagoCredito>().HasOne(p => p.Cuota).WithMany().HasForeignKey(p => p.CuotaId);
        mb.Entity<PagoCredito>().HasOne(p => p.MetodoPago).WithMany().HasForeignKey(p => p.MetodoPagoId);

        // Vistas de solo lectura
        mb.Entity<InventarioDisponibleDto>().HasNoKey().ToView("vw_InventarioDisponible");
        mb.Entity<CuotaVencidaDto>().HasNoKey().ToView("vw_CuotasVencidas");
        mb.Entity<GarantiaVigenteDto>().HasNoKey().ToView("vw_GarantiaVigentePorImei");
        mb.Entity<IndiceFallaDto>().HasNoKey().ToView("vw_IndiceFallasPorModelo");

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
