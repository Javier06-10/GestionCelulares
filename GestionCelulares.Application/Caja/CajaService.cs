using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Caja;

public interface ICajaService
{
    Task<SesionCajaDto> AbrirAsync(AperturaCajaDto dto, int usuarioId);
    Task<SesionCajaDto?> SesionActualAsync(int sucursalId);
    Task<SesionCajaDto?> PorIdAsync(int id);
    Task<IReadOnlyList<MovimientoCajaDto>> MovimientosAsync(int sesionCajaId);
    Task<MovimientoCajaDto> RegistrarMovimientoAsync(int sesionCajaId, MovimientoCajaRegistroDto dto);
    Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, CierreCajaDto dto, int usuarioId);
}

public class CajaService : ICajaService
{
    private static readonly string[] TiposValidos = { "Ingreso", "Egreso" };

    private readonly IApplicationDbContext _db;
    private readonly ICajaProcedures _procedures;

    public CajaService(IApplicationDbContext db, ICajaProcedures procedures)
    {
        _db = db;
        _procedures = procedures;
    }

    public async Task<SesionCajaDto> AbrirAsync(AperturaCajaDto dto, int usuarioId)
    {
        if (!await _db.Sucursales.AnyAsync(s => s.SucursalId == dto.SucursalId))
            throw new CajaException("La sucursal indicada no existe.");

        if (await _db.SesionesCaja.AnyAsync(s => s.SucursalId == dto.SucursalId && s.Estado == "Abierta"))
            throw new CajaException("Ya hay una sesión de caja abierta en esta sucursal. Ciérrala antes de abrir otra.");

        var sesion = new SesionCaja
        {
            SucursalId = dto.SucursalId,
            UsuarioApertura = usuarioId,
            MontoApertura = dto.MontoApertura,
            Estado = "Abierta",
            FechaApertura = DateTime.UtcNow
        };
        _db.SesionesCaja.Add(sesion);
        await _db.SaveChangesAsync();

        return (await PorIdAsync(sesion.SesionCajaId))!;
    }

    public async Task<SesionCajaDto?> SesionActualAsync(int sucursalId)
    {
        var id = await _db.SesionesCaja
            .Where(s => s.SucursalId == sucursalId && s.Estado == "Abierta")
            .Select(s => (int?)s.SesionCajaId)
            .FirstOrDefaultAsync();

        return id is null ? null : await PorIdAsync(id.Value);
    }

    public async Task<SesionCajaDto?> PorIdAsync(int id)
        => await _db.SesionesCaja.AsNoTracking()
            .Where(s => s.SesionCajaId == id)
            .Select(s => new SesionCajaDto
            {
                SesionCajaId = s.SesionCajaId,
                SucursalId = s.SucursalId,
                UsuarioApertura = s.UsuarioApertura,
                UsuarioCierre = s.UsuarioCierre,
                MontoApertura = s.MontoApertura,
                MontoCierre = s.MontoCierre,
                Diferencia = s.Diferencia,
                Estado = s.Estado,
                FechaApertura = s.FechaApertura,
                FechaCierre = s.FechaCierre,
                TotalIngresos = s.Movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => (decimal?)m.Monto) ?? 0,
                TotalEgresos = s.Movimientos.Where(m => m.Tipo == "Egreso").Sum(m => (decimal?)m.Monto) ?? 0
            })
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<MovimientoCajaDto>> MovimientosAsync(int sesionCajaId)
    {
        await ExigirSesionAsync(sesionCajaId);

        return await _db.MovimientosCaja.AsNoTracking()
            .Where(m => m.SesionCajaId == sesionCajaId)
            .OrderBy(m => m.Fecha)
            .Select(m => new MovimientoCajaDto
            {
                MovimientoCajaId = m.MovimientoCajaId,
                SesionCajaId = m.SesionCajaId,
                Tipo = m.Tipo,
                Concepto = m.Concepto,
                Monto = m.Monto,
                Referencia = m.Referencia,
                Fecha = m.Fecha
            })
            .ToListAsync();
    }

    public async Task<MovimientoCajaDto> RegistrarMovimientoAsync(int sesionCajaId, MovimientoCajaRegistroDto dto)
    {
        if (!TiposValidos.Contains(dto.Tipo))
            throw new CajaException("El tipo de movimiento debe ser 'Ingreso' o 'Egreso'.");

        var sesion = await _db.SesionesCaja.FirstOrDefaultAsync(s => s.SesionCajaId == sesionCajaId)
            ?? throw new CajaException("La sesión de caja no existe.");

        if (sesion.Estado != "Abierta")
            throw new CajaException("La sesión de caja ya está cerrada; no admite movimientos.");

        var movimiento = new MovimientoCaja
        {
            SesionCajaId = sesionCajaId,
            Tipo = dto.Tipo,
            Concepto = dto.Concepto.Trim(),
            Monto = dto.Monto,
            Referencia = string.IsNullOrWhiteSpace(dto.Referencia) ? null : dto.Referencia.Trim(),
            Fecha = DateTime.UtcNow
        };
        _db.MovimientosCaja.Add(movimiento);
        await _db.SaveChangesAsync();

        return new MovimientoCajaDto
        {
            MovimientoCajaId = movimiento.MovimientoCajaId,
            SesionCajaId = movimiento.SesionCajaId,
            Tipo = movimiento.Tipo,
            Concepto = movimiento.Concepto,
            Monto = movimiento.Monto,
            Referencia = movimiento.Referencia,
            Fecha = movimiento.Fecha
        };
    }

    public async Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, CierreCajaDto dto, int usuarioId)
    {
        var sesion = await _db.SesionesCaja.AsNoTracking().FirstOrDefaultAsync(s => s.SesionCajaId == sesionCajaId)
            ?? throw new CajaException("La sesión de caja no existe.");

        if (sesion.Estado != "Abierta")
            throw new CajaException("La sesión de caja ya está cerrada.");

        // El arqueo (esperado vs. contado) lo calcula usp_Caja_Cerrar en una transacción
        return await _procedures.CerrarAsync(sesionCajaId, usuarioId, dto.MontoContado);
    }

    private async Task ExigirSesionAsync(int sesionCajaId)
    {
        if (!await _db.SesionesCaja.AnyAsync(s => s.SesionCajaId == sesionCajaId))
            throw new CajaException("La sesión de caja no existe.");
    }
}
