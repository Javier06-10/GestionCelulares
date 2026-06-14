using GestionCelulares.Application.Common;
using GestionCelulares.Application.Faltantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FaltantesController : ControllerBase
{
    private readonly IFaltanteService _faltantes;

    public FaltantesController(IFaltanteService faltantes) => _faltantes = faltantes;

    /// <summary>Lista los productos agotados (automático) y la lista manual de reposición.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _faltantes.ListarAsync());

    /// <summary>Agrega un producto a la lista manual de faltantes.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Agregar([FromBody] FaltanteCrearDto dto)
    {
        try
        {
            return Ok(await _faltantes.AgregarAsync(dto));
        }
        catch (FaltanteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Marca un faltante manual como resuelto (lo quita de la lista).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/resolver")]
    public async Task<IActionResult> Resolver(int id)
    {
        try
        {
            await _faltantes.ResolverAsync(id);
            return NoContent();
        }
        catch (FaltanteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
