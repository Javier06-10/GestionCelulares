using System.Security.Claims;
using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CajaController : ControllerBase
{
    private readonly ICajaService _caja;

    public CajaController(ICajaService caja) => _caja = caja;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Abre una sesión de caja en la sucursal (solo una abierta por sucursal).</summary>
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AperturaCajaDto dto)
    {
        if (UsuarioId is null)
            return Unauthorized();

        try
        {
            var sesion = await _caja.AbrirAsync(dto, UsuarioId.Value);
            return CreatedAtAction(nameof(PorId), new { id = sesion.SesionCajaId }, sesion);
        }
        catch (CajaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Sesión de caja abierta en la sucursal (404 si no hay; el POS la exige antes de vender).</summary>
    [HttpGet("actual")]
    public async Task<IActionResult> Actual([FromQuery] int sucursalId)
    {
        var sesion = await _caja.SesionActualAsync(sucursalId);
        return sesion is null
            ? NotFound(new { error = "No hay sesión de caja abierta en esta sucursal." })
            : Ok(sesion);
    }

    /// <summary>Caja abierta del usuario autenticado (null si no tiene). Se usa para
    /// impedir cerrar sesión con la caja abierta.</summary>
    [HttpGet("mia")]
    public async Task<IActionResult> Mia()
    {
        if (UsuarioId is null) return Unauthorized();
        return Ok(await _caja.MiSesionAbiertaAsync(UsuarioId.Value));
    }

    /// <summary>Consulta una sesión de caja con sus totales de ingresos y egresos.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var sesion = await _caja.PorIdAsync(id);
        return sesion is null ? NotFound() : Ok(sesion);
    }

    /// <summary>Movimientos manuales (ingresos/egresos) de la sesión.</summary>
    [HttpGet("{id:int}/movimientos")]
    public async Task<IActionResult> Movimientos(int id)
    {
        try
        {
            return Ok(await _caja.MovimientosAsync(id));
        }
        catch (CajaException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Resumen del turno: ventas, abonos y movimientos del empleado de la sesión.</summary>
    [HttpGet("{id:int}/resumen")]
    public async Task<IActionResult> Resumen(int id)
    {
        try
        {
            return Ok(await _caja.ResumenTurnoAsync(id));
        }
        catch (CajaException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Registra un ingreso o egreso manual en la sesión abierta.</summary>
    [HttpPost("{id:int}/movimientos")]
    public async Task<IActionResult> RegistrarMovimiento(int id, [FromBody] MovimientoCajaRegistroDto dto)
    {
        try
        {
            return Ok(await _caja.RegistrarMovimientoAsync(id, dto));
        }
        catch (CajaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Cierra la sesión con arqueo: usp_Caja_Cerrar calcula el esperado y la diferencia.</summary>
    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CierreCajaDto dto)
    {
        if (UsuarioId is null)
            return Unauthorized();

        try
        {
            return Ok(await _caja.CerrarAsync(id, dto, UsuarioId.Value, User.IsInRole(Roles.Admin)));
        }
        catch (CajaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
