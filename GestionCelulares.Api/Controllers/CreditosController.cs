using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Creditos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CreditosController : ControllerBase
{
    private readonly ICreditoService _creditos;

    public CreditosController(ICreditoService creditos) => _creditos = creditos;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Lista créditos con filtros por cliente y estado (Activo, EnMora, Saldado).</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] int? clienteId, [FromQuery] string? estado)
        => Ok(await _creditos.BuscarAsync(clienteId, estado));

    /// <summary>Consulta un crédito con su tabla de cuotas.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _creditos.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Crea el financiamiento y genera sus cuotas (usp_Credito_Crear, interés simple mensual).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreditoCrearDto dto)
    {
        try
        {
            var credito = await _creditos.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = credito.CreditoId }, credito);
        }
        catch (CreditoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Historial de abonos del crédito.</summary>
    [HttpGet("{id:int}/abonos")]
    public async Task<IActionResult> Abonos(int id)
    {
        try
        {
            return Ok(await _creditos.AbonosAsync(id));
        }
        catch (CreditoException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Registra un abono (usp_PagoCredito_Registrar): distribuye desde la cuota más antigua.</summary>
    [HttpPost("{id:int}/abonos")]
    public async Task<IActionResult> RegistrarAbono(int id, [FromBody] AbonoRegistroDto dto)
    {
        try
        {
            return Ok(await _creditos.RegistrarAbonoAsync(id, dto, UsuarioId));
        }
        catch (CreditoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Cuotas vencidas en tiempo real (vista vw_CuotasVencidas), para cobranza.</summary>
    [HttpGet("cuotas-vencidas")]
    public async Task<IActionResult> CuotasVencidas()
        => Ok(await _creditos.CuotasVencidasAsync());

    /// <summary>Corre el proceso de mora (usp_Creditos_ActualizarMora); pensado como job diario.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("actualizar-mora")]
    public async Task<IActionResult> ActualizarMora([FromQuery] DateTime? fechaCorte)
    {
        try
        {
            return Ok(await _creditos.ActualizarMoraAsync(fechaCorte));
        }
        catch (CreditoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
