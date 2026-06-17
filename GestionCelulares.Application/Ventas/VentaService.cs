using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Ventas;

public interface IVentaService
{
    Task<VentaDto> RegistrarAsync(VentaRegistroDto dto, int usuarioId);
    Task<VentaDto?> PorIdAsync(int id);
    Task<IReadOnlyList<VentaResumenDto>> BuscarAsync(DateTime? desde, DateTime? hasta, int? sucursalId, int? clienteId);
    Task<IReadOnlyList<MetodoPagoDto>> MetodosPagoAsync();
}

public class VentaService : IVentaService
{
    private readonly IApplicationDbContext _db;
    private readonly IVentaProcedures _procedures;

    public VentaService(IApplicationDbContext db, IVentaProcedures procedures)
    {
        _db = db;
        _procedures = procedures;
    }

    public async Task<VentaDto> RegistrarAsync(VentaRegistroDto dto, int usuarioId)
    {
        if (dto.Detalles.Count == 0)
            throw new VentaException("La venta no tiene detalles.");

        // El POS exige una sesión de caja abierta en la sucursal (US-066)
        var sesionCajaId = await _db.SesionesCaja
            .Where(s => s.SucursalId == dto.SucursalId && s.Estado == "Abierta")
            .Select(s => (int?)s.SesionCajaId)
            .FirstOrDefaultAsync()
            ?? throw new VentaException("No hay una sesión de caja abierta en esta sucursal. Abre la caja antes de vender.");

        if (dto.EsCredito)
        {
            if (dto.ClienteId is null)
                throw new VentaException("La venta a crédito requiere un cliente.");
        }
        else if (dto.MetodoPagoId is null)
        {
            throw new VentaException("Debe indicar el método de pago para una venta de contado.");
        }

        if (dto.MetodoPagoId.HasValue &&
            !await _db.MetodosPago.AnyAsync(m => m.MetodoPagoId == dto.MetodoPagoId.Value))
            throw new VentaException("El método de pago indicado no existe.");

        if (dto.ClienteId.HasValue)
        {
            var cliente = await _db.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId.Value)
                ?? throw new VentaException("El cliente indicado no existe.");

            if (dto.EsCredito && cliente.Bloqueado)
                throw new VentaException($"El cliente '{cliente.Nombre}' está bloqueado y no puede comprar a crédito.");
        }

        // Stock suficiente para accesorios no serializados (el SP descuenta sin validar)
        foreach (var linea in dto.Detalles.Where(d => d.ImeiId is null))
        {
            var variante = await _db.ProductoVariantes.AsNoTracking()
                .FirstOrDefaultAsync(v => v.VarianteId == linea.VarianteId)
                ?? throw new VentaException($"La variante {linea.VarianteId} no existe.");

            if (variante.StockNoSerial < linea.Cantidad)
                throw new VentaException(
                    $"Stock insuficiente de la variante {linea.VarianteId}: hay {variante.StockNoSerial} y se piden {linea.Cantidad}.");
        }

        // Número de factura secuencial (si el cliente no lo envía explícito)
        if (string.IsNullOrWhiteSpace(dto.NumeroFactura))
            dto.NumeroFactura = await _procedures.ProximoNumeroFacturaAsync();

        // usp_Venta_Registrar hace el resto en una transacción:
        // valida IMEIs, inserta cabecera/detalle/pago, marca vendidos, kardex y totales
        var ventaId = await _procedures.RegistrarAsync(dto, usuarioId, sesionCajaId);

        return (await PorIdAsync(ventaId))!;
    }

    public async Task<VentaDto?> PorIdAsync(int id)
        => await _db.Ventas.AsNoTracking()
            .Where(v => v.VentaId == id)
            .Select(ProyeccionVenta)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<VentaResumenDto>> BuscarAsync(DateTime? desde, DateTime? hasta, int? sucursalId, int? clienteId)
    {
        var q = _db.Ventas.AsNoTracking();

        if (desde.HasValue) q = q.Where(v => v.Fecha >= desde.Value);
        if (hasta.HasValue) q = q.Where(v => v.Fecha < hasta.Value.AddDays(1));
        if (sucursalId.HasValue) q = q.Where(v => v.SucursalId == sucursalId.Value);
        if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId.Value);

        return await q.OrderByDescending(v => v.Fecha)
            .Select(v => new VentaResumenDto
            {
                VentaId = v.VentaId,
                NumeroFactura = v.NumeroFactura,
                SucursalId = v.SucursalId,
                Cliente = v.Cliente == null ? null : v.Cliente.Nombre,
                Fecha = v.Fecha,
                Total = v.Total,
                EsCredito = v.EsCredito,
                Estado = v.Estado
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MetodoPagoDto>> MetodosPagoAsync()
        => await _db.MetodosPago.AsNoTracking()
            .Where(m => m.Nombre != "Apartado")   // método interno; no se ofrece al cajero
            .OrderBy(m => m.MetodoPagoId)
            .Select(m => new MetodoPagoDto { MetodoPagoId = m.MetodoPagoId, Nombre = m.Nombre })
            .ToListAsync();

    private static readonly Expression<Func<Venta, VentaDto>> ProyeccionVenta = v => new VentaDto
    {
        VentaId = v.VentaId,
        NumeroFactura = v.NumeroFactura,
        SucursalId = v.SucursalId,
        ClienteId = v.ClienteId,
        Cliente = v.Cliente == null ? null : v.Cliente.Nombre,
        UsuarioId = v.UsuarioId,
        SesionCajaId = v.SesionCajaId,
        Fecha = v.Fecha,
        Subtotal = v.Subtotal,
        Descuento = v.Descuento,
        Impuesto = v.Impuesto,
        Total = v.Total,
        EsCredito = v.EsCredito,
        Estado = v.Estado,
        Detalles = v.Detalles.Select(d => new VentaDetalleDto
        {
            VentaDetalleId = d.VentaDetalleId,
            ImeiId = d.ImeiId,
            Imei = d.Imei == null ? null : d.Imei.Imei,
            VarianteId = d.VarianteId,
            Producto = d.Variante.Producto.Nombre,
            Color = d.Variante.Color,
            Almacenamiento = d.Variante.Almacenamiento,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Impuesto = d.Impuesto,
            Total = d.Total
        }).ToList(),
        Pagos = v.Pagos.Select(p => new VentaPagoDto
        {
            MetodoPagoId = p.MetodoPagoId,
            MetodoPago = p.MetodoPago.Nombre,
            Monto = p.Monto,
            Referencia = p.Referencia
        }).ToList()
    };
}
