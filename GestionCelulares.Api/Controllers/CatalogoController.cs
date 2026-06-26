using GestionCelulares.Application.Catalogo;
using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/catalogo")]
public class CatalogoController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public CatalogoController(ICatalogoService catalogo) => _catalogo = catalogo;

    // ----- Marcas -----

    /// <summary>Lista las marcas.</summary>
    [HttpGet("marcas")]
    public async Task<IActionResult> Marcas() => Ok(await _catalogo.MarcasAsync());

    /// <summary>Crea una marca (nombre único).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("marcas")]
    public async Task<IActionResult> CrearMarca([FromBody] NombreDto dto)
    {
        try
        {
            return Ok(await _catalogo.CrearMarcaAsync(dto));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ----- Categorías -----

    /// <summary>Lista las categorías.</summary>
    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias() => Ok(await _catalogo.CategoriasAsync());

    /// <summary>Crea una categoría (nombre único).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("categorias")]
    public async Task<IActionResult> CrearCategoria([FromBody] NombreDto dto)
    {
        try
        {
            return Ok(await _catalogo.CrearCategoriaAsync(dto));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ----- Productos -----

    /// <summary>Busca productos por nombre o marca; permite filtrar por activos.</summary>
    [HttpGet("productos")]
    public async Task<IActionResult> Productos([FromQuery] string? buscar, [FromQuery] bool? activos)
        => Ok(await _catalogo.BuscarProductosAsync(buscar, activos));

    /// <summary>Consulta un producto con sus variantes.</summary>
    [HttpGet("productos/{id:int}")]
    public async Task<IActionResult> ProductoPorId(int id)
    {
        var dto = await _catalogo.ProductoPorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Crea un producto, opcionalmente con sus variantes iniciales.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("productos")]
    public async Task<IActionResult> CrearProducto([FromBody] ProductoCrearDto dto)
    {
        try
        {
            var creado = await _catalogo.CrearProductoAsync(dto);
            return CreatedAtAction(nameof(ProductoPorId), new { id = creado.ProductoId }, creado);
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza un producto (incluye activar/desactivar).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("productos/{id:int}")]
    public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ProductoActualizarDto dto)
    {
        try
        {
            return Ok(await _catalogo.ActualizarProductoAsync(id, dto));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ----- Variantes -----

    /// <summary>Agrega una variante (color, almacenamiento, condición, precios) a un producto.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("productos/{id:int}/variantes")]
    public async Task<IActionResult> AgregarVariante(int id, [FromBody] VarianteCrearDto dto)
    {
        try
        {
            return Ok(await _catalogo.AgregarVarianteAsync(id, dto));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza una variante (gestión de precios y stock no serializado).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("variantes/{id:int}")]
    public async Task<IActionResult> ActualizarVariante(int id, [FromBody] VarianteActualizarDto dto)
    {
        try
        {
            return Ok(await _catalogo.ActualizarVarianteAsync(id, dto));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Genera y asigna un código de barras EAN-13 único a una variante sin código.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("variantes/{id:int}/generar-codigo")]
    public async Task<IActionResult> GenerarCodigo(int id)
    {
        try
        {
            return Ok(await _catalogo.GenerarCodigoBarrasAsync(id));
        }
        catch (CatalogoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Genera código de barras para todas las variantes que no tengan uno.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("variantes/generar-codigos")]
    public async Task<IActionResult> GenerarCodigosFaltantes()
        => Ok(await _catalogo.GenerarCodigosFaltantesAsync());
}
