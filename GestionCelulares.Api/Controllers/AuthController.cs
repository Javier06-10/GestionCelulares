using GestionCelulares.Application.Auth;
using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Inicia sesión y devuelve el access token + refresh token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        try
        {
            return Ok(await _auth.LoginAsync(req));
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Renueva el access token a partir de un refresh token válido.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest req)
    {
        try
        {
            return Ok(await _auth.RefreshAsync(req.RefreshToken));
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
