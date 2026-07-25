using GestionCelulares.Application.Clientes;
using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesController(IClienteService clientes) => _clientes = clientes;

    /// <summary>Busca clientes (paginado) por nombre, cédula o teléfono; filtra morosos y bloqueados.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string? buscar, [FromQuery] bool? morosos, [FromQuery] bool? bloqueados,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
        => Ok(await _clientes.BuscarAsync(buscar, morosos, bloqueados, pagina, tamano));

    /// <summary>Consulta un cliente por su Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _clientes.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Registra un nuevo cliente (la cédula, si se indica, debe ser única).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ClienteCrearDto dto)
    {
        try
        {
            var creado = await _clientes.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = creado.ClienteId }, creado);
        }
        catch (ClienteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza los datos de un cliente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ClienteActualizarDto dto)
    {
        try
        {
            return Ok(await _clientes.ActualizarAsync(id, dto));
        }
        catch (ClienteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Bloquea un cliente (p. ej. moroso) para impedirle nuevas ventas a crédito.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/bloquear")]
    public async Task<IActionResult> Bloquear(int id)
    {
        try
        {
            await _clientes.BloquearAsync(id, bloqueado: true);
            return NoContent();
        }
        catch (ClienteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Desbloquea un cliente.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        try
        {
            await _clientes.BloquearAsync(id, bloqueado: false);
            return NoContent();
        }
        catch (ClienteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
