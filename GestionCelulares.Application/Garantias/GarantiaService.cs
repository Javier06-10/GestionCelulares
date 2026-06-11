using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Garantias;

public interface IGarantiaService
{
    Task<GarantiaDto> CrearAsync(GarantiaCrearDto dto);
    Task<GarantiaDto?> PorIdAsync(int id);
    Task<GarantiaVigenteDto?> PorImeiAsync(string imei);
    Task<IReadOnlyList<GarantiaDto>> BuscarAsync(int? clienteId, bool? vigentes);
    Task AnularAsync(int id);

    Task<CasoDto> AbrirCasoAsync(CasoCrearDto dto);
    Task<CasoDto?> CasoPorIdAsync(int id);
    Task<IReadOnlyList<CasoDto>> CasosAsync(string? estado, int? clienteId);
    Task<CasoDto> IniciarCasoAsync(int id, int? ordenTallerId);
    Task<CasoDto> ResolverCasoAsync(int id, CasoResolverDto dto, int? usuarioId);
    Task<CasoDto> CerrarCasoAsync(int id);

    Task<IReadOnlyList<IndiceFallaDto>> IndiceFallasAsync();
}

public class GarantiaService : IGarantiaService
{
    private static readonly string[] Resoluciones = { "Reparacion", "Reemplazo", "NotaCredito", "Rechazado" };

    private readonly IApplicationDbContext _db;

    public GarantiaService(IApplicationDbContext db) => _db = db;

    // ----- Garantías -----

    public async Task<GarantiaDto> CrearAsync(GarantiaCrearDto dto)
    {
        if (dto.ImeiId is null && dto.VentaId is null)
            throw new GarantiaException("Indica al menos el IMEI o la venta que origina la garantía.");

        if (dto.ImeiId.HasValue)
        {
            if (!await _db.InventarioImeis.AnyAsync(i => i.ImeiId == dto.ImeiId.Value))
                throw new GarantiaException("El equipo (IMEI) indicado no existe.");

            var hoy = DateTime.Today;
            if (await _db.Garantias.AnyAsync(g =>
                    g.ImeiId == dto.ImeiId.Value && g.Estado == "Vigente" && g.FechaVencimiento >= hoy))
                throw new GarantiaException("El equipo ya tiene una garantía vigente.");
        }

        if (dto.VentaId.HasValue && !await _db.Ventas.AnyAsync(v => v.VentaId == dto.VentaId.Value))
            throw new GarantiaException("La venta indicada no existe.");

        if (dto.ClienteId.HasValue && !await _db.Clientes.AnyAsync(c => c.ClienteId == dto.ClienteId.Value))
            throw new GarantiaException("El cliente indicado no existe.");

        var inicio = (dto.FechaInicio ?? DateTime.Today).Date;
        var garantia = new Garantia
        {
            VentaId = dto.VentaId,
            VentaDetalleId = dto.VentaDetalleId,
            ImeiId = dto.ImeiId,
            ClienteId = dto.ClienteId,
            FechaInicio = inicio,
            MesesCobertura = dto.MesesCobertura,
            FechaVencimiento = inicio.AddMonths(dto.MesesCobertura),
            Condiciones = Normalizar(dto.Condiciones),
            Estado = "Vigente",
            FechaCreacion = DateTime.Now
        };
        _db.Garantias.Add(garantia);
        await _db.SaveChangesAsync();

        return (await PorIdAsync(garantia.GarantiaId))!;
    }

    public async Task<GarantiaDto?> PorIdAsync(int id)
        => await _db.Garantias.AsNoTracking()
            .Where(g => g.GarantiaId == id)
            .Select(ProyeccionGarantia)
            .FirstOrDefaultAsync();

    public async Task<GarantiaVigenteDto?> PorImeiAsync(string imei)
        => await _db.GarantiasVigentes.AsNoTracking()
            .Where(g => g.Imei == imei)
            .OrderByDescending(g => g.Vigente).ThenByDescending(g => g.FechaVencimiento)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<GarantiaDto>> BuscarAsync(int? clienteId, bool? vigentes)
    {
        var q = _db.Garantias.AsNoTracking();

        if (clienteId.HasValue)
            q = q.Where(g => g.ClienteId == clienteId.Value);

        if (vigentes == true)
        {
            var hoy = DateTime.Today;
            q = q.Where(g => g.Estado == "Vigente" && g.FechaVencimiento >= hoy);
        }

        return await q.OrderByDescending(g => g.FechaCreacion).Select(ProyeccionGarantia).ToListAsync();
    }

    public async Task AnularAsync(int id)
    {
        var garantia = await _db.Garantias.FirstOrDefaultAsync(g => g.GarantiaId == id)
            ?? throw new GarantiaException("La garantía no existe.");

        if (garantia.Estado != "Vigente")
            throw new GarantiaException($"La garantía está '{garantia.Estado}' y no se puede anular.");

        garantia.Estado = "Anulada";
        await _db.SaveChangesAsync();
    }

    // ----- Casos (RMA) -----

    public async Task<CasoDto> AbrirCasoAsync(CasoCrearDto dto)
    {
        Garantia? garantia = null;
        if (dto.GarantiaId.HasValue)
        {
            garantia = await _db.Garantias.AsNoTracking()
                .FirstOrDefaultAsync(g => g.GarantiaId == dto.GarantiaId.Value)
                ?? throw new GarantiaException("La garantía indicada no existe.");

            if (garantia.Estado != "Vigente" || garantia.FechaVencimiento < DateTime.Today)
                throw new GarantiaException("La garantía no está vigente; el caso no puede abrirse bajo cobertura.");
        }

        var imeiId = dto.ImeiId ?? garantia?.ImeiId;
        var clienteId = dto.ClienteId ?? garantia?.ClienteId;

        if (imeiId is null)
            throw new GarantiaException("Indica el equipo (IMEI) del caso, directamente o vía la garantía.");

        if (!await _db.InventarioImeis.AnyAsync(i => i.ImeiId == imeiId.Value))
            throw new GarantiaException("El equipo (IMEI) indicado no existe.");

        if (dto.OrdenTallerId.HasValue &&
            !await _db.OrdenesTaller.AnyAsync(o => o.OrdenTallerId == dto.OrdenTallerId.Value))
            throw new GarantiaException("La orden de taller indicada no existe.");

        var caso = new CasoGarantia
        {
            NumeroCaso = Normalizar(dto.NumeroCaso),
            GarantiaId = dto.GarantiaId,
            ImeiId = imeiId,
            ClienteId = clienteId,
            OrdenTallerId = dto.OrdenTallerId,
            FechaApertura = DateTime.Now,
            TipoFalla = Normalizar(dto.TipoFalla),
            DescripcionFalla = Normalizar(dto.DescripcionFalla),
            Estado = "Abierto"
        };
        _db.CasosGarantia.Add(caso);
        await _db.SaveChangesAsync();

        return (await CasoPorIdAsync(caso.CasoGarantiaId))!;
    }

    public async Task<CasoDto?> CasoPorIdAsync(int id)
        => await _db.CasosGarantia.AsNoTracking()
            .Where(c => c.CasoGarantiaId == id)
            .Select(ProyeccionCaso)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<CasoDto>> CasosAsync(string? estado, int? clienteId)
    {
        var q = _db.CasosGarantia.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(c => c.Estado == estado.Trim());

        if (clienteId.HasValue)
            q = q.Where(c => c.ClienteId == clienteId.Value);

        return await q.OrderByDescending(c => c.FechaApertura).Select(ProyeccionCaso).ToListAsync();
    }

    public async Task<CasoDto> IniciarCasoAsync(int id, int? ordenTallerId)
    {
        var caso = await ObtenerCasoAsync(id);

        if (caso.Estado != "Abierto")
            throw new GarantiaException($"Solo un caso 'Abierto' puede pasar a proceso (está '{caso.Estado}').");

        if (ordenTallerId.HasValue)
        {
            if (!await _db.OrdenesTaller.AnyAsync(o => o.OrdenTallerId == ordenTallerId.Value))
                throw new GarantiaException("La orden de taller indicada no existe.");
            caso.OrdenTallerId = ordenTallerId;
        }

        caso.Estado = "EnProceso";
        await _db.SaveChangesAsync();

        return (await CasoPorIdAsync(id))!;
    }

    public async Task<CasoDto> ResolverCasoAsync(int id, CasoResolverDto dto, int? usuarioId)
    {
        var caso = await ObtenerCasoAsync(id);

        if (caso.Estado is not ("Abierto" or "EnProceso"))
            throw new GarantiaException($"El caso está '{caso.Estado}' y ya no admite resolución.");

        var resolucion = dto.TipoResolucion.Trim();
        if (!Resoluciones.Contains(resolucion))
            throw new GarantiaException($"Resolución inválida. Opciones: {string.Join(", ", Resoluciones)}.");

        if (resolucion == "Reemplazo")
        {
            if (dto.ImeiReemplazoId is null)
                throw new GarantiaException("El reemplazo requiere el IMEI del equipo que se entrega.");

            var reemplazo = await _db.InventarioImeis
                .FirstOrDefaultAsync(i => i.ImeiId == dto.ImeiReemplazoId.Value)
                ?? throw new GarantiaException("El IMEI de reemplazo no existe.");

            if (reemplazo.Estado != "Disponible")
                throw new GarantiaException($"El IMEI de reemplazo no está disponible (está '{reemplazo.Estado}').");

            // Trazabilidad en inventario: sale el reemplazo y reingresa el equipo devuelto
            reemplazo.Estado = "Vendido";
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                ImeiId = reemplazo.ImeiId,
                Tipo = "Venta",
                SucursalOrigen = reemplazo.SucursalId,
                Referencia = $"Reemplazo por garantía, caso #{caso.CasoGarantiaId}",
                UsuarioId = usuarioId,
                Fecha = DateTime.Now
            });

            if (caso.ImeiId.HasValue)
            {
                var devuelto = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == caso.ImeiId.Value);
                if (devuelto is not null)
                {
                    devuelto.Estado = "Devuelto";
                    _db.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ImeiId = devuelto.ImeiId,
                        Tipo = "Devolucion",
                        SucursalDestino = devuelto.SucursalId,
                        Referencia = $"Devolución por garantía, caso #{caso.CasoGarantiaId}",
                        UsuarioId = usuarioId,
                        Fecha = DateTime.Now
                    });
                }
            }

            caso.ImeiReemplazoId = dto.ImeiReemplazoId;
        }

        caso.TipoResolucion = resolucion;
        caso.Notas = Normalizar(dto.Notas) ?? caso.Notas;
        caso.Estado = resolucion == "Rechazado" ? "Rechazado" : "Resuelto";
        await _db.SaveChangesAsync();

        return (await CasoPorIdAsync(id))!;
    }

    public async Task<CasoDto> CerrarCasoAsync(int id)
    {
        var caso = await ObtenerCasoAsync(id);

        if (caso.Estado is not ("Resuelto" or "Rechazado"))
            throw new GarantiaException("Solo un caso 'Resuelto' o 'Rechazado' puede cerrarse.");

        caso.Estado = "Cerrado";
        caso.FechaCierre = DateTime.Now;
        await _db.SaveChangesAsync();

        return (await CasoPorIdAsync(id))!;
    }

    public async Task<IReadOnlyList<IndiceFallaDto>> IndiceFallasAsync()
        => await _db.IndiceFallas.AsNoTracking()
            .OrderByDescending(f => f.TotalCasos)
            .ToListAsync();

    private async Task<CasoGarantia> ObtenerCasoAsync(int id)
        => await _db.CasosGarantia.FirstOrDefaultAsync(c => c.CasoGarantiaId == id)
            ?? throw new GarantiaException("El caso de garantía no existe.");

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static readonly Expression<Func<Garantia, GarantiaDto>> ProyeccionGarantia = g => new GarantiaDto
    {
        GarantiaId = g.GarantiaId,
        VentaId = g.VentaId,
        ImeiId = g.ImeiId,
        Imei = g.Imei == null ? null : g.Imei.Imei,
        ClienteId = g.ClienteId,
        Cliente = g.Cliente == null ? null : g.Cliente.Nombre,
        FechaInicio = g.FechaInicio,
        MesesCobertura = g.MesesCobertura,
        FechaVencimiento = g.FechaVencimiento,
        Condiciones = g.Condiciones,
        Estado = g.Estado,
        Vigente = g.Estado == "Vigente" && g.FechaVencimiento >= DateTime.Today
    };

    private static readonly Expression<Func<CasoGarantia, CasoDto>> ProyeccionCaso = c => new CasoDto
    {
        CasoGarantiaId = c.CasoGarantiaId,
        NumeroCaso = c.NumeroCaso,
        GarantiaId = c.GarantiaId,
        ImeiId = c.ImeiId,
        Imei = c.Imei == null ? null : c.Imei.Imei,
        ClienteId = c.ClienteId,
        Cliente = c.Cliente == null ? null : c.Cliente.Nombre,
        OrdenTallerId = c.OrdenTallerId,
        FechaApertura = c.FechaApertura,
        TipoFalla = c.TipoFalla,
        DescripcionFalla = c.DescripcionFalla,
        TipoResolucion = c.TipoResolucion,
        ImeiReemplazoId = c.ImeiReemplazoId,
        ImeiReemplazo = c.ImeiReemplazo == null ? null : c.ImeiReemplazo.Imei,
        Estado = c.Estado,
        FechaCierre = c.FechaCierre,
        Notas = c.Notas
    };
}
