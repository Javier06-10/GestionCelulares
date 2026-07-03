namespace GestionCelulares.Domain.Entities;

public class Rol
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}

public class Permiso
{
    public int PermisoId { get; set; }
    public string Codigo { get; set; } = null!;
    public string? Descripcion { get; set; }

    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}

public class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }
    public Rol Rol { get; set; } = null!;
    public Permiso Permiso { get; set; } = null!;
}

public class Usuario
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Email { get; set; }
    public string HashContrasena { get; set; } = null!;
    public int RolId { get; set; }
    public int? SucursalId { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Bloqueo por intentos fallidos de inicio de sesión
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }

    public Rol Rol { get; set; } = null!;
    public Sucursal? Sucursal { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken
{
    public long RefreshTokenId { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime Expira { get; set; }
    public bool Revocado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
