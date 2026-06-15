using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Taller;

public interface ITallerService
{
    Task<OrdenDto> CrearAsync(OrdenCrearDto dto, int? usuarioRecepcion = null);
    Task<OrdenDto?> PorIdAsync(int id);
    Task<IReadOnlyList<OrdenResumenDto>> BuscarAsync(string? estado, int? sucursalId, int? tecnicoId, int? clienteId);
    Task<OrdenDto> ActualizarAsync(int id, OrdenActualizarDto dto);
    Task<OrdenDto> CambiarEstadoAsync(int id, CambioEstadoDto dto);
    Task<OrdenDto> AgregarRepuestoAsync(int id, RepuestoAgregarDto dto);
    Task<OrdenDto> AgregarFotoAsync(int id, FotoAgregarDto dto);
    Task<IReadOnlyList<ComisionTecnicoDto>> ComisionesPorTecnicoAsync(DateTime? desde, DateTime? hasta);
}

public class TallerService : ITallerService
{
    // Transiciones válidas del flujo del taller (alimenta el Kanban)
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        ["Recibido"] = new[] { "EnReparacion", "Cancelado" },
        ["EnReparacion"] = new[] { "Reparado", "Cancelado" },
        ["Reparado"] = new[] { "Entregado", "EnReparacion" }, // vuelve si falla el control de calidad
        ["Entregado"] = Array.Empty<string>(),
        ["Cancelado"] = Array.Empty<string>()
    };

    private readonly IApplicationDbContext _db;

    public TallerService(IApplicationDbContext db) => _db = db;

    public async Task<OrdenDto> CrearAsync(OrdenCrearDto dto, int? usuarioRecepcion = null)
    {
        if (!await _db.Sucursales.AnyAsync(s => s.SucursalId == dto.SucursalId))
            throw new TallerException("La sucursal indicada no existe.");

        if (dto.ImeiId is null && string.IsNullOrWhiteSpace(dto.EquipoDescripcion))
            throw new TallerException("Indica el IMEI del inventario o la descripción del equipo del cliente.");

        if (dto.ClienteId.HasValue && !await _db.Clientes.AnyAsync(c => c.ClienteId == dto.ClienteId.Value))
            throw new TallerException("El cliente indicado no existe.");

        if (dto.ImeiId.HasValue && !await _db.InventarioImeis.AnyAsync(i => i.ImeiId == dto.ImeiId.Value))
            throw new TallerException("El equipo (IMEI) indicado no existe en el inventario.");

        await ValidarTecnicoAsync(dto.TecnicoId);

        // La recepción queda atada a la caja abierta de la sucursal (si la hay) y al
        // usuario que recibe el equipo, igual que las ventas.
        var sesionCajaId = await _db.SesionesCaja
            .Where(s => s.SucursalId == dto.SucursalId && s.Estado == "Abierta")
            .Select(s => (int?)s.SesionCajaId)
            .FirstOrDefaultAsync();

        var orden = new OrdenTaller
        {
            NumeroOrden = Normalizar(dto.NumeroOrden),
            SucursalId = dto.SucursalId,
            ClienteId = dto.ClienteId,
            ImeiId = dto.ImeiId,
            EquipoDescripcion = Normalizar(dto.EquipoDescripcion),
            Diagnostico = Normalizar(dto.Diagnostico),
            TecnicoId = dto.TecnicoId,
            Estado = "Recibido",
            Anticipo = dto.Anticipo,
            CostoEstimado = dto.CostoEstimado,
            ComisionTecnico = 0,
            SesionCajaId = sesionCajaId,
            UsuarioRecepcion = usuarioRecepcion,
            FechaRecepcion = DateTime.Now
        };
        _db.OrdenesTaller.Add(orden);
        await _db.SaveChangesAsync();

        return (await PorIdAsync(orden.OrdenTallerId))!;
    }

    public async Task<OrdenDto?> PorIdAsync(int id)
        => await _db.OrdenesTaller.AsNoTracking()
            .Where(o => o.OrdenTallerId == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<OrdenResumenDto>> BuscarAsync(string? estado, int? sucursalId, int? tecnicoId, int? clienteId)
    {
        var q = _db.OrdenesTaller.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(o => o.Estado == estado.Trim());

        if (sucursalId.HasValue) q = q.Where(o => o.SucursalId == sucursalId.Value);
        if (tecnicoId.HasValue) q = q.Where(o => o.TecnicoId == tecnicoId.Value);
        if (clienteId.HasValue) q = q.Where(o => o.ClienteId == clienteId.Value);

        return await q.OrderBy(o => o.FechaRecepcion)
            .Select(o => new OrdenResumenDto
            {
                OrdenTallerId = o.OrdenTallerId,
                NumeroOrden = o.NumeroOrden,
                Cliente = o.Cliente == null ? null : o.Cliente.Nombre,
                Equipo = o.Imei != null ? o.Imei.Variante.Producto.Nombre : o.EquipoDescripcion,
                Tecnico = o.Tecnico == null ? null : o.Tecnico.NombreCompleto,
                Estado = o.Estado,
                CostoEstimado = o.CostoEstimado,
                FechaRecepcion = o.FechaRecepcion
            })
            .ToListAsync();
    }

    public async Task<OrdenDto> ActualizarAsync(int id, OrdenActualizarDto dto)
    {
        var orden = await ObtenerEditableAsync(id);

        await ValidarTecnicoAsync(dto.TecnicoId);

        orden.EquipoDescripcion = Normalizar(dto.EquipoDescripcion) ?? orden.EquipoDescripcion;
        orden.Diagnostico = Normalizar(dto.Diagnostico);
        orden.TecnicoId = dto.TecnicoId;
        orden.NumeroOrden = Normalizar(dto.NumeroOrden);
        orden.Anticipo = dto.Anticipo;
        orden.CostoEstimado = dto.CostoEstimado;
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task<OrdenDto> CambiarEstadoAsync(int id, CambioEstadoDto dto)
    {
        var orden = await _db.OrdenesTaller.FirstOrDefaultAsync(o => o.OrdenTallerId == id)
            ?? throw new TallerException("La orden de taller no existe.");

        var destino = dto.Estado.Trim();
        if (!Transiciones.ContainsKey(destino))
            throw new TallerException($"Estado '{destino}' inválido. Estados: {string.Join(", ", Transiciones.Keys)}.");

        if (!Transiciones[orden.Estado].Contains(destino))
            throw new TallerException($"No se puede pasar de '{orden.Estado}' a '{destino}'.");

        if (destino == "EnReparacion" && orden.TecnicoId is null)
            throw new TallerException("Asigna un técnico antes de iniciar la reparación.");

        if (destino == "Entregado")
        {
            orden.CostoFinal = dto.CostoFinal ?? orden.CostoEstimado;
            orden.ComisionTecnico = dto.ComisionTecnico ?? orden.ComisionTecnico;
            orden.FechaEntrega = DateTime.Now;
        }

        orden.Estado = destino;
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task<OrdenDto> AgregarRepuestoAsync(int id, RepuestoAgregarDto dto)
    {
        var orden = await ObtenerEditableAsync(id);

        if (dto.VarianteId.HasValue)
        {
            var variante = await _db.ProductoVariantes
                .FirstOrDefaultAsync(v => v.VarianteId == dto.VarianteId.Value)
                ?? throw new TallerException("La variante de repuesto indicada no existe.");

            if (variante.StockNoSerial < dto.Cantidad)
                throw new TallerException(
                    $"Stock insuficiente del repuesto: hay {variante.StockNoSerial} y se piden {dto.Cantidad}.");

            // Consumo del inventario no serializado
            variante.StockNoSerial -= dto.Cantidad;
        }

        _db.OrdenTallerRepuestos.Add(new OrdenTallerRepuesto
        {
            OrdenTallerId = id,
            VarianteId = dto.VarianteId,
            Descripcion = dto.Descripcion.Trim(),
            Cantidad = dto.Cantidad,
            Costo = dto.Costo
        });
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task<OrdenDto> AgregarFotoAsync(int id, FotoAgregarDto dto)
    {
        await ObtenerEditableAsync(id);

        _db.OrdenTallerFotos.Add(new OrdenTallerFoto
        {
            OrdenTallerId = id,
            Url = dto.Url.Trim(),
            Fecha = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task<IReadOnlyList<ComisionTecnicoDto>> ComisionesPorTecnicoAsync(DateTime? desde, DateTime? hasta)
    {
        // Solo cuentan las órdenes entregadas (es cuando se fija la comisión)
        var q = _db.OrdenesTaller.AsNoTracking()
            .Where(o => o.Estado == "Entregado" && o.TecnicoId != null);

        if (desde.HasValue) q = q.Where(o => o.FechaEntrega >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(o => o.FechaEntrega < hasta.Value.Date.AddDays(1));

        return await q
            .GroupBy(o => new { o.TecnicoId, o.Tecnico!.NombreCompleto })
            .Select(g => new ComisionTecnicoDto
            {
                TecnicoId = g.Key.TecnicoId!.Value,
                Tecnico = g.Key.NombreCompleto,
                OrdenesEntregadas = g.Count(),
                TotalComision = g.Sum(o => o.ComisionTecnico)
            })
            .OrderByDescending(c => c.TotalComision)
            .ToListAsync();
    }

    private async Task<OrdenTaller> ObtenerEditableAsync(int id)
    {
        var orden = await _db.OrdenesTaller.FirstOrDefaultAsync(o => o.OrdenTallerId == id)
            ?? throw new TallerException("La orden de taller no existe.");

        if (orden.Estado is "Entregado" or "Cancelado")
            throw new TallerException($"La orden está '{orden.Estado}' y ya no se puede modificar.");

        return orden;
    }

    private async Task ValidarTecnicoAsync(int? tecnicoId)
    {
        if (tecnicoId.HasValue &&
            !await _db.Usuarios.AnyAsync(u => u.UsuarioId == tecnicoId.Value && u.Activo))
            throw new TallerException("El técnico indicado no existe o está inactivo.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static readonly Expression<Func<OrdenTaller, OrdenDto>> Proyeccion = o => new OrdenDto
    {
        OrdenTallerId = o.OrdenTallerId,
        NumeroOrden = o.NumeroOrden,
        SucursalId = o.SucursalId,
        ClienteId = o.ClienteId,
        Cliente = o.Cliente == null ? null : o.Cliente.Nombre,
        ImeiId = o.ImeiId,
        Imei = o.Imei == null ? null : o.Imei.Imei,
        EquipoDescripcion = o.EquipoDescripcion,
        Diagnostico = o.Diagnostico,
        TecnicoId = o.TecnicoId,
        Tecnico = o.Tecnico == null ? null : o.Tecnico.NombreCompleto,
        Estado = o.Estado,
        Anticipo = o.Anticipo,
        CostoEstimado = o.CostoEstimado,
        CostoFinal = o.CostoFinal,
        ComisionTecnico = o.ComisionTecnico,
        TotalRepuestos = o.Repuestos.Sum(r => (decimal?)(r.Costo * r.Cantidad)) ?? 0,
        SesionCajaId = o.SesionCajaId,
        RecibidoPor = o.Recepcionista == null ? null : o.Recepcionista.NombreCompleto,
        FechaRecepcion = o.FechaRecepcion,
        FechaEntrega = o.FechaEntrega,
        Repuestos = o.Repuestos.OrderBy(r => r.Id).Select(r => new RepuestoDto
        {
            Id = r.Id,
            VarianteId = r.VarianteId,
            Descripcion = r.Descripcion,
            Cantidad = r.Cantidad,
            Costo = r.Costo
        }).ToList(),
        Fotos = o.Fotos.OrderBy(f => f.Id).Select(f => new FotoDto
        {
            Id = f.Id,
            Url = f.Url,
            Fecha = f.Fecha
        }).ToList()
    };
}
