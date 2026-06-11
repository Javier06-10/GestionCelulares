using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(bool? activos);
    Task<UsuarioDto?> PorIdAsync(int id);
    Task<UsuarioDto> CrearAsync(UsuarioCrearDto dto);
    Task<UsuarioDto> ActualizarAsync(int id, UsuarioActualizarDto dto, int usuarioActualId);
    Task ResetContrasenaAsync(int id, ResetContrasenaDto dto);
    Task CambiarContrasenaAsync(int usuarioActualId, CambiarContrasenaDto dto);
    Task<IReadOnlyList<RolDto>> RolesAsync();
}

public class UsuarioService : IUsuarioService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UsuarioService(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(bool? activos)
    {
        var q = _db.Usuarios.AsNoTracking();

        if (activos.HasValue)
            q = q.Where(u => u.Activo == activos.Value);

        return await q.OrderBy(u => u.NombreUsuario).Select(Proyeccion).ToListAsync();
    }

    public async Task<UsuarioDto?> PorIdAsync(int id)
        => await _db.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioId == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

    public async Task<UsuarioDto> CrearAsync(UsuarioCrearDto dto)
    {
        var nombreUsuario = dto.NombreUsuario.Trim().ToLowerInvariant();

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario))
            throw new UsuarioException($"Ya existe el usuario '{nombreUsuario}'.");

        await ValidarReferenciasAsync(dto.RolId, dto.SucursalId);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = dto.NombreCompleto.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            HashContrasena = _hasher.Hash(dto.Contrasena),
            RolId = dto.RolId,
            SucursalId = dto.SucursalId,
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return (await PorIdAsync(usuario.UsuarioId))!;
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, UsuarioActualizarDto dto, int usuarioActualId)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new UsuarioException("El usuario no existe.");

        if (id == usuarioActualId && !dto.Activo)
            throw new UsuarioException("No puedes desactivar tu propia cuenta.");

        await ValidarReferenciasAsync(dto.RolId, dto.SucursalId);

        var desactivado = usuario.Activo && !dto.Activo;

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        usuario.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        usuario.RolId = dto.RolId;
        usuario.SucursalId = dto.SucursalId;
        usuario.Activo = dto.Activo;

        if (desactivado)
            await RevocarRefreshTokensAsync(id);

        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task ResetContrasenaAsync(int id, ResetContrasenaDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new UsuarioException("El usuario no existe.");

        usuario.HashContrasena = _hasher.Hash(dto.ContrasenaNueva);
        await RevocarRefreshTokensAsync(id);
        await _db.SaveChangesAsync();
    }

    public async Task CambiarContrasenaAsync(int usuarioActualId, CambiarContrasenaDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioActualId)
            ?? throw new UsuarioException("El usuario no existe.");

        if (!_hasher.Verify(dto.ContrasenaActual, usuario.HashContrasena))
            throw new UsuarioException("La contraseña actual es incorrecta.");

        usuario.HashContrasena = _hasher.Hash(dto.ContrasenaNueva);
        await RevocarRefreshTokensAsync(usuarioActualId);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RolDto>> RolesAsync()
        => await _db.Roles.AsNoTracking()
            .OrderBy(r => r.RolId)
            .Select(r => new RolDto { RolId = r.RolId, Nombre = r.Nombre, Descripcion = r.Descripcion })
            .ToListAsync();

    /// <summary>Al cambiar la clave o desactivar la cuenta, las sesiones abiertas dejan de poder renovarse.</summary>
    private async Task RevocarRefreshTokensAsync(int usuarioId)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && !t.Revocado)
            .ToListAsync();
        foreach (var t in tokens)
            t.Revocado = true;
    }

    private async Task ValidarReferenciasAsync(int rolId, int? sucursalId)
    {
        if (!await _db.Roles.AnyAsync(r => r.RolId == rolId))
            throw new UsuarioException("El rol indicado no existe.");

        if (sucursalId.HasValue && !await _db.Sucursales.AnyAsync(s => s.SucursalId == sucursalId.Value))
            throw new UsuarioException("La sucursal indicada no existe.");
    }

    private static readonly Expression<Func<Usuario, UsuarioDto>> Proyeccion = u => new UsuarioDto
    {
        UsuarioId = u.UsuarioId,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Email = u.Email,
        RolId = u.RolId,
        Rol = u.Rol.Nombre,
        SucursalId = u.SucursalId,
        Activo = u.Activo,
        UltimoAcceso = u.UltimoAcceso,
        FechaCreacion = u.FechaCreacion
    };
}
