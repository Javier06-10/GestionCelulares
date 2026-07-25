using GestionCelulares.Application.Common;
using GestionCelulares.Application.Proveedores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)] // gestión de proveedores y pagos es administrativa
[Route("api/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedores;

    public ProveedoresController(IProveedorService proveedores) => _proveedores = proveedores;

    /// <summary>Busca proveedores por nombre, RNC o teléfono; permite filtrar por activos.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string? buscar, [FromQuery] bool? activos,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
        => Ok(await _proveedores.BuscarAsync(buscar, activos, pagina, tamano));

    /// <summary>Consulta un proveedor por su Id (incluye su balance actual).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> PorId(int id)
    {
        var dto = await _proveedores.PorIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Registra un nuevo proveedor.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ProveedorCrearDto dto)
    {
        try
        {
            var creado = await _proveedores.CrearAsync(dto);
            return CreatedAtAction(nameof(PorId), new { id = creado.ProveedorId }, creado);
        }
        catch (ProveedorException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Actualiza los datos de un proveedor (incluye activar/desactivar).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ProveedorActualizarDto dto)
    {
        try
        {
            return Ok(await _proveedores.ActualizarAsync(id, dto));
        }
        catch (ProveedorException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Historial de compras al proveedor.</summary>
    [HttpGet("{id:int}/compras")]
    public async Task<IActionResult> Compras(int id)
    {
        try
        {
            return Ok(await _proveedores.ComprasAsync(id));
        }
        catch (ProveedorException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Registra una compra al proveedor (aumenta el balance adeudado).</summary>
    [HttpPost("{id:int}/compras")]
    public async Task<IActionResult> RegistrarCompra(int id, [FromBody] CompraRegistroDto dto)
    {
        try
        {
            return Ok(await _proveedores.RegistrarCompraAsync(id, dto));
        }
        catch (ProveedorException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Historial de pagos hechos al proveedor.</summary>
    [HttpGet("{id:int}/pagos")]
    public async Task<IActionResult> Pagos(int id)
    {
        try
        {
            return Ok(await _proveedores.PagosAsync(id));
        }
        catch (ProveedorException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Registra un pago al proveedor (reduce el balance adeudado).</summary>
    [HttpPost("{id:int}/pagos")]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] PagoProveedorRegistroDto dto)
    {
        try
        {
            return Ok(await _proveedores.RegistrarPagoAsync(id, dto));
        }
        catch (ProveedorException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ----- Contactos -----

    /// <summary>Lista los contactos del proveedor.</summary>
    [HttpGet("{id:int}/contactos")]
    public async Task<IActionResult> Contactos(int id)
    {
        try { return Ok(await _proveedores.ContactosAsync(id)); }
        catch (ProveedorException ex) { return NotFound(new { error = ex.Message }); }
    }

    /// <summary>Agrega un contacto al proveedor.</summary>
    [HttpPost("{id:int}/contactos")]
    public async Task<IActionResult> AgregarContacto(int id, [FromBody] ContactoGuardarDto dto)
    {
        try { return Ok(await _proveedores.AgregarContactoAsync(id, dto)); }
        catch (ProveedorException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Actualiza un contacto.</summary>
    [HttpPut("contactos/{contactoId:int}")]
    public async Task<IActionResult> ActualizarContacto(int contactoId, [FromBody] ContactoGuardarDto dto)
    {
        try { return Ok(await _proveedores.ActualizarContactoAsync(contactoId, dto)); }
        catch (ProveedorException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Elimina un contacto.</summary>
    [HttpDelete("contactos/{contactoId:int}")]
    public async Task<IActionResult> EliminarContacto(int contactoId)
    {
        try { await _proveedores.EliminarContactoAsync(contactoId); return NoContent(); }
        catch (ProveedorException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
