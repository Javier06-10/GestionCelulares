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
    Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, CierreCajaDto dto, int usuarioId, bool esAdmin = false);
    Task<ResumenTurnoDto> ResumenTurnoAsync(int sesionCajaId);
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

        // Regla: un empleado solo puede tener una caja abierta a la vez
        if (await _db.SesionesCaja.AnyAsync(s => s.UsuarioApertura == usuarioId && s.Estado == "Abierta"))
            throw new CajaException("Ya tienes una caja abierta. Ciérrala antes de abrir otra.");

        var sesion = new SesionCaja
        {
            SucursalId = dto.SucursalId,
            UsuarioApertura = usuarioId,
            MontoApertura = dto.MontoApertura,
            Estado = "Abierta",
            FechaApertura = DateTime.Now
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
                UsuarioAperturaNombre = _db.Usuarios
                    .Where(u => u.UsuarioId == s.UsuarioApertura)
                    .Select(u => u.NombreCompleto)
                    .FirstOrDefault(),
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
            Fecha = DateTime.Now
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

    public async Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, CierreCajaDto dto, int usuarioId, bool esAdmin = false)
    {
        var sesion = await _db.SesionesCaja.AsNoTracking().FirstOrDefaultAsync(s => s.SesionCajaId == sesionCajaId)
            ?? throw new CajaException("La sesión de caja no existe.");

        if (sesion.Estado != "Abierta")
            throw new CajaException("La sesión de caja ya está cerrada.");

        // Regla: solo el empleado que abrió la caja puede cerrarla (el admin puede forzar)
        if (sesion.UsuarioApertura != usuarioId && !esAdmin)
            throw new CajaException("Solo el empleado que abrió la caja puede cerrarla.");

        // El arqueo (esperado vs. contado) lo calcula usp_Caja_Cerrar en una transacción
        return await _procedures.CerrarAsync(sesionCajaId, usuarioId, dto.MontoContado);
    }

    public async Task<ResumenTurnoDto> ResumenTurnoAsync(int sesionCajaId)
    {
        var sesion = await PorIdAsync(sesionCajaId)
            ?? throw new CajaException("La sesión de caja no existe.");

        // Ventas del turno (cantidad y total facturado)
        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.SesionCajaId == sesionCajaId && v.Estado != "Anulada")
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .FirstOrDefaultAsync();

        // Cobros de ventas desglosados por método de pago
        var porMetodo = await _db.VentaPagos.AsNoTracking()
            .Where(p => p.Venta.SesionCajaId == sesionCajaId && p.Venta.Estado != "Anulada")
            .GroupBy(p => p.MetodoPago.Nombre)
            .Select(g => new ResumenMetodoDto { Metodo = g.Key, Cantidad = g.Count(), Monto = g.Sum(x => x.Monto) })
            .OrderByDescending(m => m.Monto)
            .ToListAsync();

        // Abonos de crédito cobrados en el turno
        var abonos = await _db.PagosCredito.AsNoTracking()
            .Where(p => p.SesionCajaId == sesionCajaId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Count(),
                Total = g.Sum(p => p.Monto),
                Efectivo = g.Sum(p => p.MetodoPago != null && p.MetodoPago.Nombre == "Efectivo" ? p.Monto : 0m)
            })
            .FirstOrDefaultAsync();

        // Recepciones del taller atadas a este turno (anticipos cobrados al recibir)
        var taller = await _db.OrdenesTaller.AsNoTracking()
            .Where(o => o.SesionCajaId == sesionCajaId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Count(),
                Anticipos = g.Sum(o => o.Anticipo),
                AnticiposEfectivo = g.Sum(o => o.MetodoPagoAnticipo != null && o.MetodoPagoAnticipo.Nombre == "Efectivo" ? o.Anticipo : 0m)
            })
            .FirstOrDefaultAsync();

        // Entregas del taller cobradas en este turno (balance = costo final - anticipo)
        var entregas = await _db.OrdenesTaller.AsNoTracking()
            .Where(o => o.SesionCajaEntrega == sesionCajaId && o.Estado == "Entregado")
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Count(),
                Balance = g.Sum(o => (o.CostoFinal ?? 0) - o.Anticipo),
                BalanceEfectivo = g.Sum(o => o.MetodoPagoEntrega != null && o.MetodoPagoEntrega.Nombre == "Efectivo" ? (o.CostoFinal ?? 0) - o.Anticipo : 0m)
            })
            .FirstOrDefaultAsync();

        // Abonos de apartados recibidos en este turno
        var apartados = await _db.AbonosApartado.AsNoTracking()
            .Where(a => a.SesionCajaId == sesionCajaId && a.Tipo == "Abono")
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cantidad = g.Count(),
                Total = g.Sum(a => a.Monto),
                Efectivo = g.Sum(a => a.MetodoPago != null && a.MetodoPago.Nombre == "Efectivo" ? a.Monto : 0m)
            })
            .FirstOrDefaultAsync();

        var ventasEfectivo = porMetodo.Where(m => m.Metodo == "Efectivo").Sum(m => m.Monto);
        var ventasCobrado = porMetodo.Sum(m => m.Monto);
        var abonosEfectivo = abonos?.Efectivo ?? 0;
        var tallerAnticipos = taller?.Anticipos ?? 0;
        var tallerEntregasCobrado = entregas?.Balance ?? 0;
        var apartadosEfectivo = apartados?.Efectivo ?? 0;

        // Dos "sobres" de la misma caja chica (solo el efectivo entra al arqueo)
        var efectivoVentas = ventasEfectivo + abonosEfectivo;
        var efectivoReparaciones = (taller?.AnticiposEfectivo ?? 0) + (entregas?.BalanceEfectivo ?? 0);

        return new ResumenTurnoDto
        {
            SesionCajaId = sesionCajaId,
            Empleado = sesion.UsuarioAperturaNombre,
            FechaApertura = sesion.FechaApertura,
            MontoApertura = sesion.MontoApertura,
            VentasCantidad = ventas?.Cantidad ?? 0,
            VentasTotal = ventas?.Total ?? 0,
            VentasEfectivo = ventasEfectivo,
            VentasOtros = ventasCobrado - ventasEfectivo,
            AbonosCantidad = abonos?.Cantidad ?? 0,
            AbonosTotal = abonos?.Total ?? 0,
            AbonosEfectivo = abonosEfectivo,
            TallerRecepciones = taller?.Cantidad ?? 0,
            TallerAnticipos = tallerAnticipos,
            TallerEntregas = entregas?.Cantidad ?? 0,
            TallerEntregasCobrado = tallerEntregasCobrado,
            ApartadosAbonos = apartados?.Cantidad ?? 0,
            ApartadosTotal = apartados?.Total ?? 0,
            EfectivoApartados = apartadosEfectivo,
            TotalIngresos = sesion.TotalIngresos,
            TotalEgresos = sesion.TotalEgresos,
            EfectivoVentas = efectivoVentas,
            EfectivoReparaciones = efectivoReparaciones,
            EfectivoEsperado = sesion.MontoApertura + efectivoVentas + efectivoReparaciones + apartadosEfectivo + sesion.TotalIngresos - sesion.TotalEgresos,
            VentasPorMetodo = porMetodo
        };
    }

    private async Task ExigirSesionAsync(int sesionCajaId)
    {
        if (!await _db.SesionesCaja.AnyAsync(s => s.SesionCajaId == sesionCajaId))
            throw new CajaException("La sesión de caja no existe.");
    }
}
