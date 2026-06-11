using GestionCelulares.Application.Creditos;

namespace GestionCelulares.Application.Common.Interfaces;

/// <summary>
/// Puerto para los procedimientos almacenados de créditos.
/// La implementación ADO.NET vive en Infrastructure.
/// </summary>
public interface ICreditoProcedures
{
    /// <summary>usp_Credito_Crear: crea el financiamiento y su tabla de cuotas. Devuelve el CreditoId.</summary>
    Task<int> CrearAsync(CreditoCrearDto dto);

    /// <summary>usp_PagoCredito_Registrar: aplica el abono cuota por cuota. Devuelve el PagoCreditoId.</summary>
    Task<int> RegistrarAbonoAsync(int creditoId, AbonoRegistroDto dto, int? usuarioId);

    /// <summary>usp_Creditos_ActualizarMora: marca cuotas vencidas y créditos/clientes en mora.</summary>
    Task<MoraResultadoDto> ActualizarMoraAsync(DateTime? fechaCorte);
}
