using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Infrastructure.Identity;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCelulares.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("Jwt"));

        services.AddDbContext<GestionCelularesContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        // El contexto concreto también se expone a través de la abstracción de Application
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<GestionCelularesContext>());

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ICajaProcedures, CajaProcedures>();
        services.AddScoped<IVentaProcedures, VentaProcedures>();
        services.AddScoped<ISecuenciaFactura>(sp => (VentaProcedures)sp.GetRequiredService<IVentaProcedures>());
        services.AddScoped<INcfProcedures>(sp => (VentaProcedures)sp.GetRequiredService<IVentaProcedures>());
        services.AddScoped<ICreditoProcedures, CreditoProcedures>();

        return services;
    }
}
