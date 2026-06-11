using GestionCelulares.Application.Caja;

namespace GestionCelulares.Application.Common.Interfaces;

/// <summary>
/// Puerto para los procedimientos almacenados de caja.
/// La implementación (ADO.NET sobre el DbContext) vive en Infrastructure.
/// </summary>
public interface ICajaProcedures
{
    /// <summary>Ejecuta usp_Caja_Cerrar: calcula el esperado, la diferencia y cierra la sesión.</summary>
    Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, int usuarioCierre, decimal montoContado);
}
