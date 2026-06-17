using System.Security.Claims;
using GestionCelulares.Application.Apartados;
using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Equipos apartados (layaway): el cliente abona y se lleva el equipo al pagarlo.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ApartadosController : ControllerBase
{
    private readonly IApartadoService _apartados;

    public ApartadosController(IApartadoService apartados) => _apartados = apartados;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Equipos serializados disponibles para apartar.</summary>
    [HttpGet("equipos")]
    public async Task<IActionResult> Equipos([FromQuery] int? sucursalId, [FromQuery] string? buscar)
        => Ok(await _apartados.EquiposApartablesAsync(sucursalId, buscar));

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado, [FromQuery] int? clienteId)
        => Ok(await _apartados.ListarAsync(estado, clienteId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _apartados.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ApartadoCrearDto dto)
        => await Ejecutar(() => _apartados.CrearAsync(dto, UsuarioId));

    [HttpPost("{id:int}/abonos")]
    public async Task<IActionResult> Abonar(int id, [FromBody] AbonoApartadoCrearDto dto)
        => await Ejecutar(() => _apartados.AbonarAsync(id, dto, UsuarioId));

    [HttpPost("{id:int}/cambiar-equipo")]
    public async Task<IActionResult> CambiarEquipo(int id, [FromBody] CambiarEquipoDto dto)
        => await Ejecutar(() => _apartados.CambiarEquipoAsync(id, dto));

    [HttpPost("{id:int}/completar")]
    public async Task<IActionResult> Completar(int id)
    {
        if (UsuarioId is null) return Unauthorized();
        return await Ejecutar(() => _apartados.CompletarAsync(id, UsuarioId.Value));
    }

    /// <summary>Cancela el apartado y libera el equipo. La devolución de dinero es solo del administrador.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarApartadoDto dto)
        => await Ejecutar(() => _apartados.CancelarAsync(id, dto, UsuarioId));

    private async Task<IActionResult> Ejecutar(Func<Task<ApartadoDto>> accion)
    {
        try
        {
            return Ok(await accion());
        }
        catch (ApartadoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
