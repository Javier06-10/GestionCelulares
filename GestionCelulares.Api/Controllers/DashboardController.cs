using GestionCelulares.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Indicadores del negocio en tiempo real: ventas y ganancia del día y del mes,
    /// inventario disponible, taller, créditos/morosidad, caja abierta y rankings del mes.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] int? sucursalId)
        => Ok(await _dashboard.ObtenerAsync(sucursalId));
}
