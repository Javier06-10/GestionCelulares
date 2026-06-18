using GestionCelulares.Application.Common;
using GestionCelulares.Application.Ncf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Configuración de los rangos de NCF autorizados por la DGII. Solo administrador.</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class NcfController : ControllerBase
{
    private readonly INcfService _ncf;

    public NcfController(INcfService ncf) => _ncf = ncf;

    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _ncf.ListarAsync());

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Configurar(int id, [FromBody] SecuenciaNcfConfigDto dto)
    {
        try
        {
            return Ok(await _ncf.ConfigurarAsync(id, dto));
        }
        catch (NcfException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
