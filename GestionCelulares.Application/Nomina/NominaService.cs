using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Nomina;

public interface INominaService
{
    Task<IReadOnlyList<EmpleadoDto>> EmpleadosAsync();
    Task<NominaResumenDto> ListarAsync(int? empleadoId, DateTime? desde, DateTime? hasta);
    Task<PagoEmpleadoDto> RegistrarAsync(PagoEmpleadoCrearDto dto, int? registradoPor);
}

public class NominaService : INominaService
{
    private static readonly string[] TiposValidos = { "Salario", "Adelanto", "Bono", "Comisión" };

    private readonly IApplicationDbContext _db;

    public NominaService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmpleadoDto>> EmpleadosAsync()
        => await _db.Usuarios.AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new EmpleadoDto
            {
                UsuarioId = u.UsuarioId,
                NombreCompleto = u.NombreCompleto,
                Rol = u.Rol.Nombre
            })
            .ToListAsync();

    public async Task<NominaResumenDto> ListarAsync(int? empleadoId, DateTime? desde, DateTime? hasta)
    {
        var q = _db.PagosEmpleado.AsNoTracking();

        if (empleadoId.HasValue)
            q = q.Where(p => p.EmpleadoId == empleadoId.Value);
        if (desde.HasValue)
            q = q.Where(p => p.Fecha >= desde.Value.Date);
        if (hasta.HasValue)
            q = q.Where(p => p.Fecha < hasta.Value.Date.AddDays(1));

        var pagos = await q.OrderByDescending(p => p.Fecha)
            .Select(p => new PagoEmpleadoDto
            {
                PagoEmpleadoId = p.PagoEmpleadoId,
                EmpleadoId = p.EmpleadoId,
                Empleado = p.Empleado.NombreCompleto,
                Rol = p.Empleado.Rol.Nombre,
                Tipo = p.Tipo,
                Monto = p.Monto,
                Periodo = p.Periodo,
                Notas = p.Notas,
                Fecha = p.Fecha
            })
            .ToListAsync();

        return new NominaResumenDto
        {
            Pagos = pagos,
            TotalPagado = pagos.Sum(p => p.Monto)
        };
    }

    public async Task<PagoEmpleadoDto> RegistrarAsync(PagoEmpleadoCrearDto dto, int? registradoPor)
    {
        var empleado = await _db.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.UsuarioId == dto.EmpleadoId)
            ?? throw new NominaException("El empleado indicado no existe.");

        if (!empleado.Activo)
            throw new NominaException($"El empleado '{empleado.NombreCompleto}' está inactivo.");

        var tipo = (dto.Tipo ?? "").Trim();
        if (!TiposValidos.Contains(tipo))
            throw new NominaException("El tipo de pago debe ser Salario, Adelanto, Bono o Comisión.");

        var pago = new PagoEmpleado
        {
            EmpleadoId = dto.EmpleadoId,
            Tipo = tipo,
            Monto = dto.Monto,
            Periodo = string.IsNullOrWhiteSpace(dto.Periodo) ? null : dto.Periodo.Trim(),
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            RegistradoPor = registradoPor,
            Fecha = DateTime.Now
        };
        _db.PagosEmpleado.Add(pago);
        await _db.SaveChangesAsync();

        return new PagoEmpleadoDto
        {
            PagoEmpleadoId = pago.PagoEmpleadoId,
            EmpleadoId = pago.EmpleadoId,
            Empleado = empleado.NombreCompleto,
            Rol = empleado.Rol.Nombre,
            Tipo = pago.Tipo,
            Monto = pago.Monto,
            Periodo = pago.Periodo,
            Notas = pago.Notas,
            Fecha = pago.Fecha
        };
    }
}
