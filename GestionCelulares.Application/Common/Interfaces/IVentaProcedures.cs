using GestionCelulares.Application.Ventas;

namespace GestionCelulares.Application.Common.Interfaces;

/// <summary>
/// Puerto para usp_Venta_Registrar (recibe el detalle como TVP dbo.VentaDetalleTipo).
/// La implementación ADO.NET vive en Infrastructure.
/// </summary>
public interface IVentaProcedures
{
    /// <summary>Ejecuta la venta transaccional y devuelve el VentaId generado.</summary>
    Task<int> RegistrarAsync(VentaRegistroDto dto, int usuarioId, int? sesionCajaId);
}
