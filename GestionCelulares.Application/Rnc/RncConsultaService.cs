using GestionCelulares.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Rnc;

public class RncConsultaDto
{
    public string Rnc { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Estado { get; set; }
}

public interface IRncConsultaService
{
    /// <summary>Consulta un RNC/cédula en el padrón de la DGII. Devuelve null si no existe.</summary>
    Task<RncConsultaDto?> ConsultarAsync(string rnc);
}

public class RncConsultaService : IRncConsultaService
{
    private readonly IApplicationDbContext _db;

    public RncConsultaService(IApplicationDbContext db) => _db = db;

    public async Task<RncConsultaDto?> ConsultarAsync(string rnc)
    {
        var digitos = new string((rnc ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 0) return null;
        var clave = digitos.PadLeft(11, '0');   // el padrón usa 11 dígitos con ceros a la izquierda

        return await _db.PadronRnc.AsNoTracking()
            .Where(p => p.Rnc == clave)
            .Select(p => new RncConsultaDto { Rnc = p.Rnc, Nombre = p.Nombre, Estado = p.Estado })
            .FirstOrDefaultAsync();
    }
}
