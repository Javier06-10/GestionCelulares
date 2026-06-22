using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Contabilidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Contabilización automática y estados financieros. Solo administrador.</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class ContabilidadController : ControllerBase
{
    private readonly IContabilizacionService _contabilizacion;

    public ContabilidadController(IContabilizacionService contabilizacion) => _contabilizacion = contabilizacion;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Genera los asientos automáticos (ventas, nómina, caja) pendientes del rango.</summary>
    [HttpPost("contabilizar")]
    public async Task<IActionResult> Contabilizar([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        try
        {
            return Ok(await _contabilizacion.ContabilizarPendientesAsync(desde, hasta, UsuarioId));
        }
        catch (ContabilidadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("estado-resultados")]
    public async Task<IActionResult> EstadoResultados([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        => Ok(await _contabilizacion.EstadoResultadosAsync(desde, hasta));

    [HttpGet("balance-general")]
    public async Task<IActionResult> BalanceGeneral([FromQuery] DateTime hasta)
        => Ok(await _contabilizacion.BalanceGeneralAsync(hasta));
}
