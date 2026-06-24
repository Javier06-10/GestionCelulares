using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _inv;

    public InventarioController(IInventarioService inv) => _inv = inv;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Lista el stock disponible (vista vw_InventarioDisponible), opcionalmente por sucursal.</summary>
    [HttpGet("disponibles")]
    public async Task<IActionResult> Disponibles([FromQuery] int? sucursalId)
        => Ok(await _inv.DisponiblesAsync(sucursalId));

    /// <summary>Lista los IMEIs disponibles de una variante (para elegir en el POS).</summary>
    [HttpGet("disponibles-imeis")]
    public async Task<IActionResult> ImeisDisponibles([FromQuery] int varianteId, [FromQuery] int? sucursalId)
        => Ok(await _inv.ImeisDisponiblesAsync(varianteId, sucursalId));

    /// <summary>Consulta un equipo por su IMEI.</summary>
    [HttpGet("imei/{imei}")]
    public async Task<IActionResult> PorImei(string imei)
    {
        var dto = await _inv.PorImeiAsync(imei);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Consulta un equipo por su Id interno.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _inv.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Registra un nuevo equipo (IMEI) en el inventario y genera el movimiento de entrada.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] ImeiRegistroDto dto)
    {
        try
        {
            var creado = await _inv.RegistrarAsync(dto, UsuarioId);
            return CreatedAtAction(nameof(PorId), new { id = creado.ImeiId }, creado);
        }
        catch (InventarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Registra varios equipos (IMEI) de un golpe; reporta duplicados e inválidos.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("lote")]
    public async Task<IActionResult> RegistrarLote([FromBody] LoteRegistroDto dto)
    {
        try
        {
            return Ok(await _inv.RegistrarLoteAsync(dto, UsuarioId));
        }
        catch (InventarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Transfiere un equipo a otra sucursal y registra el movimiento.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/transferir")]
    public async Task<IActionResult> Transferir(int id, [FromBody] TransferenciaDto dto)
    {
        try
        {
            await _inv.TransferirAsync(id, dto.SucursalDestinoId, UsuarioId);
            return NoContent();
        }
        catch (InventarioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
