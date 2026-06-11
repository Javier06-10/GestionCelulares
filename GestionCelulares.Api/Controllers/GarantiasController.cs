using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Garantias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GarantiasController : ControllerBase
{
    private readonly IGarantiaService _garantias;

    public GarantiasController(IGarantiaService garantias) => _garantias = garantias;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // ----- Garantías -----

    /// <summary>Lista garantías; filtra por cliente o solo vigentes.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] int? clienteId, [FromQuery] bool? vigentes)
        => Ok(await _garantias.BuscarAsync(clienteId, vigentes));

    /// <summary>Consulta una garantía por su Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _garantias.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Consulta rápida de garantía por IMEI (vista vw_GarantiaVigentePorImei).</summary>
    [HttpGet("imei/{imei}")]
    public async Task<IActionResult> PorImei(string imei)
    {
        var dto = await _garantias.PorImeiAsync(imei);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Registra la garantía de una venta/equipo (cobertura en meses).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GarantiaCrearDto dto)
    {
        try
        {
            var creada = await _garantias.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = creada.GarantiaId }, creada);
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Anula una garantía vigente.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        try
        {
            await _garantias.AnularAsync(id);
            return NoContent();
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ----- Casos (RMA) -----

    /// <summary>Lista casos de garantía; filtra por estado o cliente.</summary>
    [HttpGet("casos")]
    public async Task<IActionResult> Casos([FromQuery] string? estado, [FromQuery] int? clienteId)
        => Ok(await _garantias.CasosAsync(estado, clienteId));

    /// <summary>Consulta un caso por su Id.</summary>
    [HttpGet("casos/{id:int}")]
    public async Task<IActionResult> CasoPorId(int id)
    {
        var dto = await _garantias.CasoPorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Abre un caso de garantía/RMA (exige garantía vigente si se indica).</summary>
    [HttpPost("casos")]
    public async Task<IActionResult> AbrirCaso([FromBody] CasoCrearDto dto)
    {
        try
        {
            var caso = await _garantias.AbrirCasoAsync(dto);
            return CreatedAtAction(nameof(CasoPorId), new { id = caso.CasoGarantiaId }, caso);
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Pasa el caso a EnProceso; opcionalmente lo vincula a una orden de taller.</summary>
    [HttpPost("casos/{id:int}/iniciar")]
    public async Task<IActionResult> IniciarCaso(int id, [FromQuery] int? ordenTallerId)
    {
        try
        {
            return Ok(await _garantias.IniciarCasoAsync(id, ordenTallerId));
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Resuelve el caso: Reparacion, Reemplazo (mueve inventario: sale el reemplazo y
    /// reingresa el devuelto), NotaCredito o Rechazado.
    /// </summary>
    [HttpPost("casos/{id:int}/resolver")]
    public async Task<IActionResult> ResolverCaso(int id, [FromBody] CasoResolverDto dto)
    {
        try
        {
            return Ok(await _garantias.ResolverCasoAsync(id, dto, UsuarioId));
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Cierra un caso resuelto o rechazado.</summary>
    [HttpPost("casos/{id:int}/cerrar")]
    public async Task<IActionResult> CerrarCaso(int id)
    {
        try
        {
            return Ok(await _garantias.CerrarCasoAsync(id));
        }
        catch (GarantiaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Índice de fallas por modelo (vista vw_IndiceFallasPorModelo).</summary>
    [HttpGet("indice-fallas")]
    public async Task<IActionResult> IndiceFallas()
        => Ok(await _garantias.IndiceFallasAsync());
}
