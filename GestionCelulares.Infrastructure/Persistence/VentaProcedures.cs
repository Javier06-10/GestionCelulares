using System.Data;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Infrastructure.Persistence;

public class VentaProcedures : IVentaProcedures, ISecuenciaFactura, INcfProcedures
{
    private readonly GestionCelularesContext _db;

    public VentaProcedures(GestionCelularesContext db) => _db = db;

    public async Task<string?> SiguienteNcfAsync(string tipo)
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_Ncf_Siguiente";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.NVarChar, 2) { Value = tipo });
        var salida = new SqlParameter("@Ncf", SqlDbType.NVarChar, 19) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(salida);
        await cmd.ExecuteNonQueryAsync();
        return salida.Value as string;   // null si no hay NCF disponible
    }

    private async Task<System.Data.Common.DbConnection> AbrirAsync()
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();
        return conn;
    }

    public async Task<string> ProximoNumeroFacturaAsync()
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        // UPDATE ... OUTPUT incrementa y devuelve el nuevo valor en una sola operación atómica
        cmd.CommandText = @"
            UPDATE dbo.Secuencia
               SET Valor = Valor + 1
             OUTPUT Prefijo = inserted.Prefijo, inserted.Valor, inserted.Longitud
             WHERE Nombre = N'Factura';";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return string.Empty; // sin secuencia configurada -> la venta usará null
        var prefijo = reader.GetString(0);
        var valor = reader.GetInt32(1);
        var longitud = reader.GetInt32(2);
        return prefijo + valor.ToString().PadLeft(longitud, '0');
    }

    public async Task<(string prefijo, int proximo, int longitud)> ObtenerAsync()
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Prefijo, Valor, Longitud FROM dbo.Secuencia WHERE Nombre = N'Factura';";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return ("FAC-", 1, 6);
        return (reader.GetString(0), reader.GetInt32(1) + 1, reader.GetInt32(2));
    }

    public async Task GuardarAsync(string prefijo, int proximo, int longitud)
    {
        var conn = await AbrirAsync();
        await using var cmd = conn.CreateCommand();
        // 'proximo' es el siguiente a emitir; guardamos Valor = proximo - 1
        cmd.CommandText = @"
            MERGE dbo.Secuencia AS t
            USING (SELECT N'Factura' AS Nombre) AS s ON t.Nombre = s.Nombre
            WHEN MATCHED THEN UPDATE SET Prefijo = @p, Valor = @v, Longitud = @l
            WHEN NOT MATCHED THEN INSERT (Nombre, Prefijo, Valor, Longitud) VALUES (N'Factura', @p, @v, @l);";
        cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.NVarChar, 15) { Value = prefijo ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@v", SqlDbType.Int) { Value = Math.Max(0, proximo - 1) });
        cmd.Parameters.Add(new SqlParameter("@l", SqlDbType.Int) { Value = Math.Clamp(longitud, 1, 12) });
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> RegistrarAsync(VentaRegistroDto dto, int usuarioId, int? sesionCajaId)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_Venta_Registrar";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add(new SqlParameter("@SucursalId", SqlDbType.Int) { Value = dto.SucursalId });
        cmd.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = usuarioId });
        cmd.Parameters.Add(new SqlParameter("@ClienteId", SqlDbType.Int) { Value = (object?)dto.ClienteId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@SesionCajaId", SqlDbType.Int) { Value = (object?)sesionCajaId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@EsCredito", SqlDbType.Bit) { Value = dto.EsCredito });
        cmd.Parameters.Add(new SqlParameter("@MetodoPagoId", SqlDbType.Int) { Value = (object?)dto.MetodoPagoId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@NumeroFactura", SqlDbType.NVarChar, 30)
        { Value = (object?)dto.NumeroFactura ?? DBNull.Value });

        // TVP dbo.VentaDetalleTipo (las columnas deben ir en el mismo orden que el tipo)
        var detalles = new DataTable();
        detalles.Columns.Add("ImeiId", typeof(int));
        detalles.Columns.Add("VarianteId", typeof(int));
        detalles.Columns.Add("Cantidad", typeof(int));
        detalles.Columns.Add("PrecioUnitario", typeof(decimal));
        detalles.Columns.Add("Descuento", typeof(decimal));
        foreach (var d in dto.Detalles)
            detalles.Rows.Add((object?)d.ImeiId ?? DBNull.Value, d.VarianteId, d.Cantidad, d.PrecioUnitario, d.Descuento);

        cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
        {
            TypeName = "dbo.VentaDetalleTipo",
            Value = detalles
        });

        var ventaId = new SqlParameter("@VentaId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(ventaId);

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            // RAISERROR de validación del procedimiento (IMEI inexistente/no disponible, etc.)
            throw new VentaException(ex.Message);
        }

        return (int)ventaId.Value;
    }
}
