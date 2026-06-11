using System.Data;
using GestionCelulares.Application.Caja;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Infrastructure.Persistence;

public class CajaProcedures : ICajaProcedures
{
    private readonly GestionCelularesContext _db;

    public CajaProcedures(GestionCelularesContext db) => _db = db;

    public async Task<CierreCajaResultadoDto> CerrarAsync(int sesionCajaId, int usuarioCierre, decimal montoContado)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_Caja_Cerrar";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add(new SqlParameter("@SesionCajaId", SqlDbType.Int) { Value = sesionCajaId });
        cmd.Parameters.Add(new SqlParameter("@UsuarioCierre", SqlDbType.Int) { Value = usuarioCierre });
        cmd.Parameters.Add(new SqlParameter("@MontoContado", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = montoContado });

        var esperado = new SqlParameter("@MontoEsperado", SqlDbType.Decimal)
        { Precision = 18, Scale = 2, Direction = ParameterDirection.Output };
        var diferencia = new SqlParameter("@Diferencia", SqlDbType.Decimal)
        { Precision = 18, Scale = 2, Direction = ParameterDirection.Output };
        cmd.Parameters.Add(esperado);
        cmd.Parameters.Add(diferencia);

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            // RAISERROR de validación del procedimiento (p. ej. sesión inexistente o ya cerrada)
            throw new CajaException(ex.Message);
        }

        return new CierreCajaResultadoDto
        {
            MontoEsperado = (decimal)esperado.Value,
            MontoContado = montoContado,
            Diferencia = (decimal)diferencia.Value
        };
    }
}
