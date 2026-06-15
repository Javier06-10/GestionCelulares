using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync(int? sucursalId);
}

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> ObtenerAsync(int? sucursalId)
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var manana = hoy.AddDays(1);
        var inicioMesSiguiente = inicioMes.AddMonths(1);

        var dto = new DashboardDto
        {
            VentasHoy = await VentasAsync(hoy, manana, sucursalId),
            VentasMes = await VentasAsync(inicioMes, inicioMesSiguiente, sucursalId),
            Inventario = await InventarioAsync(sucursalId),
            Taller = await TallerAsync(sucursalId),
            Creditos = await CreditosAsync(),
            Caja = await CajaAsync(sucursalId),
            TopProductosMes = await TopProductosAsync(inicioMes, inicioMesSiguiente, sucursalId),
            TopVendedoresMes = await TopVendedoresAsync(inicioMes, inicioMesSiguiente, sucursalId),
            VentasUltimos14Dias = await VentasPorDiaAsync(hoy.AddDays(-13), manana, sucursalId)
        };
        return dto;
    }

    private async Task<List<VentaDiaDto>> VentasPorDiaAsync(DateTime desde, DateTime hasta, int? sucursalId)
    {
        // Totales por día existentes en el rango
        var porDia = await VentasCompletadas(desde, hasta, sucursalId)
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new { Fecha = g.Key, Total = g.Sum(v => v.Total) })
            .ToListAsync();

        // Rellenar los días sin ventas con 0 para una serie continua
        var mapa = porDia.ToDictionary(x => x.Fecha, x => x.Total);
        var serie = new List<VentaDiaDto>();
        for (var d = desde.Date; d < hasta.Date; d = d.AddDays(1))
            serie.Add(new VentaDiaDto { Fecha = d, Total = mapa.GetValueOrDefault(d, 0), Ganancia = 0 });
        return serie;
    }

    private IQueryable<Venta> VentasCompletadas(DateTime desde, DateTime hasta, int? sucursalId)
    {
        var q = _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Completada" && v.Fecha >= desde && v.Fecha < hasta);
        if (sucursalId.HasValue)
            q = q.Where(v => v.SucursalId == sucursalId.Value);
        return q;
    }

    private async Task<VentasIndicadorDto> VentasAsync(DateTime desde, DateTime hasta, int? sucursalId)
    {
        var ventas = VentasCompletadas(desde, hasta, sucursalId);

        var resumen = await ventas
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .FirstOrDefaultAsync();

        // Ganancia = (precio - descuento) - costo (IMEI al costo del equipo; accesorio al costo de la variante)
        var ganancia = await ventas
            .SelectMany(v => v.Detalles)
            .Select(d => d.PrecioUnitario * d.Cantidad - d.Descuento
                - (d.ImeiId != null ? d.Imei!.PrecioCosto : d.Variante.PrecioCosto * d.Cantidad))
            .SumAsync(x => (decimal?)x) ?? 0;

        return new VentasIndicadorDto
        {
            Cantidad = resumen?.Cantidad ?? 0,
            Total = resumen?.Total ?? 0,
            Ganancia = ganancia
        };
    }

    private async Task<InventarioIndicadorDto> InventarioAsync(int? sucursalId)
    {
        // Equipos serializados disponibles (por IMEI, con sucursal)
        var qEquipos = _db.InventarioImeis.AsNoTracking().Where(i => i.Estado == "Disponible");
        if (sucursalId.HasValue)
            qEquipos = qEquipos.Where(i => i.SucursalId == sucursalId.Value);

        var equipos = await qEquipos
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Valor = g.Sum(i => i.PrecioCosto) })
            .FirstOrDefaultAsync();

        // Accesorios / productos no serializados (stock por variante)
        var accesorios = await _db.ProductoVariantes.AsNoTracking()
            .Where(v => v.Activo && !v.Producto.Serializado && v.StockNoSerial > 0)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Sum(v => v.StockNoSerial),
                Valor = g.Sum(v => v.StockNoSerial * v.PrecioCosto)
            })
            .FirstOrDefaultAsync();

        return new InventarioIndicadorDto
        {
            EquiposDisponibles = equipos?.Cantidad ?? 0,
            AccesoriosDisponibles = accesorios?.Cantidad ?? 0,
            ValorCosto = (equipos?.Valor ?? 0) + (accesorios?.Valor ?? 0)
        };
    }

    private async Task<TallerIndicadorDto> TallerAsync(int? sucursalId)
    {
        var q = _db.OrdenesTaller.AsNoTracking();
        if (sucursalId.HasValue)
            q = q.Where(o => o.SucursalId == sucursalId.Value);

        var grupos = await q
            .Where(o => o.Estado == "Recibido" || o.Estado == "EnReparacion" || o.Estado == "Reparado")
            .GroupBy(o => o.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        return new TallerIndicadorDto
        {
            Recibidos = grupos.FirstOrDefault(g => g.Estado == "Recibido")?.Cantidad ?? 0,
            EnReparacion = grupos.FirstOrDefault(g => g.Estado == "EnReparacion")?.Cantidad ?? 0,
            Reparados = grupos.FirstOrDefault(g => g.Estado == "Reparado")?.Cantidad ?? 0
        };
    }

    private async Task<CreditosIndicadorDto> CreditosAsync()
    {
        var creditos = await _db.Creditos.AsNoTracking()
            .Where(c => c.Estado == "Activo" || c.Estado == "EnMora")
            .GroupBy(c => c.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count(), Saldo = g.Sum(c => c.Saldo) })
            .ToListAsync();

        var vencidas = await _db.CuotasVencidas.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Monto = g.Sum(c => c.Saldo) })
            .FirstOrDefaultAsync();

        return new CreditosIndicadorDto
        {
            Activos = creditos.FirstOrDefault(g => g.Estado == "Activo")?.Cantidad ?? 0,
            EnMora = creditos.FirstOrDefault(g => g.Estado == "EnMora")?.Cantidad ?? 0,
            SaldoPendiente = creditos.Sum(g => g.Saldo),
            CuotasVencidas = vencidas?.Cantidad ?? 0,
            MontoVencido = vencidas?.Monto ?? 0
        };
    }

    private async Task<CajaIndicadorDto> CajaAsync(int? sucursalId)
    {
        var q = _db.SesionesCaja.AsNoTracking().Where(s => s.Estado == "Abierta");
        if (sucursalId.HasValue)
            q = q.Where(s => s.SucursalId == sucursalId.Value);

        var sesion = await q
            .Select(s => new CajaIndicadorDto
            {
                SesionAbierta = true,
                SesionCajaId = s.SesionCajaId,
                MontoApertura = s.MontoApertura,
                Ingresos = s.Movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => (decimal?)m.Monto) ?? 0,
                Egresos = s.Movimientos.Where(m => m.Tipo == "Egreso").Sum(m => (decimal?)m.Monto) ?? 0
            })
            .FirstOrDefaultAsync();

        return sesion ?? new CajaIndicadorDto();
    }

    private async Task<List<TopProductoDto>> TopProductosAsync(DateTime desde, DateTime hasta, int? sucursalId)
        => await VentasCompletadas(desde, hasta, sucursalId)
            .SelectMany(v => v.Detalles)
            .GroupBy(d => d.Variante.Producto.Nombre)
            .Select(g => new TopProductoDto
            {
                Producto = g.Key,
                Unidades = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(t => t.Unidades)
            .Take(5)
            .ToListAsync();

    private async Task<List<TopVendedorDto>> TopVendedoresAsync(DateTime desde, DateTime hasta, int? sucursalId)
        => await VentasCompletadas(desde, hasta, sucursalId)
            .GroupBy(v => v.UsuarioId)
            .Select(g => new TopVendedorDto
            {
                UsuarioId = g.Key,
                Vendedor = _db.Usuarios.Where(u => u.UsuarioId == g.Key)
                    .Select(u => u.NombreCompleto).FirstOrDefault() ?? "",
                Ventas = g.Count(),
                Total = g.Sum(v => v.Total)
            })
            .OrderByDescending(t => t.Total)
            .Take(5)
            .ToListAsync();
}
