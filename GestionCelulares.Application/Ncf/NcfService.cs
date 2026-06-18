using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Ncf;

public interface INcfService
{
    Task<IReadOnlyList<SecuenciaNcfDto>> ListarAsync();
    Task<SecuenciaNcfDto> ConfigurarAsync(int id, SecuenciaNcfConfigDto dto);
}

public class NcfService : INcfService
{
    private readonly IApplicationDbContext _db;

    public NcfService(IApplicationDbContext db) => _db = db;

    private static string Descripcion(string tipo) => tipo switch
    {
        "01" => "Factura de Crédito Fiscal",
        "02" => "Factura de Consumo",
        "03" => "Nota de Débito",
        "04" => "Nota de Crédito",
        "11" => "Comprobante de Compras",
        "14" => "Régimen Especial",
        "15" => "Gubernamental",
        _ => "Comprobante " + tipo
    };

    public async Task<IReadOnlyList<SecuenciaNcfDto>> ListarAsync()
    {
        var secs = await _db.SecuenciasNcf.AsNoTracking().OrderBy(s => s.TipoComprobante).ToListAsync();
        return secs.Select(Map).ToList();
    }

    public async Task<SecuenciaNcfDto> ConfigurarAsync(int id, SecuenciaNcfConfigDto dto)
    {
        var sec = await _db.SecuenciasNcf.FirstOrDefaultAsync(s => s.SecuenciaNcfId == id)
            ?? throw new NcfException("La secuencia de NCF no existe.");

        if (dto.Activo && dto.Hasta < dto.Secuencia)
            throw new NcfException("El límite ('hasta') no puede ser menor que la secuencia inicial.");

        sec.Serie = string.IsNullOrWhiteSpace(dto.Serie) ? "B" : dto.Serie.Trim().ToUpperInvariant();
        sec.Secuencia = dto.Secuencia;
        sec.Hasta = dto.Hasta;
        sec.Vencimiento = dto.Vencimiento;
        sec.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return Map(sec);
    }

    private static SecuenciaNcfDto Map(Domain.Entities.SecuenciaNcf s) => new()
    {
        SecuenciaNcfId = s.SecuenciaNcfId,
        TipoComprobante = s.TipoComprobante,
        Descripcion = Descripcion(s.TipoComprobante),
        Serie = s.Serie,
        Secuencia = s.Secuencia,
        Hasta = s.Hasta,
        Disponibles = s.Hasta >= s.Secuencia ? s.Hasta - s.Secuencia + 1 : 0,
        Ejemplo = s.Serie + s.TipoComprobante + s.Secuencia.ToString().PadLeft(8, '0'),
        Vencimiento = s.Vencimiento,
        Activo = s.Activo
    };
}
