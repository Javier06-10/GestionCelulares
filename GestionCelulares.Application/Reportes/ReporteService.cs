using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Reportes;

public interface IReporteService
{
    Task<ReporteVentasDto> VentasAsync(DateTime desde, DateTime hasta, int? sucursalId);
    Task<ReporteInventarioDto> InventarioAsync(int? sucursalId);
    Task<ReporteMorosidadDto> MorosidadAsync();
    Task<ReporteCajaDto> CajaAsync(DateTime desde, DateTime hasta, int? sucursalId);
    Task<ReporteTallerDto> TallerAsync(DateTime desde, DateTime hasta, int? sucursalId);
    Task<ReporteCobrosDto> CobrosAsync(DateTime desde, DateTime hasta);
}

public class ReporteService : IReporteService
{
    private readonly IApplicationDbContext _db;

    public ReporteService(IApplicationDbContext db) => _db = db;

    public async Task<ReporteVentasDto> VentasAsync(DateTime desde, DateTime hasta, int? sucursalId)
    {
        var fin = hasta.Date.AddDays(1);
        var q = _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Completada" && v.Fecha >= desde.Date && v.Fecha < fin);
        if (sucursalId.HasValue)
            q = q.Where(v => v.SucursalId == sucursalId.Value);

        var totales = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Count(),
                Subtotal = g.Sum(v => v.Subtotal),
                Descuento = g.Sum(v => v.Descuento),
                Impuesto = g.Sum(v => v.Impuesto),
                Total = g.Sum(v => v.Total)
            })
            .FirstOrDefaultAsync();

        var porDia = await q
            .SelectMany(v => v.Detalles, (v, d) => new
            {
                Fecha = v.Fecha.Date,
                d.Total,
                Ganancia = d.PrecioUnitario * d.Cantidad - d.Descuento
                    - (d.ImeiId != null ? d.Imei!.PrecioCosto : d.Variante.PrecioCosto * d.Cantidad),
                v.VentaId
            })
            .GroupBy(x => x.Fecha)
            .Select(g => new VentaDiaDto
            {
                Fecha = g.Key,
                Cantidad = g.Select(x => x.VentaId).Distinct().Count(),
                Total = g.Sum(x => x.Total),
                Ganancia = g.Sum(x => x.Ganancia)
            })
            .OrderBy(d => d.Fecha)
            .ToListAsync();

        return new ReporteVentasDto
        {
            Desde = desde.Date,
            Hasta = hasta.Date,
            Cantidad = totales?.Cantidad ?? 0,
            Subtotal = totales?.Subtotal ?? 0,
            Descuento = totales?.Descuento ?? 0,
            Impuesto = totales?.Impuesto ?? 0,
            Total = totales?.Total ?? 0,
            Ganancia = porDia.Sum(d => d.Ganancia),
            PorDia = porDia
        };
    }

    public async Task<ReporteInventarioDto> InventarioAsync(int? sucursalId)
    {
        // Costo real desde los IMEIs disponibles
        var costoQ = _db.InventarioImeis.AsNoTracking().Where(i => i.Estado == "Disponible");
        if (sucursalId.HasValue)
            costoQ = costoQ.Where(i => i.SucursalId == sucursalId.Value);

        var costoPorVariante = await costoQ
            .GroupBy(i => new { i.VarianteId, i.SucursalId })
            .Select(g => new { g.Key.VarianteId, g.Key.SucursalId, Costo = g.Sum(i => i.PrecioCosto) })
            .ToListAsync();

        var vistaQ = _db.InventarioDisponible.AsNoTracking();
        if (sucursalId.HasValue)
            vistaQ = vistaQ.Where(x => x.SucursalId == sucursalId.Value);
        var vista = await vistaQ.OrderBy(x => x.Producto).ToListAsync();

        var lineas = vista.Select(x => new InventarioLineaDto
        {
            Producto = x.Producto,
            Marca = x.Marca,
            Color = x.Color,
            Almacenamiento = x.Almacenamiento,
            SucursalId = x.SucursalId,
            Disponibles = x.Disponibles,
            PrecioVenta = x.PrecioVenta,
            ValorCosto = costoPorVariante
                .Where(c => c.VarianteId == x.VarianteId && c.SucursalId == x.SucursalId)
                .Sum(c => c.Costo)
        }).ToList();

        return new ReporteInventarioDto
        {
            Equipos = lineas.Sum(l => l.Disponibles),
            ValorCosto = lineas.Sum(l => l.ValorCosto),
            ValorVenta = lineas.Sum(l => l.PrecioVenta * l.Disponibles),
            Lineas = lineas
        };
    }

    public async Task<ReporteMorosidadDto> MorosidadAsync()
    {
        var clientes = await _db.CuotasVencidas.AsNoTracking()
            .GroupBy(c => new { c.ClienteId, c.Cliente, c.Telefono })
            .Select(g => new MorosidadClienteDto
            {
                ClienteId = g.Key.ClienteId,
                Cliente = g.Key.Cliente,
                Telefono = g.Key.Telefono,
                CuotasVencidas = g.Count(),
                MontoVencido = g.Sum(c => c.Saldo),
                DiasMaxVencido = g.Max(c => c.DiasVencido)
            })
            .OrderByDescending(c => c.MontoVencido)
            .ToListAsync();

        return new ReporteMorosidadDto
        {
            ClientesEnMora = clientes.Count,
            MontoVencidoTotal = clientes.Sum(c => c.MontoVencido),
            Clientes = clientes
        };
    }

    public async Task<ReporteCajaDto> CajaAsync(DateTime desde, DateTime hasta, int? sucursalId)
    {
        var fin = hasta.Date.AddDays(1);
        var q = _db.SesionesCaja.AsNoTracking()
            .Where(s => s.FechaApertura >= desde.Date && s.FechaApertura < fin);
        if (sucursalId.HasValue)
            q = q.Where(s => s.SucursalId == sucursalId.Value);

        var detalle = await q.OrderBy(s => s.FechaApertura)
            .Select(s => new CajaSesionDto
            {
                SesionCajaId = s.SesionCajaId,
                SucursalId = s.SucursalId,
                FechaApertura = s.FechaApertura,
                FechaCierre = s.FechaCierre,
                MontoApertura = s.MontoApertura,
                MontoCierre = s.MontoCierre,
                Diferencia = s.Diferencia,
                Estado = s.Estado
            })
            .ToListAsync();

        return new ReporteCajaDto
        {
            Sesiones = detalle.Count,
            TotalDiferencias = detalle.Sum(s => s.Diferencia ?? 0),
            Detalle = detalle
        };
    }

    public async Task<ReporteTallerDto> TallerAsync(DateTime desde, DateTime hasta, int? sucursalId)
    {
        var fin = hasta.Date.AddDays(1);
        var q = _db.OrdenesTaller.AsNoTracking()
            .Where(o => (o.Estado == "Entregado" && o.FechaEntrega >= desde.Date && o.FechaEntrega < fin)
                     || (o.Estado == "Cancelado" && o.FechaRecepcion >= desde.Date && o.FechaRecepcion < fin));
        if (sucursalId.HasValue)
            q = q.Where(o => o.SucursalId == sucursalId.Value);

        var entregadas = await q.Where(o => o.Estado == "Entregado")
            .Select(o => new
            {
                o.TecnicoId,
                Tecnico = o.Tecnico == null ? "(sin técnico)" : o.Tecnico.NombreCompleto,
                Costo = o.CostoFinal ?? 0,
                o.ComisionTecnico,
                Repuestos = o.Repuestos.Sum(r => (decimal?)(r.Costo * r.Cantidad)) ?? 0
            })
            .ToListAsync();

        var canceladas = await q.CountAsync(o => o.Estado == "Cancelado");

        return new ReporteTallerDto
        {
            Entregadas = entregadas.Count,
            Canceladas = canceladas,
            IngresosPorReparacion = entregadas.Sum(e => e.Costo),
            CostoRepuestos = entregadas.Sum(e => e.Repuestos),
            ComisionesTecnicos = entregadas.Sum(e => e.ComisionTecnico),
            PorTecnico = entregadas
                .GroupBy(e => new { e.TecnicoId, e.Tecnico })
                .Select(g => new TallerTecnicoDto
                {
                    TecnicoId = g.Key.TecnicoId ?? 0,
                    Tecnico = g.Key.Tecnico,
                    OrdenesEntregadas = g.Count(),
                    Ingresos = g.Sum(e => e.Costo),
                    Comision = g.Sum(e => e.ComisionTecnico)
                })
                .OrderByDescending(t => t.Ingresos)
                .ToList()
        };
    }

    public async Task<ReporteCobrosDto> CobrosAsync(DateTime desde, DateTime hasta)
    {
        var fin = hasta.Date.AddDays(1);

        var porDia = await _db.PagosCredito.AsNoTracking()
            .Where(p => p.Fecha >= desde.Date && p.Fecha < fin)
            .GroupBy(p => p.Fecha.Date)
            .Select(g => new CobroDiaDto
            {
                Fecha = g.Key,
                Pagos = g.Count(),
                Total = g.Sum(p => p.Monto)
            })
            .OrderBy(d => d.Fecha)
            .ToListAsync();

        return new ReporteCobrosDto
        {
            Pagos = porDia.Sum(d => d.Pagos),
            TotalCobrado = porDia.Sum(d => d.Total),
            PorDia = porDia
        };
    }
}
