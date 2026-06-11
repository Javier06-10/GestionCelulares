using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventas;

    public VentasController(IVentaService ventas) => _ventas = ventas;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Lista ventas con filtros por fecha, sucursal y cliente.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int? sucursalId, [FromQuery] int? clienteId)
        => Ok(await _ventas.BuscarAsync(desde, hasta, sucursalId, clienteId));

    /// <summary>Consulta una venta con su detalle y pagos.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _ventas.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Métodos de pago disponibles para el POS.</summary>
    [HttpGet("metodos-pago")]
    public async Task<IActionResult> MetodosPago() => Ok(await _ventas.MetodosPagoAsync());

    /// <summary>
    /// Registra una venta (usp_Venta_Registrar): exige caja abierta, valida cliente y stock,
    /// marca los IMEI vendidos, registra kardex, calcula ITBIS y guarda el pago si es de contado.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] VentaRegistroDto dto)
    {
        if (UsuarioId is null)
            return Unauthorized();

        try
        {
            var venta = await _ventas.RegistrarAsync(dto, UsuarioId.Value);
            return CreatedAtAction(nameof(PorId), new { id = venta.VentaId }, venta);
        }
        catch (VentaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
