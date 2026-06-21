using GestionCelulares.Application.Common;
using GestionCelulares.Application.Contabilidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Catálogo de cuentas (plan contable). Solo administrador.</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class CuentasController : ControllerBase
{
    private readonly ICuentaService _cuentas;

    public CuentasController(ICuentaService cuentas) => _cuentas = cuentas;

    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _cuentas.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CuentaCrearDto dto)
        => await Ejecutar(() => _cuentas.CrearAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CuentaActualizarDto dto)
        => await Ejecutar(() => _cuentas.ActualizarAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _cuentas.EliminarAsync(id);
            return NoContent();
        }
        catch (ContabilidadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<IActionResult> Ejecutar(Func<Task<CuentaDto>> accion)
    {
        try
        {
            return Ok(await accion());
        }
        catch (ContabilidadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
