using GestionCelulares.Application.Rnc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

/// <summary>Consulta del padrón de RNC de la DGII (auto-completar razón social).</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RncController : ControllerBase
{
    private readonly IRncConsultaService _rnc;

    public RncController(IRncConsultaService rnc) => _rnc = rnc;

    [HttpGet("{rnc}")]
    public async Task<IActionResult> Consultar(string rnc)
    {
        var dto = await _rnc.ConsultarAsync(rnc);
        return dto is null ? NotFound(new { error = "RNC/Cédula no encontrado en el padrón." }) : Ok(dto);
    }
}
