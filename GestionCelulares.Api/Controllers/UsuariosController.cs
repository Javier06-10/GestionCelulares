using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

// Nota: los [Authorize] se acumulan (controlador Y acción deben cumplirse),
// por eso el rol Admin se exige por acción y no a nivel de clase.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Lista los usuarios, opcionalmente filtrando por activos.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? activos)
        => Ok(await _usuarios.ListarAsync(activos));

    /// <summary>Consulta un usuario por su Id.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _usuarios.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Roles disponibles (para el formulario de usuarios).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("roles")]
    public async Task<IActionResult> ListarRoles() => Ok(await _usuarios.RolesAsync());

    /// <summary>Crea un usuario con su rol, sucursal y contraseña inicial.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] UsuarioCrearDto dto)
    {
        try
        {
            var creado = await _usuarios.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = creado.UsuarioId }, creado);
        }
        catch (UsuarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza datos, rol, sucursal y estado (no permite desactivar la cuenta propia).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] UsuarioActualizarDto dto)
    {
        if (UsuarioId is null)
            return Unauthorized();

        try
        {
            return Ok(await _usuarios.ActualizarAsync(id, dto, UsuarioId.Value));
        }
        catch (UsuarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Restablece la contraseña de un usuario (revoca sus refresh tokens).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/reset-contrasena")]
    public async Task<IActionResult> ResetContrasena(int id, [FromBody] ResetContrasenaDto dto)
    {
        try
        {
            await _usuarios.ResetContrasenaAsync(id, dto);
            return NoContent();
        }
        catch (UsuarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Cambio de contraseña del usuario autenticado (exige la contraseña actual).</summary>
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaDto dto)
    {
        if (UsuarioId is null)
            return Unauthorized();

        try
        {
            await _usuarios.CambiarContrasenaAsync(UsuarioId.Value, dto);
            return NoContent();
        }
        catch (UsuarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
