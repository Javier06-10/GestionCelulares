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
    Task<CierreEjercicioResultadoDto> CerrarEjercicioAsync(DateTime hasta, int? usuarioId);
}

public class ContabilizacionService : IContabilizacionService
{
    private readonly IApplicationDbContext _db;

    public ContabilizacionService(IApplicationDbContext db) => _db = db;

    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // Códigos del plan de cuentas base (v11). Si el usuario los renombra el código se mantiene.
    private const string CajaCod = "1.01";
    private const string BancosCod = "1.02";
    private const string CxCCod = "1.03";
    private const string InventarioCod = "1.04";
    private const string ItbisAdelantadoCod = "1.05";
    private const string MobiliarioCod = "1.06";
    private const string CxPCod = "2.01";
    private const string ItbisPorPagarCod = "2.02";
    private const string ResultadosAcumCod = "3.02";
    private const string VentasCod = "4.01";
    private const string OtrosIngresosCod = "4.03";
    private const string CostoVentaCod = "5.01";
    private const string SueldosCod = "6.01";
    private const string AlquilerCod = "6.02";
    private const string ComisionesCod = "6.04";
    private const string OtrosGastosCod = "6.06";

    /// <summary>Cuenta de cargo de una compra según el tipo de bien/servicio DGII (606).</summary>
    private static string CuentaCompraPorTipo(string? tipoDgii) => tipoDgii switch
    {
        "10" => MobiliarioCod,    // 10 - Adquisiciones de activos fijos
        "01" => SueldosCod,       // 01 - Gastos de personal
        "03" => AlquilerCod,      // 03 - Arrendamientos
        "02" or "04" or "05" or "06" or "07" or "08" or "11" => OtrosGastosCod,
        _ => InventarioCod        // 09 (mercancía/costo de venta) y por defecto
    };

    /// <summary>Un cobro/pago va a Bancos si el método es tarjeta, transferencia, cheque o depósito; si no, a Caja.</summary>
    private static bool EsBanco(string? metodo)
    {
        if (string.IsNullOrWhiteSpace(metodo)) return false;
        var m = metodo.ToLowerInvariant();
        return m.Contains("tarjeta") || m.Contains("transfer") || m.Contains("banco")
            || m.Contains("cheque") || m.Contains("deposit") || m.Contains("depósit");
    }

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
        var comprasHechas = hechos.Where(x => x.Origen == "Compra").Select(x => x.Ref).ToHashSet();
        var pagosProvHechos = hechos.Where(x => x.Origen == "PagoProv").Select(x => x.Ref).ToHashSet();
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

        var pendientesVenta = ventas.Where(v => !ventasHechas.Contains(v.VentaId)).Select(v => v.VentaId).ToList();

        // Costo de mercancía vendida por venta: IMEI serializado usa su costo de compra;
        // accesorio no serializado usa el costo de la variante por la cantidad.
        var costoPorVenta = await _db.VentaDetalles.AsNoTracking()
            .Where(dt => pendientesVenta.Contains(dt.VentaId))
            .GroupBy(dt => dt.VentaId)
            .Select(g => new
            {
                VentaId = g.Key,
                Costo = g.Sum(dt => dt.ImeiId != null ? dt.Imei!.PrecioCosto : dt.Variante.PrecioCosto * dt.Cantidad)
            })
            .ToDictionaryAsync(x => x.VentaId, x => x.Costo);

        // Cobros por banco (tarjeta/transferencia/etc.) por venta de contado
        var pagosVenta = await _db.VentaPagos.AsNoTracking()
            .Where(p => pendientesVenta.Contains(p.VentaId))
            .Select(p => new { p.VentaId, p.Monto, Metodo = p.MetodoPago.Nombre })
            .ToListAsync();
        var bancoPorVenta = pagosVenta.Where(p => EsBanco(p.Metodo))
            .GroupBy(p => p.VentaId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        foreach (var v in ventas.Where(v => !ventasHechas.Contains(v.VentaId)))
        {
            var itbis = R(v.Impuesto);
            var neto = R(v.Total) - itbis;
            var costo = R(costoPorVenta.TryGetValue(v.VentaId, out var c) ? c : 0);

            var lineas = new List<(int, decimal, decimal)>();
            // Cobro
            if (v.EsCredito)
                lineas.Add((Cuenta(CxCCod), R(v.Total), 0));
            else
            {
                var banco = Math.Min(R(bancoPorVenta.TryGetValue(v.VentaId, out var b) ? b : 0), R(v.Total));
                var efectivo = R(v.Total) - banco;
                lineas.Add((Cuenta(BancosCod), banco, 0));
                lineas.Add((Cuenta(CajaCod), efectivo, 0));
            }
            // Ingreso
            lineas.Add((Cuenta(VentasCod), 0, neto));
            lineas.Add((Cuenta(ItbisPorPagarCod), 0, itbis));
            // Costo de la mercancía vendida (descarga el inventario al costo)
            lineas.Add((Cuenta(CostoVentaCod), costo, 0));
            lineas.Add((Cuenta(InventarioCod), 0, costo));

            nuevos.Add(Crear(v.Fecha, $"Venta factura {v.NumeroFactura}", "Venta", v.VentaId, lineas.ToArray()));
            resultado.Ventas++;
        }

        // ---- Compras (inventario o gasto/activo según el tipo DGII) ----
        var compras = await _db.Compras.AsNoTracking()
            .Where(co => co.Fecha >= d && co.Fecha < hExclusivo)
            .Select(co => new { co.CompraId, co.NumeroFactura, co.Fecha, co.Total, co.Itbis, co.TipoBienServicio, co.MetodoPagoId, Metodo = co.MetodoPago!.Nombre })
            .ToListAsync();

        foreach (var co in compras.Where(co => !comprasHechas.Contains(co.CompraId)))
        {
            var itbis = R(co.Itbis ?? 0);
            var baseInv = R(co.Total) - itbis;   // se deriva del total para garantizar el cuadre
            var cuentaCargo = Cuenta(CuentaCompraPorTipo(co.TipoBienServicio));
            var lineas = new List<(int, decimal, decimal)>
            {
                (cuentaCargo, baseInv, 0),
                (Cuenta(ItbisAdelantadoCod), itbis, 0)
            };
            // Crédito: contado (Caja/Bancos) o a crédito (Cuentas por Pagar)
            if (co.MetodoPagoId != null)
                lineas.Add((EsBanco(co.Metodo) ? Cuenta(BancosCod) : Cuenta(CajaCod), 0, R(co.Total)));
            else
                lineas.Add((Cuenta(CxPCod), 0, R(co.Total)));

            nuevos.Add(Crear(co.Fecha, $"Compra {co.NumeroFactura ?? "s/n"}", "Compra", co.CompraId, lineas.ToArray()));
            resultado.Compras++;
        }

        // ---- Pagos a proveedores (abono a Cuentas por Pagar) ----
        var pagosProv = await _db.PagosProveedor.AsNoTracking()
            .Where(p => p.Fecha >= d && p.Fecha < hExclusivo)
            .Select(p => new { p.PagoProveedorId, p.Fecha, p.Monto, Proveedor = p.Proveedor.Nombre })
            .ToListAsync();

        foreach (var p in pagosProv.Where(p => !pagosProvHechos.Contains(p.PagoProveedorId)))
        {
            nuevos.Add(Crear(p.Fecha, $"Pago a proveedor {p.Proveedor}", "PagoProv", p.PagoProveedorId,
                (Cuenta(CxPCod), R(p.Monto), 0),
                (Cuenta(CajaCod), 0, R(p.Monto))));
            resultado.PagosProveedor++;
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
            resultado.Mensajes.Add($"Se generaron {resultado.Total} asiento(s): {resultado.Ventas} de ventas, {resultado.Compras} de compras, {resultado.PagosProveedor} de pagos a proveedor, {resultado.Nomina} de nómina, {resultado.Caja} de caja.");

        return resultado;
    }

    // ----------------------------------------------------- Cierre del ejercicio

    public async Task<CierreEjercicioResultadoDto> CerrarEjercicioAsync(DateTime hasta, int? usuarioId)
    {
        var h = hasta.Date;
        var refCierre = int.Parse(h.ToString("yyyyMMdd"));   // idempotencia: un cierre por fecha de corte

        if (await _db.Asientos.AnyAsync(a => a.Origen == "Cierre" && a.ReferenciaId == refCierre))
            throw new ContabilidadException($"Ya existe un cierre del ejercicio al {h:dd/MM/yyyy}.");

        var cuentas = await _db.CuentasContables.AsNoTracking().ToListAsync();
        var porCodigo = cuentas.ToDictionary(c => c.Codigo, c => c);
        int Cuenta(string codigo)
        {
            if (!porCodigo.TryGetValue(codigo, out var c))
                throw new ContabilidadException($"Falta la cuenta {codigo} en el catálogo.");
            return c.CuentaContableId;
        }

        // Saldos de resultado acumulados (excluye cierres previos para no arrastrarlos)
        var movs = await SaldosPorCuentaAsync(null, h, incluirCierre: false);

        var lineas = new List<(int cuentaId, decimal debito, decimal credito)>();
        decimal totalIngresos = 0, totalCostosGastos = 0;

        foreach (var m in movs.Where(m => m.Tipo == "Ingreso"))
        {
            var saldo = R(m.Credito - m.Debito);   // naturaleza acreedora
            if (saldo == 0) continue;
            lineas.Add((m.CuentaContableId, saldo > 0 ? saldo : 0, saldo < 0 ? -saldo : 0));
            totalIngresos += saldo;
        }
        foreach (var m in movs.Where(m => m.Tipo == "Costo" || m.Tipo == "Gasto"))
        {
            var saldo = R(m.Debito - m.Credito);   // naturaleza deudora
            if (saldo == 0) continue;
            lineas.Add((m.CuentaContableId, saldo < 0 ? -saldo : 0, saldo > 0 ? saldo : 0));
            totalCostosGastos += saldo;
        }

        var resultado = R(totalIngresos - totalCostosGastos);
        if (lineas.Count == 0)
            return new CierreEjercicioResultadoDto { Generado = false, Resultado = 0, Mensaje = "No hay cuentas de resultado con saldo para cerrar." };

        // Traslada el resultado a Resultados Acumulados (Capital)
        lineas.Add((Cuenta(ResultadosAcumCod),
            resultado < 0 ? -resultado : 0,    // pérdida: débito
            resultado > 0 ? resultado : 0));   // utilidad: crédito

        var numero = (await _db.Asientos.MaxAsync(a => (int?)a.Numero) ?? 0) + 1;
        var asiento = new AsientoContable
        {
            Numero = numero,
            Fecha = h,
            Concepto = $"Cierre del ejercicio al {h:dd/MM/yyyy}",
            Origen = "Cierre",
            ReferenciaId = refCierre,
            Estado = "Registrado",
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.Now,
            Detalles = lineas.Select(l => new AsientoDetalle
            {
                CuentaContableId = l.cuentaId,
                Debito = R(l.debito),
                Credito = R(l.credito)
            }).ToList()
        };
        _db.Asientos.Add(asiento);
        await _db.SaveChangesAsync();

        return new CierreEjercicioResultadoDto
        {
            Generado = true,
            AsientoNumero = numero,
            Resultado = resultado,
            Mensaje = resultado >= 0
                ? $"Cierre #{numero}: utilidad de {resultado:N2} trasladada a Resultados Acumulados."
                : $"Cierre #{numero}: pérdida de {-resultado:N2} trasladada a Resultados Acumulados."
        };
    }

    // ---------------------------------------------------- Estado de Resultados

    public async Task<EstadoResultadosDto> EstadoResultadosAsync(DateTime desde, DateTime hasta)
    {
        var d = desde.Date;
        var h = hasta.Date;

        // Excluye los asientos de cierre para que el P&L muestre las cifras reales del período
        var movs = await SaldosPorCuentaAsync(d, h, incluirCierre: false);

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

    private record SaldoCuenta(int CuentaContableId, string Codigo, string Nombre, string Tipo, decimal Debito, decimal Credito);

    /// <summary>Suma débitos y créditos por cuenta de los asientos registrados en el rango.</summary>
    private async Task<List<SaldoCuenta>> SaldosPorCuentaAsync(DateTime? desde, DateTime? hasta, bool incluirCierre = true)
    {
        var q = _db.AsientoDetalles.AsNoTracking()
            .Where(x => x.Asiento.Estado == "Registrado");
        if (!incluirCierre) q = q.Where(x => x.Asiento.Origen != "Cierre");
        if (desde.HasValue) q = q.Where(x => x.Asiento.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Asiento.Fecha <= hasta.Value.Date);

        var movs = await q
            .GroupBy(x => new { x.CuentaContableId, x.Cuenta.Codigo, x.Cuenta.Nombre, x.Cuenta.Tipo })
            .Select(g => new SaldoCuenta(
                g.Key.CuentaContableId, g.Key.Codigo, g.Key.Nombre, g.Key.Tipo,
                g.Sum(x => x.Debito), g.Sum(x => x.Credito)))
            .ToListAsync();

        return movs.OrderBy(m => m.Codigo, StringComparer.Ordinal).ToList();
    }
}
