using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Usuarios;

public class UsuarioCrearDto
{
    [Required, StringLength(50, MinimumLength = 3)] public string NombreUsuario { get; set; } = null!;
    [Required, StringLength(150)] public string NombreCompleto { get; set; } = null!;
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [Required, StringLength(100, MinimumLength = 8)] public string Contrasena { get; set; } = null!;
    [Required] public int RolId { get; set; }
    public int? SucursalId { get; set; }
}

public class UsuarioActualizarDto
{
    [Required, StringLength(150)] public string NombreCompleto { get; set; } = null!;
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [Required] public int RolId { get; set; }
    public int? SucursalId { get; set; }
    public bool Activo { get; set; } = true;
}

public class UsuarioDto
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Email { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = null!;
    public int? SucursalId { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>Cambio de contraseña del propio usuario (exige la actual).</summary>
public class CambiarContrasenaDto
{
    [Required] public string ContrasenaActual { get; set; } = null!;
    [Required, StringLength(100, MinimumLength = 8)] public string ContrasenaNueva { get; set; } = null!;
}

/// <summary>Restablecimiento de contraseña por un administrador.</summary>
public class ResetContrasenaDto
{
    [Required, StringLength(100, MinimumLength = 8)] public string ContrasenaNueva { get; set; } = null!;
}

public class RolDto
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
}
