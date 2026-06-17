using GestionCelulares.Domain.Entities;

namespace GestionCelulares.Application.Common.Interfaces;

public interface ITokenService
{
    (string token, DateTime expira) CrearAccessToken(Usuario usuario);
    (string token, DateTime expira) CrearRefreshToken();

    /// <summary>Hash SHA-256 del refresh token; en la BD solo se guarda el hash, nunca el token en claro.</summary>
    string HashRefreshToken(string token);
}
