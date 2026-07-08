using System.Security.Claims;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Devoluciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class DevolucionesController : ControllerBase
{
    private readonly IDevolucionService _devoluciones;

    public DevolucionesController(IDevolucionService devoluciones) => _devoluciones = devoluciones;

    private int? UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Lista las notas de crédito (devoluciones) emitidas.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _devoluciones.ListarAsync());

    /// <summary>Registra una devolución: emite la Nota de Crédito (04) y revierte el inventario.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] DevolucionCrearDto dto)
    {
        try
        {
            return Ok(await _devoluciones.CrearAsync(dto, UsuarioId));
        }
        catch (VentaException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
