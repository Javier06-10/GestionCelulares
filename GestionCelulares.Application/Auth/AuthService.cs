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

        if (usuario is null || !usuario.Activo)
            throw new AuthException("Usuario o contraseña inválidos.");

        if (!_hasher.Verify(req.Contrasena, usuario.HashContrasena))
            throw new AuthException("Usuario o contraseña inválidos.");

        usuario.UltimoAcceso = DateTime.Now;
        var resp = Emitir(usuario);
        await _db.SaveChangesAsync();
        return resp;
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token is null || token.Revocado || token.Expira < DateTime.Now)
            throw new AuthException("Refresh token inválido o expirado.");

        token.Revocado = true; // rotación
        var resp = Emitir(token.Usuario);
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
            Token = refresh,
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
