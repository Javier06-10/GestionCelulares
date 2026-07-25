using System.Linq.Expressions;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Apartados;

public interface IApartadoService
{
    Task<IReadOnlyList<EquipoApartableDto>> EquiposApartablesAsync(int? sucursalId, string? termino);
    Task<IReadOnlyList<ApartadoResumenDto>> ListarAsync(string? estado, int? clienteId);
    Task<ApartadoDto?> PorIdAsync(int id);
    Task<ApartadoDto> CrearAsync(ApartadoCrearDto dto, int? usuarioId);
    Task<ApartadoDto> AbonarAsync(int id, AbonoApartadoCrearDto dto, int? usuarioId);
    Task<ApartadoDto> CambiarEquipoAsync(int id, CambiarEquipoDto dto);
    Task<ApartadoDto> CompletarAsync(int id, int usuarioId);
    Task<ApartadoDto> CancelarAsync(int id, CancelarApartadoDto dto, int? usuarioId);
}

public class ApartadoService : IApartadoService
{
    private readonly IApplicationDbContext _db;
    private readonly IVentaProcedures _ventas;

    public ApartadoService(IApplicationDbContext db, IVentaProcedures ventas)
    {
        _db = db;
        _ventas = ventas;
    }

    public async Task<IReadOnlyList<EquipoApartableDto>> EquiposApartablesAsync(int? sucursalId, string? termino)
    {
        var q = _db.InventarioImeis.AsNoTracking().Where(i => i.Estado == "Disponible");
        if (sucursalId.HasValue) q = q.Where(i => i.SucursalId == sucursalId.Value);
        if (!string.IsNullOrWhiteSpace(termino))
        {
            termino = termino.Trim();
            q = q.Where(i => i.Imei.Contains(termino) || i.Variante.Producto.Nombre.Contains(termino));
        }

        return await q.OrderBy(i => i.Variante.Producto.Nombre)
            .Select(i => new EquipoApartableDto
            {
                ImeiId = i.ImeiId,
                Imei = i.Imei,
                VarianteId = i.VarianteId,
                Producto = i.Variante.Producto.Nombre,
                Marca = i.Variante.Producto.Marca!.Nombre,
                Variante = string.Join(" · ", new[] { i.Variante.Color, i.Variante.Almacenamiento, i.Variante.Condicion }.Where(x => x != null)),
                PrecioVenta = i.Variante.PrecioVenta
            })
            .Take(50)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ApartadoResumenDto>> ListarAsync(string? estado, int? clienteId)
    {
        var q = _db.Apartados.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(a => a.Estado == estado.Trim());
        if (clienteId.HasValue) q = q.Where(a => a.ClienteId == clienteId.Value);

        return await q.OrderByDescending(a => a.FechaInicio)
            .Select(a => new ApartadoResumenDto
            {
                ApartadoId = a.ApartadoId,
                Cliente = a.Cliente.Nombre,
                Equipo = a.Variante.Producto.Nombre,
                PrecioTotal = a.PrecioTotal,
                TotalAbonado = a.TotalAbonado,
                Saldo = a.PrecioTotal - a.TotalAbonado,
                Estado = a.Estado,
                FechaInicio = a.FechaInicio
            })
            .ToListAsync();
    }

    public async Task<ApartadoDto?> PorIdAsync(int id)
        => await _db.Apartados.AsNoTracking().Where(a => a.ApartadoId == id).Select(Proyeccion).FirstOrDefaultAsync();

    public async Task<ApartadoDto> CrearAsync(ApartadoCrearDto dto, int? usuarioId)
    {
        if (!await _db.Clientes.AnyAsync(c => c.ClienteId == dto.ClienteId))
            throw new ApartadoException("El cliente indicado no existe.");

        var imei = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == dto.ImeiId)
            ?? throw new ApartadoException("El equipo indicado no existe.");
        if (imei.Estado != "Disponible")
            throw new ApartadoException("El equipo no está disponible para apartar.");

        if (dto.AbonoInicial > dto.PrecioTotal)
            throw new ApartadoException("El abono inicial no puede superar el precio total.");

        var apartado = new Apartado
        {
            ClienteId = dto.ClienteId,
            ImeiId = imei.ImeiId,
            VarianteId = imei.VarianteId,
            SucursalId = imei.SucursalId,
            UsuarioId = usuarioId,
            PrecioTotal = dto.PrecioTotal,
            TotalAbonado = 0,
            Estado = "Activo",
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            FechaInicio = DateTime.Now
        };

        // Reserva del equipo
        imei.Estado = "Apartado";
        _db.Apartados.Add(apartado);

        // El abono inicial (si lo hay) va en la MISMA operación: apartado + reserva + abono
        // se guardan atómicamente en un solo SaveChanges (si algo falla, no persiste nada).
        if (dto.AbonoInicial > 0)
            await PrepararAbonoAsync(apartado, dto.AbonoInicial, dto.MetodoPagoId, usuarioId);

        await _db.SaveChangesAsync();

        return (await PorIdAsync(apartado.ApartadoId))!;
    }

    public async Task<ApartadoDto> AbonarAsync(int id, AbonoApartadoCrearDto dto, int? usuarioId)
    {
        var apartado = await _db.Apartados.FirstOrDefaultAsync(a => a.ApartadoId == id)
            ?? throw new ApartadoException("El apartado no existe.");
        if (apartado.Estado != "Activo")
            throw new ApartadoException("El apartado no está activo.");

        var saldo = apartado.PrecioTotal - apartado.TotalAbonado;
        if (dto.Monto > saldo)
            throw new ApartadoException($"El abono ({dto.Monto:N2}) excede el saldo pendiente ({saldo:N2}).");

        await PrepararAbonoAsync(apartado, dto.Monto, dto.MetodoPagoId, usuarioId);
        await _db.SaveChangesAsync();
        return (await PorIdAsync(id))!;
    }

    // Prepara (sin guardar) el abono en la colección del apartado, para que el llamador haga
    // UN solo SaveChanges y la operación sea atómica. Valida el método de pago y la caja abierta.
    private async Task PrepararAbonoAsync(Apartado apartado, decimal monto, int? metodoPagoId, int? usuarioId)
    {
        if (metodoPagoId is null)
            throw new ApartadoException("Indica el método de pago del abono.");
        if (!await _db.MetodosPago.AnyAsync(m => m.MetodoPagoId == metodoPagoId.Value))
            throw new ApartadoException("El método de pago no existe.");

        var sesionCajaId = await SesionAbiertaAsync(apartado.SucursalId)
            ?? throw new ApartadoException("No hay una caja abierta en esta sucursal para registrar el abono.");

        // Se agrega vía la navegación (no _db.AbonosApartado.Add) para que EF resuelva el FK
        // incluso cuando el apartado aún no tiene Id (alta con abono inicial en un solo save).
        apartado.Abonos.Add(new AbonoApartado
        {
            Monto = monto,
            MetodoPagoId = metodoPagoId,
            SesionCajaId = sesionCajaId,
            UsuarioId = usuarioId,
            Tipo = "Abono",
            Fecha = DateTime.Now
        });
        apartado.TotalAbonado += monto;
    }

    public async Task<ApartadoDto> CambiarEquipoAsync(int id, CambiarEquipoDto dto)
    {
        var apartado = await _db.Apartados.FirstOrDefaultAsync(a => a.ApartadoId == id)
            ?? throw new ApartadoException("El apartado no existe.");
        if (apartado.Estado != "Activo")
            throw new ApartadoException("Solo se puede cambiar el equipo de un apartado activo.");

        var nuevo = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == dto.ImeiId)
            ?? throw new ApartadoException("El equipo indicado no existe.");
        if (nuevo.Estado != "Disponible")
            throw new ApartadoException("El equipo seleccionado no está disponible.");
        if (nuevo.SucursalId != apartado.SucursalId)
            throw new ApartadoException("El equipo es de otra sucursal.");

        // Libera el equipo anterior y reserva el nuevo
        if (apartado.ImeiId.HasValue)
        {
            var anterior = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == apartado.ImeiId.Value);
            if (anterior is not null) anterior.Estado = "Disponible";
        }
        nuevo.Estado = "Apartado";
        apartado.ImeiId = nuevo.ImeiId;
        apartado.VarianteId = nuevo.VarianteId;
        apartado.PrecioTotal = dto.PrecioTotal;
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    public async Task<ApartadoDto> CompletarAsync(int id, int usuarioId)
    {
        var apartado = await _db.Apartados.FirstOrDefaultAsync(a => a.ApartadoId == id)
            ?? throw new ApartadoException("El apartado no existe.");
        if (apartado.Estado != "Activo")
            throw new ApartadoException("El apartado no está activo.");
        if (apartado.TotalAbonado < apartado.PrecioTotal)
            throw new ApartadoException($"Aún queda saldo pendiente ({apartado.PrecioTotal - apartado.TotalAbonado:N2}). No se puede entregar.");
        if (apartado.ImeiId is null)
            throw new ApartadoException("El apartado no tiene un equipo asignado.");

        var sesionCajaId = await SesionAbiertaAsync(apartado.SucursalId)
            ?? throw new ApartadoException("No hay una caja abierta para registrar la entrega.");

        var metodoApartado = await _db.MetodosPago.Where(m => m.Nombre == "Apartado")
            .Select(m => (int?)m.MetodoPagoId).FirstOrDefaultAsync()
            ?? throw new ApartadoException("Falta el método de pago 'Apartado' en la configuración.");

        var itbis = await _db.Empresas.Select(e => e.PorcentajeItbis).FirstOrDefaultAsync();
        var tasa = itbis > 1 ? itbis / 100m : itbis;                 // 18 -> 0.18
        var precioSinImpuesto = Math.Round(apartado.PrecioTotal / (1 + tasa), 2);

        var imei = await _db.InventarioImeis.FirstAsync(i => i.ImeiId == apartado.ImeiId.Value);
        // El SP exige el IMEI disponible para venderlo; al venderse lo marca como vendido.
        imei.Estado = "Disponible";
        await _db.SaveChangesAsync();

        try
        {
            var ventaDto = new VentaRegistroDto
            {
                SucursalId = apartado.SucursalId,
                ClienteId = apartado.ClienteId,
                EsCredito = false,
                MetodoPagoId = metodoApartado,
                NumeroFactura = await _ventas.ProximoNumeroFacturaAsync(),
                Detalles = new List<VentaDetalleRegistroDto>
                {
                    new() { ImeiId = imei.ImeiId, VarianteId = apartado.VarianteId, Cantidad = 1, PrecioUnitario = precioSinImpuesto, Descuento = 0 }
                }
            };
            var ventaId = await _ventas.RegistrarAsync(ventaDto, usuarioId, sesionCajaId);

            apartado.VentaId = ventaId;
            apartado.Estado = "Completado";
            apartado.FechaCierre = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Revertir la reserva si la venta falla
            imei.Estado = "Apartado";
            await _db.SaveChangesAsync();
            throw;
        }

        return (await PorIdAsync(id))!;
    }

    public async Task<ApartadoDto> CancelarAsync(int id, CancelarApartadoDto dto, int? usuarioId)
    {
        var apartado = await _db.Apartados.FirstOrDefaultAsync(a => a.ApartadoId == id)
            ?? throw new ApartadoException("El apartado no existe.");
        if (apartado.Estado != "Activo")
            throw new ApartadoException("El apartado no está activo.");

        // Liberar siempre el equipo
        if (apartado.ImeiId.HasValue)
        {
            var imei = await _db.InventarioImeis.FirstOrDefaultAsync(i => i.ImeiId == apartado.ImeiId.Value);
            if (imei is not null && imei.Estado == "Apartado") imei.Estado = "Disponible";
        }

        // Devolución de dinero (opcional, solo administrador): egreso de caja
        if (dto.DevolverMonto > 0)
        {
            if (dto.DevolverMonto > apartado.TotalAbonado)
                throw new ApartadoException("La devolución no puede superar lo abonado.");

            var sesionCajaId = await SesionAbiertaAsync(apartado.SucursalId)
                ?? throw new ApartadoException("No hay una caja abierta para registrar la devolución.");

            _db.MovimientosCaja.Add(new MovimientoCaja
            {
                SesionCajaId = sesionCajaId,
                Tipo = "Egreso",
                Concepto = $"Devolución apartado #{apartado.ApartadoId}",
                Monto = dto.DevolverMonto,
                Referencia = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim(),
                Fecha = DateTime.Now
            });
            _db.AbonosApartado.Add(new AbonoApartado
            {
                ApartadoId = apartado.ApartadoId,
                Monto = dto.DevolverMonto,
                SesionCajaId = sesionCajaId,
                UsuarioId = usuarioId,
                Tipo = "Devolucion",
                Fecha = DateTime.Now
            });
        }

        apartado.Estado = "Cancelado";
        apartado.FechaCierre = DateTime.Now;
        await _db.SaveChangesAsync();

        return (await PorIdAsync(id))!;
    }

    private Task<int?> SesionAbiertaAsync(int sucursalId)
        => _db.SesionesCaja.Where(s => s.SucursalId == sucursalId && s.Estado == "Abierta")
            .Select(s => (int?)s.SesionCajaId).FirstOrDefaultAsync();

    private static readonly Expression<Func<Apartado, ApartadoDto>> Proyeccion = a => new ApartadoDto
    {
        ApartadoId = a.ApartadoId,
        ClienteId = a.ClienteId,
        Cliente = a.Cliente.Nombre,
        ImeiId = a.ImeiId,
        Imei = a.Imei == null ? null : a.Imei.Imei,
        VarianteId = a.VarianteId,
        Equipo = a.Variante.Producto.Nombre,
        PrecioTotal = a.PrecioTotal,
        TotalAbonado = a.TotalAbonado,
        Saldo = a.PrecioTotal - a.TotalAbonado,
        Estado = a.Estado,
        VentaId = a.VentaId,
        Notas = a.Notas,
        FechaInicio = a.FechaInicio,
        FechaCierre = a.FechaCierre,
        Abonos = a.Abonos.OrderBy(b => b.Fecha).Select(b => new AbonoApartadoDto
        {
            AbonoApartadoId = b.AbonoApartadoId,
            Monto = b.Monto,
            MetodoPago = b.MetodoPago == null ? null : b.MetodoPago.Nombre,
            Tipo = b.Tipo,
            Fecha = b.Fecha
        }).ToList()
    };
}
