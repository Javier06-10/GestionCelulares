using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Common;

/// <summary>Página de resultados de un listado, con el total para paginar en la UI.</summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int Total { get; init; }
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling(Total / (double)TamanoPagina) : 0;

    /// <summary>Aplica Skip/Take sobre una consulta YA ordenada. Cap de seguridad: 1..200 por página.</summary>
    public static async Task<ResultadoPaginado<T>> DesdeConsultaAsync(IQueryable<T> consultaOrdenada, int pagina, int tamano)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamano = tamano < 1 ? 50 : (tamano > 200 ? 200 : tamano);

        var total = await consultaOrdenada.CountAsync();
        var items = await consultaOrdenada.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();

        return new ResultadoPaginado<T> { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamano };
    }
}
