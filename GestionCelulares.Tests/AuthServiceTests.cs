using GestionCelulares.Application.Auth;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class AuthServiceTests
{
    private const string Pass = "secreta123";

    // Hasher de prueba: hash determinístico "hash:<password>".
    private sealed class HasherFake : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hash) => hash == "hash:" + password;
    }

    private sealed class TokenFake : ITokenService
    {
        public (string token, DateTime expira) CrearAccessToken(Usuario usuario) => ("access", DateTime.Now.AddMinutes(60));
        public (string token, DateTime expira) CrearRefreshToken() => (Guid.NewGuid().ToString("N"), DateTime.Now.AddDays(7));
        public string HashRefreshToken(string token) => "rh:" + token;
    }

    private static async Task<GestionCelularesContext> ConUsuarioAsync()
    {
        var db = TestDb.Crear();
        db.Roles.Add(new Rol { RolId = 1, Nombre = "Admin" });
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = 1,
            NombreUsuario = "admin",
            NombreCompleto = "Administrador",
            HashContrasena = "hash:" + Pass,
            RolId = 1,
            Activo = true,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        return db;
    }

    private static AuthService Servicio(GestionCelularesContext db) => new(db, new TokenFake(), new HasherFake());

    [Fact]
    public async Task Login_correcto_emite_token_y_resetea_intentos()
    {
        var db = await ConUsuarioAsync();
        var u = await db.Usuarios.FirstAsync();
        u.IntentosFallidos = 3;
        await db.SaveChangesAsync();

        var resp = await Servicio(db).LoginAsync(new LoginRequest { NombreUsuario = "admin", Contrasena = Pass });

        Assert.False(string.IsNullOrEmpty(resp.AccessToken));
        var refrescado = await db.Usuarios.AsNoTracking().FirstAsync();
        Assert.Equal(0, refrescado.IntentosFallidos);
        Assert.Null(refrescado.BloqueadoHasta);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_incrementa_intentos()
    {
        var db = await ConUsuarioAsync();
        var svc = Servicio(db);

        await Assert.ThrowsAsync<AuthException>(() =>
            svc.LoginAsync(new LoginRequest { NombreUsuario = "admin", Contrasena = "mala" }));

        var u = await db.Usuarios.AsNoTracking().FirstAsync();
        Assert.Equal(1, u.IntentosFallidos);
    }

    [Fact]
    public async Task Login_bloquea_la_cuenta_tras_cinco_fallos()
    {
        var db = await ConUsuarioAsync();
        var svc = Servicio(db);

        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<AuthException>(() =>
                svc.LoginAsync(new LoginRequest { NombreUsuario = "admin", Contrasena = "mala" }));

        var u = await db.Usuarios.AsNoTracking().FirstAsync();
        Assert.NotNull(u.BloqueadoHasta);
        Assert.True(u.BloqueadoHasta > DateTime.Now);
    }

    [Fact]
    public async Task Login_bloqueado_rechaza_aunque_la_password_sea_correcta()
    {
        var db = await ConUsuarioAsync();
        var u = await db.Usuarios.FirstAsync();
        u.BloqueadoHasta = DateTime.Now.AddMinutes(10);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AuthException>(() =>
            Servicio(db).LoginAsync(new LoginRequest { NombreUsuario = "admin", Contrasena = Pass }));
    }
}
