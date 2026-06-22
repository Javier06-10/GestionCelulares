using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Contabilidad;

public interface IAsientoService
{
    Task<IReadOnlyList<AsientoResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta);
    Task<AsientoDto?> PorIdAsync(int id);
    Task<AsientoDto> CrearAsync(AsientoCrearDto dto, int? usuarioId);
    Task AnularAsync(int id);
    Task<BalanceComprobacionDto> BalanceComprobacionAsync(DateTime desde, DateTime hasta);
}

public class AsientoService : IAsientoService
{
    private readonly IApplicationDbContext _db;

    public AsientoService(IApplicationDbContext db) => _db = db;

    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public async Task<IReadOnlyList<AsientoResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta)
    {
        var q = _db.Asientos.AsNoTracking().AsQueryable();
        if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(a => a.Fecha <= hasta.Value.Date);

        return await q.OrderByDescending(a => a.Numero)
            .Select(a => new AsientoResumenDto
            {
                AsientoContableId = a.AsientoContableId,
                Numero = a.Numero,
                Fecha = a.Fecha,
                Concepto = a.Concepto,
                Origen = a.Origen,
                Estado = a.Estado,
                Total = a.Detalles.Sum(d => d.Debito)
            })
            .ToListAsync();
    }

    public async Task<AsientoDto?> PorIdAsync(int id)
        => await _db.Asientos.AsNoTracking()
            .Where(a => a.AsientoContableId == id)
            .Select(a => new AsientoDto
            {
                AsientoContableId = a.AsientoContableId,
                Numero = a.Numero,
                Fecha = a.Fecha,
                Concepto = a.Concepto,
                Origen = a.Origen,
                Estado = a.Estado,
                TotalDebito = a.Detalles.Sum(d => d.Debito),
                TotalCredito = a.Detalles.Sum(d => d.Credito),
                Detalles = a.Detalles.OrderBy(d => d.AsientoDetalleId).Select(d => new AsientoDetalleDto
                {
                    AsientoDetalleId = d.AsientoDetalleId,
                    CuentaContableId = d.CuentaContableId,
                    CuentaCodigo = d.Cuenta.Codigo,
                    CuentaNombre = d.Cuenta.Nombre,
                    Debito = d.Debito,
                    Credito = d.Credito,
                    Descripcion = d.Descripcion
                }).ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<AsientoDto> CrearAsync(AsientoCrearDto dto, int? usuarioId)
    {
        if (dto.Detalles.Count < 2)
            throw new ContabilidadException("El asiento debe tener al menos dos líneas.");

        decimal totalDebito = 0, totalCredito = 0;
        foreach (var d in dto.Detalles)
        {
            var deb = R(d.Debito);
            var cred = R(d.Credito);
            if (deb < 0 || cred < 0)
                throw new ContabilidadException("Los montos no pueden ser negativos.");
            if (deb > 0 && cred > 0)
                throw new ContabilidadException("Cada línea es débito o crédito, no ambos.");
            if (deb == 0 && cred == 0)
                throw new ContabilidadException("Cada línea debe tener un débito o un crédito.");

            var cuenta = await _db.CuentasContables.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CuentaContableId == d.CuentaContableId)
                ?? throw new ContabilidadException("Una de las cuentas indicadas no existe.");
            if (!cuenta.PermiteMovimiento)
                throw new ContabilidadException($"La cuenta {cuenta.Codigo} {cuenta.Nombre} es de agrupación y no admite movimientos.");
            if (!cuenta.Activo)
                throw new ContabilidadException($"La cuenta {cuenta.Codigo} {cuenta.Nombre} está inactiva.");

            totalDebito += deb;
            totalCredito += cred;
        }

        if (R(totalDebito) != R(totalCredito))
            throw new ContabilidadException($"El asiento no cuadra: débitos {R(totalDebito):N2} ≠ créditos {R(totalCredito):N2}.");
        if (totalDebito == 0)
            throw new ContabilidadException("El asiento no puede ser por cero.");

        var numero = (await _db.Asientos.MaxAsync(a => (int?)a.Numero) ?? 0) + 1;

        var asiento = new AsientoContable
        {
            Numero = numero,
            Fecha = dto.Fecha.Date,
            Concepto = dto.Concepto.Trim(),
            Origen = "Manual",
            Estado = "Registrado",
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.Now,
            Detalles = dto.Detalles.Select(d => new AsientoDetalle
            {
                CuentaContableId = d.CuentaContableId,
                Debito = R(d.Debito),
                Credito = R(d.Credito),
                Descripcion = string.IsNullOrWhiteSpace(d.Descripcion) ? null : d.Descripcion.Trim()
            }).ToList()
        };
        _db.Asientos.Add(asiento);
        await _db.SaveChangesAsync();

        return (await PorIdAsync(asiento.AsientoContableId))!;
    }

    public async Task AnularAsync(int id)
    {
        var asiento = await _db.Asientos.FirstOrDefaultAsync(a => a.AsientoContableId == id)
            ?? throw new ContabilidadException("El asiento no existe.");
        if (asiento.Estado == "Anulado")
            throw new ContabilidadException("El asiento ya está anulado.");

        asiento.Estado = "Anulado";
        await _db.SaveChangesAsync();
    }

    public async Task<BalanceComprobacionDto> BalanceComprobacionAsync(DateTime desde, DateTime hasta)
    {
        var d = desde.Date;
        var h = hasta.Date;

        var movs = await _db.AsientoDetalles.AsNoTracking()
            .Where(x => x.Asiento.Estado == "Registrado" && x.Asiento.Fecha >= d && x.Asiento.Fecha <= h)
            .GroupBy(x => new { x.CuentaContableId, x.Cuenta.Codigo, x.Cuenta.Nombre, x.Cuenta.Tipo })
            .Select(g => new
            {
                g.Key.CuentaContableId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.Tipo,
                Debito = g.Sum(x => x.Debito),
                Credito = g.Sum(x => x.Credito)
            })
            .ToListAsync();

        var lineas = movs
            .OrderBy(m => m.Codigo, StringComparer.Ordinal)
            .Select(m =>
            {
                var saldo = m.Debito - m.Credito;
                return new BalanceLineaDto
                {
                    CuentaContableId = m.CuentaContableId,
                    Codigo = m.Codigo,
                    Nombre = m.Nombre,
                    Tipo = m.Tipo,
                    Debito = m.Debito,
                    Credito = m.Credito,
                    SaldoDeudor = saldo > 0 ? saldo : 0,
                    SaldoAcreedor = saldo < 0 ? -saldo : 0
                };
            })
            .ToList();

        var totalDeb = lineas.Sum(l => l.Debito);
        var totalCred = lineas.Sum(l => l.Credito);

        return new BalanceComprobacionDto
        {
            Desde = d,
            Hasta = h,
            TotalDebito = totalDeb,
            TotalCredito = totalCred,
            TotalSaldoDeudor = lineas.Sum(l => l.SaldoDeudor),
            TotalSaldoAcreedor = lineas.Sum(l => l.SaldoAcreedor),
            Cuadrado = R(totalDeb) == R(totalCred),
            Lineas = lineas
        };
    }
}
