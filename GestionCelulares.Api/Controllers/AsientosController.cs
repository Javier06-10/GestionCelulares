using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Contabilidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Libro diario (asientos) y balance de comprobación. Solo administrador.</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class AsientosController : ControllerBase
{
    private readonly IAsientoService _asientos;

    public AsientosController(IAsientoService asientos) => _asientos = asientos;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _asientos.ListarAsync(desde, hasta));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _asientos.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AsientoCrearDto dto)
    {
        try
        {
            var a = await _asientos.CrearAsync(dto, UsuarioId);
            return CreatedAtAction(nameof(PorId), new { id = a.AsientoContableId }, a);
        }
        catch (ContabilidadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        try
        {
            await _asientos.AnularAsync(id);
            return NoContent();
        }
        catch (ContabilidadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("balance-comprobacion")]
    public async Task<IActionResult> Balance([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        => Ok(await _asientos.BalanceComprobacionAsync(desde, hasta));
}
