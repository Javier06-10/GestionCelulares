using GestionCelulares.Application.Auth;
using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Catalogo;
using GestionCelulares.Application.Clientes;
using GestionCelulares.Application.Inventario;
using GestionCelulares.Application.Proveedores;
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
        return services;
    }
}
