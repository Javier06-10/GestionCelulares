using System.Data;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Infrastructure.Persistence;

public class VentaProcedures : IVentaProcedures
{
    private readonly GestionCelularesContext _db;

    public VentaProcedures(GestionCelularesContext db) => _db = db;

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
