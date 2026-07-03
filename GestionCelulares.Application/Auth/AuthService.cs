using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest req);
    Task<LoginResponse> RefreshAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    // Bloqueo por intentos fallidos
    private const int MaxIntentos = 5;
    private static readonly TimeSpan Bloqueo = TimeSpan.FromMinutes(15);
    // Hash válido para igualar el tiempo de respuesta cuando el usuario no existe
    // (evita enumeración de usuarios por timing). Se calcula una vez y se cachea.
    private static string? _hashDummy;

    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher _hasher;

    public AuthService(IApplicationDbContext db, ITokenService tokens, IPasswordHasher hasher)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == req.NombreUsuario);

        // Cuenta bloqueada temporalmente por intentos fallidos
        if (usuario is not null && usuario.BloqueadoHasta is { } hasta && hasta > DateTime.Now)
        {
            var restan = Math.Max(1, (int)Math.Ceiling((hasta - DateTime.Now).TotalMinutes));
            throw new AuthException($"Cuenta bloqueada por intentos fallidos. Intenta de nuevo en {restan} minuto(s).");
        }

        // Siempre se ejecuta un Verify (con hash dummy si el usuario no existe) para
        // que el tiempo de respuesta no delate si el usuario existe o no.
        var hash = usuario?.HashContrasena ?? (_hashDummy ??= _hasher.Hash("timing-guard"));
        var passwordOk = _hasher.Verify(req.Contrasena, hash);

        if (usuario is null || !usuario.Activo || !passwordOk)
        {
            // Solo cuentan los fallos de contraseña de una cuenta activa existente
            if (usuario is not null && usuario.Activo)
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= MaxIntentos)
                {
                    usuario.BloqueadoHasta = DateTime.Now.Add(Bloqueo);
                    usuario.IntentosFallidos = 0;
                }
                await _db.SaveChangesAsync();
            }
            throw new AuthException("Usuario o contraseña inválidos.");
        }

        // Éxito: se limpian contadores de bloqueo
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = DateTime.Now;
        var resp = Emitir(usuario);
        await _db.SaveChangesAsync();
        return resp;
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var token = await _db.RefreshTokens
            .Include(t => t.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(t => t.Token == hash);

        if (token is null)
            throw new AuthException("Refresh token inválido o expirado.");

        // Detección de reuso: un token ya revocado se está presentando de nuevo
        // (posible robo). Se revocan TODOS los tokens activos del usuario (familia).
        if (token.Revocado)
        {
            var activos = await _db.RefreshTokens
                .Where(t => t.UsuarioId == token.UsuarioId && !t.Revocado)
                .ToListAsync();
            foreach (var t in activos) t.Revocado = true;
            await _db.SaveChangesAsync();
            throw new AuthException("Refresh token inválido o expirado.");
        }

        if (token.Expira < DateTime.Now)
            throw new AuthException("Refresh token inválido o expirado.");

        token.Revocado = true; // rotación
        var resp = Emitir(token.Usuario);

        // Purga de housekeeping: elimina los tokens ya vencidos del usuario
        // (los revocados no vencidos se conservan para poder detectar reuso).
        var vencidos = await _db.RefreshTokens
            .Where(t => t.UsuarioId == token.UsuarioId && t.Expira < DateTime.Now)
            .ToListAsync();
        _db.RefreshTokens.RemoveRange(vencidos);

        await _db.SaveChangesAsync();
        return resp;
    }

    private LoginResponse Emitir(Usuario usuario)
    {
        var (access, expira) = _tokens.CrearAccessToken(usuario);
        var (refresh, refreshExpira) = _tokens.CrearRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuario.UsuarioId,
            Token = _tokens.HashRefreshToken(refresh), // solo se persiste el hash, nunca el token en claro
            Expira = refreshExpira,
            Revocado = false,
            FechaCreacion = DateTime.Now
        });

        return new LoginResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiraEn = expira,
            Usuario = new UsuarioDto
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol?.Nombre ?? string.Empty,
                SucursalId = usuario.SucursalId
            }
        };
    }
}
