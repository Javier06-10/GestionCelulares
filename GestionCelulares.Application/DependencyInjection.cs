using GestionCelulares.Application.Auth;
using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Catalogo;
using GestionCelulares.Application.Clientes;
using GestionCelulares.Application.Creditos;
using GestionCelulares.Application.Dashboard;
using GestionCelulares.Application.Faltantes;
using GestionCelulares.Application.Garantias;
using GestionCelulares.Application.Inventario;
using GestionCelulares.Application.Proveedores;
using GestionCelulares.Application.Reportes;
using GestionCelulares.Application.Taller;
using GestionCelulares.Application.Usuarios;
using GestionCelulares.Application.Ventas;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCelulares.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInventarioService, InventarioService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<ICajaService, CajaService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICreditoService, CreditoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ITallerService, TallerService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IGarantiaService, GarantiaService>();
        services.AddScoped<IFaltanteService, FaltanteService>();
        return services;
    }
}
