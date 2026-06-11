using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Creditos;

public interface ICreditoService
{
    Task<CreditoDto> CrearAsync(CreditoCrearDto dto);
    Task<CreditoDto?> PorIdAsync(int id);
    Task<IReadOnlyList<CreditoResumenDto>> BuscarAsync(int? clienteId, string? estado);
    Task<AbonoResultadoDto> RegistrarAbonoAsync(int creditoId, AbonoRegistroDto dto, int? usuarioId);
    Task<IReadOnlyList<PagoCreditoDto>> AbonosAsync(int creditoId);
    Task<IReadOnlyList<CuotaVencidaDto>> CuotasVencidasAsync();
    Task<MoraResultadoDto> ActualizarMoraAsync(DateTime? fechaCorte);
}

public class CreditoService : ICreditoService
{
    private readonly IApplicationDbContext _db;
    private readonly ICreditoProcedures _procedures;

    public CreditoService(IApplicationDbContext db, ICreditoProcedures procedures)
    {
        _db = db;
        _procedures = procedures;
    }

    public async Task<CreditoDto> CrearAsync(CreditoCrearDto dto)
    {
        var cliente = await _db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId)
            ?? throw new CreditoException("El cliente indicado no existe.");

        if (cliente.Bloqueado)
            throw new CreditoException($"El cliente '{cliente.Nombre}' está bloqueado y no puede recibir financiamiento.");

        if (dto.FechaPrimerVencimiento.Date < DateTime.Today)
            throw new CreditoException("La fecha del primer vencimiento no puede estar en el pasado.");

        if (dto.VentaId.HasValue)
        {
            var venta = await _db.Ventas.AsNoTracking()
                .FirstOrDefaultAsync(v => v.VentaId == dto.VentaId.Value)
                ?? throw new CreditoException("La venta indicada no existe.");

            if (!venta.EsCredito)
                throw new CreditoException("La venta indicada no es a crédito.");

            if (await _db.Creditos.AnyAsync(c => c.VentaId == dto.VentaId.Value))
                throw new CreditoException("La venta ya tiene un crédito asociado.");
        }

        // usp_Credito_Crear genera el crédito y su tabla de cuotas en una transacción
        var creditoId = await _procedures.CrearAsync(dto);

        return (await PorIdAsync(creditoId))!;
    }

    public async Task<CreditoDto?> PorIdAsync(int id)
        => await _db.Creditos.AsNoTracking()
            .Where(c => c.CreditoId == id)
            .Select(ProyeccionCredito)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<CreditoResumenDto>> BuscarAsync(int? clienteId, string? estado)
    {
        var q = _db.Creditos.AsNoTracking();

        if (clienteId.HasValue)
            q = q.Where(c => c.ClienteId == clienteId.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(c => c.Estado == estado.Trim());

        return await q.OrderByDescending(c => c.FechaInicio)
            .Select(c => new CreditoResumenDto
            {
                CreditoId = c.CreditoId,
                ClienteId = c.ClienteId,
                Cliente = c.Cliente.Nombre,
                MontoTotal = c.MontoTotal,
                Saldo = c.Saldo,
                NumeroCuotas = c.NumeroCuotas,
                Estado = c.Estado,
                FechaInicio = c.FechaInicio
            })
            .ToListAsync();
    }

    public async Task<AbonoResultadoDto> RegistrarAbonoAsync(int creditoId, AbonoRegistroDto dto, int? usuarioId)
    {
        if (!await _db.Creditos.AnyAsync(c => c.CreditoId == creditoId))
            throw new CreditoException("El crédito no existe.");

        if (dto.MetodoPagoId.HasValue &&
            !await _db.MetodosPago.AnyAsync(m => m.MetodoPagoId == dto.MetodoPagoId.Value))
            throw new CreditoException("El método de pago indicado no existe.");

        if (dto.SesionCajaId.HasValue &&
            !await _db.SesionesCaja.AnyAsync(s => s.SesionCajaId == dto.SesionCajaId.Value && s.Estado == "Abierta"))
            throw new CreditoException("La sesión de caja indicada no existe o está cerrada.");

        // usp_PagoCredito_Registrar distribuye el abono (cuota más antigua primero) y actualiza estados
        var pagoId = await _procedures.RegistrarAbonoAsync(creditoId, dto, usuarioId);

        var credito = await _db.Creditos.AsNoTracking().FirstAsync(c => c.CreditoId == creditoId);
        return new AbonoResultadoDto
        {
            PagoCreditoId = pagoId,
            CreditoId = creditoId,
            MontoAbonado = dto.Monto,
            SaldoCredito = credito.Saldo,
            EstadoCredito = credito.Estado
        };
    }

    public async Task<IReadOnlyList<PagoCreditoDto>> AbonosAsync(int creditoId)
    {
        if (!await _db.Creditos.AnyAsync(c => c.CreditoId == creditoId))
            throw new CreditoException("El crédito no existe.");

        return await _db.PagosCredito.AsNoTracking()
            .Where(p => p.CreditoId == creditoId)
            .OrderBy(p => p.Fecha)
            .Select(p => new PagoCreditoDto
            {
                PagoCreditoId = p.PagoCreditoId,
                CreditoId = p.CreditoId,
                CuotaId = p.CuotaId,
                MetodoPago = p.MetodoPago == null ? null : p.MetodoPago.Nombre,
                Monto = p.Monto,
                NumeroRecibo = p.NumeroRecibo,
                Fecha = p.Fecha
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CuotaVencidaDto>> CuotasVencidasAsync()
        => await _db.CuotasVencidas.AsNoTracking()
            .OrderByDescending(c => c.DiasVencido)
            .ToListAsync();

    public Task<MoraResultadoDto> ActualizarMoraAsync(DateTime? fechaCorte)
        => _procedures.ActualizarMoraAsync(fechaCorte);

    private static readonly Expression<Func<Credito, CreditoDto>> ProyeccionCredito = c => new CreditoDto
    {
        CreditoId = c.CreditoId,
        VentaId = c.VentaId,
        ClienteId = c.ClienteId,
        Cliente = c.Cliente.Nombre,
        MontoFinanciado = c.MontoFinanciado,
        Inicial = c.Inicial,
        TasaInteres = c.TasaInteres,
        NumeroCuotas = c.NumeroCuotas,
        MontoTotal = c.MontoTotal,
        Saldo = c.Saldo,
        Estado = c.Estado,
        FechaInicio = c.FechaInicio,
        Cuotas = c.Cuotas.OrderBy(q => q.NumeroCuota).Select(q => new CuotaDto
        {
            CuotaId = q.CuotaId,
            NumeroCuota = q.NumeroCuota,
            FechaVencimiento = q.FechaVencimiento,
            MontoCuota = q.MontoCuota,
            MontoPagado = q.MontoPagado,
            Saldo = q.Saldo,
            Estado = q.Estado
        }).ToList()
    };
}
