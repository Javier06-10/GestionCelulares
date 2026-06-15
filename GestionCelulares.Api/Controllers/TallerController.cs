using GestionCelulares.Application.Common;
using GestionCelulares.Application.Taller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TallerController : ControllerBase
{
    private readonly ITallerService _taller;

    public TallerController(ITallerService taller) => _taller = taller;

    /// <summary>Lista órdenes para el panel Kanban; filtra por estado, sucursal, técnico o cliente.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? estado, [FromQuery] int? sucursalId,
        [FromQuery] int? tecnicoId, [FromQuery] int? clienteId)
        => Ok(await _taller.BuscarAsync(estado, sucursalId, tecnicoId, clienteId));

    /// <summary>Comisiones acumuladas por técnico (órdenes entregadas). Solo administrador.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("comisiones")]
    public async Task<IActionResult> Comisiones([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _taller.ComisionesPorTecnicoAsync(desde, hasta));

    /// <summary>Consulta una orden con sus repuestos y fotos.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _taller.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Recepción del equipo: crea la orden en estado Recibido.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] OrdenCrearDto dto)
    {
        try
        {
            var orden = await _taller.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = orden.OrdenTallerId }, orden);
        }
        catch (TallerException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza diagnóstico, técnico, costos y número de orden (si no está cerrada).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] OrdenActualizarDto dto)
    {
        try
        {
            return Ok(await _taller.ActualizarAsync(id, dto));
        }
        catch (TallerException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mueve la orden en el flujo: Recibido → EnReparacion → Reparado → Entregado (o Cancelado).
    /// Al entregar registra costo final, comisión del técnico y fecha de entrega.
    /// </summary>
    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoDto dto)
    {
        try
        {
            return Ok(await _taller.CambiarEstadoAsync(id, dto));
        }
        catch (TallerException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Registra un repuesto consumido (descuenta stock si es variante del catálogo).</summary>
    [HttpPost("{id:int}/repuestos")]
    public async Task<IActionResult> AgregarRepuesto(int id, [FromBody] RepuestoAgregarDto dto)
    {
        try
        {
            return Ok(await _taller.AgregarRepuestoAsync(id, dto));
        }
        catch (TallerException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Adjunta la URL de una foto del equipo (evidencia de recepción/reparación).</summary>
    [HttpPost("{id:int}/fotos")]
    public async Task<IActionResult> AgregarFoto(int id, [FromBody] FotoAgregarDto dto)
    {
        try
        {
            return Ok(await _taller.AgregarFotoAsync(id, dto));
        }
        catch (TallerException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
