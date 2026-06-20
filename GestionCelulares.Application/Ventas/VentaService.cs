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
    Task<VentaDto> AnularAsync(int ventaId, AnularVentaDto dto, int? usuarioId);
}

public class VentaService : IVentaService
{
    private readonly IApplicationDbContext _db;
    private readonly IVentaProcedures _procedures;
    private readonly INcfProcedures _ncf;

    public VentaService(IApplicationDbContext db, IVentaProcedures procedures, INcfProcedures ncf)
    {
        _db = db;
        _procedures = procedures;
        _ncf = ncf;
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

        Cliente? clienteVenta = null;
        if (dto.ClienteId.HasValue)
        {
            clienteVenta = await _db.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId.Value)
                ?? throw new VentaException("El cliente indicado no existe.");

            if (dto.EsCredito && clienteVenta.Bloqueado)
                throw new VentaException($"El cliente '{clienteVenta.Nombre}' está bloqueado y no puede comprar a crédito.");
        }

        // La Factura de Crédito Fiscal (01) exige identificar al cliente con RNC/Cédula
        if (dto.TipoComprobante == "01" && SoloDigitos(clienteVenta?.Cedula).Length == 0)
            throw new VentaException("La Factura de Crédito Fiscal requiere un cliente con RNC o cédula.");

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

        // Asigna el NCF: 01 (Crédito Fiscal) si el cliente tiene RNC, 02 (Consumo) si no.
        // Si no hay rango de NCF configurado/activo, la venta queda sin NCF (se reporta en el 607).
        // La venta ya quedó registrada por el SP: un fallo aquí NO debe tumbarla.
        try
        {
            // Tipo de comprobante: el que eligió el cajero (01/02); si no, se infiere por el cliente.
            var tipoNcf = dto.TipoComprobante is "01" or "02"
                ? dto.TipoComprobante
                : (SoloDigitos(clienteVenta?.Cedula).Length == 9 ? "01" : "02");
            var ncf = await _ncf.SiguienteNcfAsync(tipoNcf);
            if (!string.IsNullOrEmpty(ncf))
            {
                var venta = await _db.Ventas.FirstAsync(v => v.VentaId == ventaId);
                venta.Ncf = ncf;
                await _db.SaveChangesAsync();
            }
        }
        catch
        {
            // El NCF se podrá asignar/corregir luego; la venta ya está registrada.
        }

        return (await PorIdAsync(ventaId))!;
    }

    private static string SoloDigitos(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    private static readonly string[] TiposAnulacion =
        { "01", "02", "03", "04", "05", "06", "07", "08", "09" };

    public async Task<VentaDto> AnularAsync(int ventaId, AnularVentaDto dto, int? usuarioId)
    {
        var venta = await _db.Ventas.Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.VentaId == ventaId)
            ?? throw new VentaException("La venta no existe.");

        if (venta.Estado == "Anulada")
            throw new VentaException("La venta ya está anulada.");
        if (venta.Estado != "Completada")
            throw new VentaException("Solo se pueden anular ventas completadas.");
        if (!TiposAnulacion.Contains(dto.TipoAnulacion))
            throw new VentaException("El tipo de anulación no es válido.");

        // Revertir el inventario (los bienes vuelven al stock)
        foreach (var d in venta.Detalles)
        {
            if (d.ImeiId.HasValue)
            {
                var imei = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == d.ImeiId.Value);
                if (imei is not null && imei.Estado == "Vendido")
                {
                    imei.Estado = "Disponible";
                    _db.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ImeiId = imei.ImeiId,
                        Tipo = "Anulacion",
                        SucursalDestino = venta.SucursalId,
                        Referencia = $"Anulación venta #{venta.VentaId}",
                        UsuarioId = usuarioId,
                        Fecha = DateTime.Now
                    });
                }
            }
            else
            {
                var variante = await _db.ProductoVariantes.FirstOrDefaultAsync(v => v.VarianteId == d.VarianteId);
                if (variante is not null) variante.StockNoSerial += d.Cantidad;
            }
        }

        venta.Estado = "Anulada";

        // Si tenía NCF, queda registrado como comprobante anulado (alimenta el 608)
        if (!string.IsNullOrWhiteSpace(venta.Ncf))
        {
            _db.ComprobantesAnulados.Add(new ComprobanteAnulado
            {
                Ncf = venta.Ncf,
                FechaComprobante = venta.Fecha,
                TipoAnulacion = dto.TipoAnulacion,
                Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim(),
                VentaId = venta.VentaId,
                UsuarioId = usuarioId,
                FechaRegistro = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
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
                Ncf = v.Ncf,
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
        Ncf = v.Ncf,
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
