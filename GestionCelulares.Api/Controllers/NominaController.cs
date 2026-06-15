using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Nomina;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Nómina / pago a empleados. Reservado exclusivamente al administrador.</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class NominaController : ControllerBase
{
    private readonly INominaService _nomina;

    public NominaController(INominaService nomina) => _nomina = nomina;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Empleados activos disponibles para registrarles un pago.</summary>
    [HttpGet("empleados")]
    public async Task<IActionResult> Empleados() => Ok(await _nomina.EmpleadosAsync());

    /// <summary>Lista de pagos con filtros opcionales por empleado y rango de fechas.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? empleadoId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _nomina.ListarAsync(empleadoId, desde, hasta));

    /// <summary>Registra un pago a un empleado.</summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] PagoEmpleadoCrearDto dto)
    {
        try
        {
            return Ok(await _nomina.RegistrarAsync(dto, UsuarioId));
        }
        catch (NominaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
