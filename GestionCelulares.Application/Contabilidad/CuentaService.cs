using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Contabilidad;

public interface ICuentaService
{
    Task<IReadOnlyList<CuentaDto>> ListarAsync();
    Task<CuentaDto> CrearAsync(CuentaCrearDto dto);
    Task<CuentaDto> ActualizarAsync(int id, CuentaActualizarDto dto);
    Task EliminarAsync(int id);
}

public class CuentaService : ICuentaService
{
    private static readonly string[] TiposValidos = { "Activo", "Pasivo", "Capital", "Ingreso", "Costo", "Gasto" };

    private readonly IApplicationDbContext _db;

    public CuentaService(IApplicationDbContext db) => _db = db;

    private static string NaturalezaPorTipo(string tipo) =>
        tipo is "Activo" or "Costo" or "Gasto" ? "Deudora" : "Acreedora";

    public async Task<IReadOnlyList<CuentaDto>> ListarAsync()
    {
        var cuentas = await _db.CuentasContables.AsNoTracking().ToListAsync();
        return cuentas
            .OrderBy(c => c.Codigo, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    public async Task<CuentaDto> CrearAsync(CuentaCrearDto dto)
    {
        var codigo = dto.Codigo.Trim();
        if (await _db.CuentasContables.AnyAsync(c => c.Codigo == codigo))
            throw new ContabilidadException($"Ya existe una cuenta con el código '{codigo}'.");

        string tipo;
        if (dto.CuentaPadreId.HasValue)
        {
            var padre = await _db.CuentasContables.FirstOrDefaultAsync(c => c.CuentaContableId == dto.CuentaPadreId.Value)
                ?? throw new ContabilidadException("La cuenta padre no existe.");
            tipo = padre.Tipo;   // hereda el tipo del padre
        }
        else
        {
            tipo = (dto.Tipo ?? "").Trim();
            if (!TiposValidos.Contains(tipo))
                throw new ContabilidadException("El tipo de cuenta debe ser Activo, Pasivo, Capital, Ingreso, Costo o Gasto.");
        }

        var naturaleza = string.IsNullOrWhiteSpace(dto.Naturaleza) ? NaturalezaPorTipo(tipo) : dto.Naturaleza.Trim();

        var cuenta = new CuentaContable
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Tipo = tipo,
            CuentaPadreId = dto.CuentaPadreId,
            Naturaleza = naturaleza,
            PermiteMovimiento = dto.PermiteMovimiento,
            EsSistema = false,
            Activo = true
        };
        _db.CuentasContables.Add(cuenta);
        await _db.SaveChangesAsync();
        return Map(cuenta);
    }

    public async Task<CuentaDto> ActualizarAsync(int id, CuentaActualizarDto dto)
    {
        var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.CuentaContableId == id)
            ?? throw new ContabilidadException("La cuenta no existe.");

        cuenta.Nombre = dto.Nombre.Trim();
        cuenta.PermiteMovimiento = dto.PermiteMovimiento;
        cuenta.Activo = dto.Activo;
        await _db.SaveChangesAsync();
        return Map(cuenta);
    }

    public async Task EliminarAsync(int id)
    {
        var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.CuentaContableId == id)
            ?? throw new ContabilidadException("La cuenta no existe.");

        if (cuenta.EsSistema)
            throw new ContabilidadException("Las cuentas base del sistema no se pueden eliminar (puedes desactivarlas).");
        if (await _db.CuentasContables.AnyAsync(c => c.CuentaPadreId == id))
            throw new ContabilidadException("La cuenta tiene sub-cuentas; elimínalas primero.");

        _db.CuentasContables.Remove(cuenta);
        await _db.SaveChangesAsync();
    }

    private static CuentaDto Map(CuentaContable c) => new()
    {
        CuentaContableId = c.CuentaContableId,
        Codigo = c.Codigo,
        Nombre = c.Nombre,
        Tipo = c.Tipo,
        CuentaPadreId = c.CuentaPadreId,
        Naturaleza = c.Naturaleza,
        PermiteMovimiento = c.PermiteMovimiento,
        EsSistema = c.EsSistema,
        Activo = c.Activo,
        Nivel = c.Codigo.Count(ch => ch == '.')
    };
}
