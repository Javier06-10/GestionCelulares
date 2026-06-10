using GestionCelulares.Application.Auth;
using GestionCelulares.Application.Inventario;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCelulares.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInventarioService, InventarioService>();
        return services;
    }
}
