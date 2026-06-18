namespace GestionCelulares.Application.Common.Interfaces;

/// <summary>Puerto para la asignación atómica de NCF (usp_Ncf_Siguiente).</summary>
public interface INcfProcedures
{
    /// <summary>Devuelve el próximo NCF del tipo indicado, o null si no hay rango disponible/activo.</summary>
    Task<string?> SiguienteNcfAsync(string tipo);
}
