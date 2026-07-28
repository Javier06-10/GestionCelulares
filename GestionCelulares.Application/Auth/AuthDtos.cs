using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Auth;

public class LoginRequest
{
    [Required] public string NombreUsuario { get; set; } = null!;
    [Required] public string Contrasena { get; set; } = null!;
}

public class UsuarioDto
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public int? SucursalId { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiraEn { get; set; }
    public UsuarioDto Usuario { get; set; } = null!;
}
