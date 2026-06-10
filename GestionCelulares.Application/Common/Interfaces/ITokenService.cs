using GestionCelulares.Domain.Entities;

namespace GestionCelulares.Application.Common.Interfaces;

public interface ITokenService
{
    (string token, DateTime expira) CrearAccessToken(Usuario usuario);
    (string token, DateTime expira) CrearRefreshToken();
}
