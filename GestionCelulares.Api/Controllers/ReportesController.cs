using GestionCelulares.Application.Common;
using GestionCelulares.Application.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)] // los reportes son gerenciales
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reportes;
    private readonly IReporteFiscalService _fiscal;

    public ReportesController(IReporteService reportes, IReporteFiscalService fiscal)
    {
        _reportes = reportes;
        _fiscal = fiscal;
    }

    /// <summary>Ventas del período con totales, ITBIS, ganancia y desglose por día.</summary>
    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] int? sucursalId)
        => Ok(await _reportes.VentasAsync(desde, hasta, sucursalId));

    /// <summary>Stock disponible valorizado a costo y a precio de venta.</summary>
    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario([FromQuery] int? sucursalId)
        => Ok(await _reportes.InventarioAsync(sucursalId));

    /// <summary>Morosidad por cliente: cuotas vencidas, monto y días máximos de atraso.</summary>
    [HttpGet("morosidad")]
    public async Task<IActionResult> Morosidad()
        => Ok(await _reportes.MorosidadAsync());

    /// <summary>Sesiones de caja del período con sus diferencias de arqueo.</summary>
    [HttpGet("caja")]
    public async Task<IActionResult> Caja(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] int? sucursalId)
        => Ok(await _reportes.CajaAsync(desde, hasta, sucursalId));

    /// <summary>Producción del taller: órdenes entregadas, ingresos, repuestos y comisiones por técnico.</summary>
    [HttpGet("taller")]
    public async Task<IActionResult> Taller(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] int? sucursalId)
        => Ok(await _reportes.TallerAsync(desde, hasta, sucursalId));

    /// <summary>Cobros de cuotas de crédito por día.</summary>
    [HttpGet("cobros")]
    public async Task<IActionResult> Cobros([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        => Ok(await _reportes.CobrosAsync(desde, hasta));

    /// <summary>Formato 607 (ventas) de la DGII para un mes; devuelve resumen + contenido TXT.</summary>
    [HttpGet("607")]
    public async Task<IActionResult> Reporte607([FromQuery] int anio, [FromQuery] int mes)
    {
        if (mes < 1 || mes > 12) return BadRequest(new { error = "Mes inválido." });
        return Ok(await _fiscal.Generar607Async(anio, mes));
    }
}
