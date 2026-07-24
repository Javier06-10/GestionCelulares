using System.ComponentModel.DataAnnotations;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Devoluciones;

/// <summary>Nota de crédito emitida por una devolución.</summary>
public class NotaCreditoDto
{
    public int NotaCreditoId { get; set; }
    public string? Ncf { get; set; }            // NCF 04 emitido
    public string? NcfModificado { get; set; }  // NCF original de la venta
    public int VentaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Cliente { get; set; }
    public decimal Monto { get; set; }          // base imponible devuelta
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class DevolucionCrearDto
{
    [Required] public int VentaId { get; set; }
    [StringLength(200)] public string? Motivo { get; set; }
}

public interface IDevolucionService
{
    Task<IReadOnlyList<NotaCreditoDto>> ListarAsync();
    Task<NotaCreditoDto> CrearAsync(DevolucionCrearDto dto, int? usuarioId);
}

public class DevolucionService : IDevolucionService
{
    private readonly IApplicationDbContext _db;
    private readonly IVentaService _ventas;

    public DevolucionService(IApplicationDbContext db, IVentaService ventas)
    {
        _db = db;
        _ventas = ventas;
    }

    public async Task<IReadOnlyList<NotaCreditoDto>> ListarAsync()
        => await _db.NotasCredito.AsNoTracking()
            .OrderByDescending(n => n.NotaCreditoId)
            .Select(n => new NotaCreditoDto
            {
                NotaCreditoId = n.NotaCreditoId,
                Ncf = n.Ncf,
                NcfModificado = n.NcfModificado,
                VentaId = n.VentaId,
                NumeroFactura = n.Venta != null ? n.Venta.NumeroFactura : null,
                Cliente = n.Venta != null && n.Venta.Cliente != null ? n.Venta.Cliente.Nombre : null,
                Monto = n.Monto,
                Itbis = n.Itbis,
                Total = n.Total,
                Motivo = n.Motivo,
                FechaRegistro = n.FechaRegistro
            })
            .ToListAsync();

    public async Task<NotaCreditoDto> CrearAsync(DevolucionCrearDto dto, int? usuarioId)
    {
        // DevolverAsync valida (venta completada + con NCF), revierte el inventario,
        // deja la venta como "Devuelta" (sigue en el 607) y emite la Nota de Crédito (04).
        await _ventas.DevolverAsync(dto.VentaId, dto.Motivo, usuarioId);

        var nc = await _db.NotasCredito.AsNoTracking()
            .Include(n => n.Venta)!.ThenInclude(v => v!.Cliente)
            .OrderByDescending(n => n.NotaCreditoId)
            .FirstAsync(n => n.VentaId == dto.VentaId);

        return new NotaCreditoDto
        {
            NotaCreditoId = nc.NotaCreditoId,
            Ncf = nc.Ncf,
            NcfModificado = nc.NcfModificado,
            VentaId = nc.VentaId,
            NumeroFactura = nc.Venta?.NumeroFactura,
            Cliente = nc.Venta?.Cliente?.Nombre,
            Monto = nc.Monto,
            Itbis = nc.Itbis,
            Total = nc.Total,
            Motivo = nc.Motivo,
            FechaRegistro = nc.FechaRegistro
        };
    }
}
