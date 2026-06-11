using System.Data;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Creditos;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Infrastructure.Persistence;

public class CreditoProcedures : ICreditoProcedures
{
    private readonly GestionCelularesContext _db;

    public CreditoProcedures(GestionCelularesContext db) => _db = db;

    public async Task<int> CrearAsync(CreditoCrearDto dto)
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_Credito_Crear";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add(new SqlParameter("@ClienteId", SqlDbType.Int) { Value = dto.ClienteId });
        cmd.Parameters.Add(new SqlParameter("@MontoFinanciado", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.MontoFinanciado });
        cmd.Parameters.Add(new SqlParameter("@NumeroCuotas", SqlDbType.Int) { Value = dto.NumeroCuotas });
        cmd.Parameters.Add(new SqlParameter("@FechaPrimerVencimiento", SqlDbType.Date) { Value = dto.FechaPrimerVencimiento.Date });
        cmd.Parameters.Add(new SqlParameter("@VentaId", SqlDbType.Int) { Value = (object?)dto.VentaId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Inicial", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.Inicial });
        cmd.Parameters.Add(new SqlParameter("@TasaInteresMensual", SqlDbType.Decimal) { Precision = 9, Scale = 4, Value = dto.TasaInteresMensual });

        var creditoId = new SqlParameter("@CreditoId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(creditoId);

        await EjecutarAsync(cmd);
        return (int)creditoId.Value;
    }

    public async Task<int> RegistrarAbonoAsync(int creditoIdValor, AbonoRegistroDto dto, int? usuarioId)
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_PagoCredito_Registrar";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add(new SqlParameter("@CreditoId", SqlDbType.Int) { Value = creditoIdValor });
        cmd.Parameters.Add(new SqlParameter("@Monto", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.Monto });
        cmd.Parameters.Add(new SqlParameter("@MetodoPagoId", SqlDbType.Int) { Value = (object?)dto.MetodoPagoId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@SesionCajaId", SqlDbType.Int) { Value = (object?)dto.SesionCajaId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = (object?)usuarioId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CuotaId", SqlDbType.Int) { Value = (object?)dto.CuotaId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@NumeroRecibo", SqlDbType.NVarChar, 30) { Value = (object?)dto.NumeroRecibo ?? DBNull.Value });

        var pagoId = new SqlParameter("@PagoCreditoId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pagoId);

        await EjecutarAsync(cmd);
        return (int)pagoId.Value;
    }

    public async Task<MoraResultadoDto> ActualizarMoraAsync(DateTime? fechaCorte)
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_Creditos_ActualizarMora";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@FechaCorte", SqlDbType.Date)
        { Value = (object?)fechaCorte?.Date ?? DBNull.Value });

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new CreditoException("El proceso de mora no devolvió resultados.");

            return new MoraResultadoDto
            {
                FechaCorte = reader.GetDateTime(reader.GetOrdinal("FechaCorte")),
                CuotasMarcadasVencidas = reader.GetInt32(reader.GetOrdinal("CuotasMarcadasVencidas")),
                CreditosPuestosEnMora = reader.GetInt32(reader.GetOrdinal("CreditosPuestosEnMora")),
                CreditosReactivados = reader.GetInt32(reader.GetOrdinal("CreditosReactivados")),
                ClientesMorososActualizados = reader.GetInt32(reader.GetOrdinal("ClientesMorososActualizados"))
            };
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            throw new CreditoException(ex.Message);
        }
    }

    private async Task<System.Data.Common.DbConnection> AbrirAsync()
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();
        return conn;
    }

    private static async Task EjecutarAsync(System.Data.Common.DbCommand cmd)
    {
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            // RAISERROR de validación del procedimiento
            throw new CreditoException(ex.Message);
        }
    }
}
