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
    private const string AccessCookie = "gc_access";
    private const string RefreshCookie = "gc_refresh";
    private const string RefreshPath = "/api/auth";   // la cookie de refresh solo se envía a /api/auth/*

    private readonly IAuthService _auth;
    private readonly int _refreshDias;

    public AuthController(IAuthService auth, IConfiguration config)
    {
        _auth = auth;
        _refreshDias = config.GetValue("Jwt:RefreshTokenDays", 7);
    }

    /// <summary>Inicia sesión. Los tokens se emiten como cookies HttpOnly; el body solo trae el usuario.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var resp = await _auth.LoginAsync(req);
            EscribirCookies(resp);
            return Ok(new { resp.Usuario, resp.ExpiraEn });
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Renueva la sesión a partir de la cookie de refresh (rota ambos tokens).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refresh = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(refresh))
            return Unauthorized(new { error = "No hay sesión que renovar." });

        try
        {
            var resp = await _auth.RefreshAsync(refresh);
            EscribirCookies(resp);
            return Ok(new { resp.Usuario, resp.ExpiraEn });
        }
        catch (AuthException ex)
        {
            BorrarCookies();
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Cierra la sesión: revoca el refresh token y borra las cookies.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync(Request.Cookies[RefreshCookie]);
        BorrarCookies();
        return NoContent();
    }

    // La cookie de refresh se limita a /api/auth; la de acceso va a toda la app.
    private void EscribirCookies(LoginResponse resp)
    {
        Response.Cookies.Append(AccessCookie, resp.AccessToken, Opciones("/", resp.ExpiraEn));
        Response.Cookies.Append(RefreshCookie, resp.RefreshToken, Opciones(RefreshPath, DateTimeOffset.Now.AddDays(_refreshDias)));
    }

    private void BorrarCookies()
    {
        Response.Cookies.Delete(AccessCookie, Opciones("/", null));
        Response.Cookies.Delete(RefreshCookie, Opciones(RefreshPath, null));
    }

    private static CookieOptions Opciones(string path, DateTimeOffset? expira) => new()
    {
        HttpOnly = true,                    // no accesible desde JS (blinda contra robo por XSS)
        Secure = true,                      // solo por HTTPS (localhost es contexto seguro en dev)
        SameSite = SameSiteMode.Strict,     // no se envía cross-site → mitiga CSRF
        Path = path,
        Expires = expira
    };
}
