using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Contabilidad;

public interface IContabilizacionService
{
    /// <summary>Genera (idempotentemente) los asientos de ventas, nómina y caja del rango.</summary>
    Task<ContabilizacionResultadoDto> ContabilizarPendientesAsync(DateTime desde, DateTime hasta, int? usuarioId);
    Task<EstadoResultadosDto> EstadoResultadosAsync(DateTime desde, DateTime hasta);
    Task<BalanceGeneralDto> BalanceGeneralAsync(DateTime hasta);
}

public class ContabilizacionService : IContabilizacionService
{
    private readonly IApplicationDbContext _db;

    public ContabilizacionService(IApplicationDbContext db) => _db = db;

    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // Códigos del plan de cuentas base (v11). Si el usuario los renombra el código se mantiene.
    private const string CajaCod = "1.01";
    private const string CxCCod = "1.03";
    private const string ItbisPorPagarCod = "2.02";
    private const string VentasCod = "4.01";
    private const string OtrosIngresosCod = "4.03";
    private const string SueldosCod = "6.01";
    private const string ComisionesCod = "6.04";
    private const string OtrosGastosCod = "6.06";

    // ---------------------------------------------------------------- Posting

    public async Task<ContabilizacionResultadoDto> ContabilizarPendientesAsync(DateTime desde, DateTime hasta, int? usuarioId)
    {
        var d = desde.Date;
        var hExclusivo = hasta.Date.AddDays(1);

        // Cuentas por código (deben existir, permitir movimiento y estar activas)
        var cuentas = await _db.CuentasContables.AsNoTracking().ToListAsync();
        var porCodigo = cuentas.ToDictionary(c => c.Codigo, c => c);
        int Cuenta(string codigo)
        {
            if (!porCodigo.TryGetValue(codigo, out var c))
                throw new ContabilidadException($"Falta la cuenta {codigo} en el catálogo. Restáurala antes de contabilizar.");
            if (!c.PermiteMovimiento || !c.Activo)
                throw new ContabilidadException($"La cuenta {c.Codigo} {c.Nombre} no admite movimientos o está inactiva.");
            return c.CuentaContableId;
        }

        // Documentos ya contabilizados (idempotencia)
        var hechos = await _db.Asientos.AsNoTracking()
            .Where(a => a.ReferenciaId != null)
            .Select(a => new { a.Origen, Ref = a.ReferenciaId!.Value })
            .ToListAsync();
        var ventasHechas = hechos.Where(x => x.Origen == "Venta").Select(x => x.Ref).ToHashSet();
        var nominaHechas = hechos.Where(x => x.Origen == "Nomina").Select(x => x.Ref).ToHashSet();
        var cajaHechas = hechos.Where(x => x.Origen == "Caja").Select(x => x.Ref).ToHashSet();

        var numero = await _db.Asientos.MaxAsync(a => (int?)a.Numero) ?? 0;
        var nuevos = new List<AsientoContable>();
        var resultado = new ContabilizacionResultadoDto();

        AsientoContable Crear(DateTime fecha, string concepto, string origen, int referenciaId, params (int cuentaId, decimal debito, decimal credito)[] lineas)
        {
            var det = lineas
                .Where(l => R(l.debito) != 0 || R(l.credito) != 0)
                .Select(l => new AsientoDetalle { CuentaContableId = l.cuentaId, Debito = R(l.debito), Credito = R(l.credito) })
                .ToList();
            if (R(det.Sum(x => x.Debito)) != R(det.Sum(x => x.Credito)))
                throw new ContabilidadException($"El asiento automático de {origen} #{referenciaId} no cuadra.");

            return new AsientoContable
            {
                Numero = ++numero,
                Fecha = fecha.Date,
                Concepto = concepto,
                Origen = origen,
                ReferenciaId = referenciaId,
                Estado = "Registrado",
                UsuarioId = usuarioId,
                FechaRegistro = DateTime.Now,
                Detalles = det
            };
        }

        // ---- Ventas completadas ----
        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Completada" && v.Fecha >= d && v.Fecha < hExclusivo)
            .Select(v => new { v.VentaId, v.NumeroFactura, v.Fecha, v.Total, v.Impuesto, v.EsCredito })
            .ToListAsync();

        foreach (var v in ventas.Where(v => !ventasHechas.Contains(v.VentaId)))
        {
            var itbis = R(v.Impuesto);
            var neto = R(v.Total) - itbis;
            var cargo = v.EsCredito ? Cuenta(CxCCod) : Cuenta(CajaCod);
            nuevos.Add(Crear(v.Fecha, $"Venta factura {v.NumeroFactura}", "Venta", v.VentaId,
                (cargo, R(v.Total), 0),
                (Cuenta(VentasCod), 0, neto),
                (Cuenta(ItbisPorPagarCod), 0, itbis)));
            resultado.Ventas++;
        }

        // ---- Pagos de nómina ----
        var pagos = await _db.PagosEmpleado.AsNoTracking()
            .Where(p => p.Fecha >= d && p.Fecha < hExclusivo)
            .Select(p => new { p.PagoEmpleadoId, p.Tipo, p.Monto, p.Fecha, Empleado = p.Empleado.NombreCompleto })
            .ToListAsync();

        foreach (var p in pagos.Where(p => !nominaHechas.Contains(p.PagoEmpleadoId)))
        {
            var gasto = p.Tipo == "Comisión" ? Cuenta(ComisionesCod) : Cuenta(SueldosCod);
            nuevos.Add(Crear(p.Fecha, $"{p.Tipo} a {p.Empleado}", "Nomina", p.PagoEmpleadoId,
                (gasto, R(p.Monto), 0),
                (Cuenta(CajaCod), 0, R(p.Monto))));
            resultado.Nomina++;
        }

        // ---- Movimientos de caja manuales (ingresos / egresos) ----
        var movs = await _db.MovimientosCaja.AsNoTracking()
            .Where(m => m.Fecha >= d && m.Fecha < hExclusivo)
            .Select(m => new { m.MovimientoCajaId, m.Tipo, m.Concepto, m.Monto, m.Fecha })
            .ToListAsync();

        foreach (var m in movs.Where(m => !cajaHechas.Contains((int)m.MovimientoCajaId)))
        {
            if (m.Tipo == "Ingreso")
                nuevos.Add(Crear(m.Fecha, $"Ingreso de caja: {m.Concepto}", "Caja", (int)m.MovimientoCajaId,
                    (Cuenta(CajaCod), R(m.Monto), 0),
                    (Cuenta(OtrosIngresosCod), 0, R(m.Monto))));
            else
                nuevos.Add(Crear(m.Fecha, $"Egreso de caja: {m.Concepto}", "Caja", (int)m.MovimientoCajaId,
                    (Cuenta(OtrosGastosCod), R(m.Monto), 0),
                    (Cuenta(CajaCod), 0, R(m.Monto))));
            resultado.Caja++;
        }

        if (nuevos.Count > 0)
        {
            _db.Asientos.AddRange(nuevos);
            await _db.SaveChangesAsync();
        }

        if (resultado.Total == 0)
            resultado.Mensajes.Add("No había documentos pendientes de contabilizar en el período.");
        else
            resultado.Mensajes.Add($"Se generaron {resultado.Total} asiento(s): {resultado.Ventas} de ventas, {resultado.Nomina} de nómina, {resultado.Caja} de caja.");

        return resultado;
    }

    // ---------------------------------------------------- Estado de Resultados

    public async Task<EstadoResultadosDto> EstadoResultadosAsync(DateTime desde, DateTime hasta)
    {
        var d = desde.Date;
        var h = hasta.Date;

        var movs = await SaldosPorCuentaAsync(d, h);

        EstadoResultadosLineaDto Linea(SaldoCuenta m, bool acreedora) => new()
        {
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Monto = acreedora ? m.Credito - m.Debito : m.Debito - m.Credito
        };

        var ingresos = movs.Where(m => m.Tipo == "Ingreso").Select(m => Linea(m, true)).Where(l => l.Monto != 0).ToList();
        var costos = movs.Where(m => m.Tipo == "Costo").Select(m => Linea(m, false)).Where(l => l.Monto != 0).ToList();
        var gastos = movs.Where(m => m.Tipo == "Gasto").Select(m => Linea(m, false)).Where(l => l.Monto != 0).ToList();

        var totalIngresos = ingresos.Sum(l => l.Monto);
        var totalCostos = costos.Sum(l => l.Monto);
        var totalGastos = gastos.Sum(l => l.Monto);

        return new EstadoResultadosDto
        {
            Desde = d,
            Hasta = h,
            Ingresos = ingresos,
            Costos = costos,
            Gastos = gastos,
            TotalIngresos = totalIngresos,
            TotalCostos = totalCostos,
            UtilidadBruta = totalIngresos - totalCostos,
            TotalGastos = totalGastos,
            UtilidadNeta = totalIngresos - totalCostos - totalGastos
        };
    }

    // -------------------------------------------------------- Balance General

    public async Task<BalanceGeneralDto> BalanceGeneralAsync(DateTime hasta)
    {
        var h = hasta.Date;

        // Acumulado desde el inicio de las operaciones hasta la fecha de corte
        var movs = await SaldosPorCuentaAsync(null, h);

        BalanceGeneralLineaDto Linea(SaldoCuenta m, bool acreedora) => new()
        {
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Monto = acreedora ? m.Credito - m.Debito : m.Debito - m.Credito
        };

        var activos = movs.Where(m => m.Tipo == "Activo").Select(m => Linea(m, false)).Where(l => l.Monto != 0).ToList();
        var pasivos = movs.Where(m => m.Tipo == "Pasivo").Select(m => Linea(m, true)).Where(l => l.Monto != 0).ToList();
        var capital = movs.Where(m => m.Tipo == "Capital").Select(m => Linea(m, true)).Where(l => l.Monto != 0).ToList();

        // Resultado del ejercicio = Ingresos - Costos - Gastos acumulados (aún no cerrado a Capital)
        var ingresos = movs.Where(m => m.Tipo == "Ingreso").Sum(m => m.Credito - m.Debito);
        var costos = movs.Where(m => m.Tipo == "Costo").Sum(m => m.Debito - m.Credito);
        var gastos = movs.Where(m => m.Tipo == "Gasto").Sum(m => m.Debito - m.Credito);
        var resultado = ingresos - costos - gastos;

        var totalActivos = activos.Sum(l => l.Monto);
        var totalPasivos = pasivos.Sum(l => l.Monto);
        var capitalAportado = capital.Sum(l => l.Monto);
        var totalCapital = capitalAportado + resultado;

        return new BalanceGeneralDto
        {
            Hasta = h,
            Activos = activos,
            Pasivos = pasivos,
            Capital = capital,
            TotalActivos = totalActivos,
            TotalPasivos = totalPasivos,
            CapitalAportado = capitalAportado,
            ResultadoEjercicio = resultado,
            TotalCapital = totalCapital,
            TotalPasivoCapital = totalPasivos + totalCapital,
            Cuadrado = R(totalActivos) == R(totalPasivos + totalCapital)
        };
    }

    // --------------------------------------------------------------- Helpers

    private record SaldoCuenta(string Codigo, string Nombre, string Tipo, decimal Debito, decimal Credito);

    /// <summary>Suma débitos y créditos por cuenta de los asientos registrados en el rango.</summary>
    private async Task<List<SaldoCuenta>> SaldosPorCuentaAsync(DateTime? desde, DateTime? hasta)
    {
        var q = _db.AsientoDetalles.AsNoTracking()
            .Where(x => x.Asiento.Estado == "Registrado");
        if (desde.HasValue) q = q.Where(x => x.Asiento.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Asiento.Fecha <= hasta.Value.Date);

        var movs = await q
            .GroupBy(x => new { x.Cuenta.Codigo, x.Cuenta.Nombre, x.Cuenta.Tipo })
            .Select(g => new SaldoCuenta(
                g.Key.Codigo, g.Key.Nombre, g.Key.Tipo,
                g.Sum(x => x.Debito), g.Sum(x => x.Credito)))
            .ToListAsync();

        return movs.OrderBy(m => m.Codigo, StringComparer.Ordinal).ToList();
    }
}
