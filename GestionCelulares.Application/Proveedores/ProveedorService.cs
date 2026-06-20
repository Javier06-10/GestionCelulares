using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Proveedores;

public interface IProveedorService
{
    Task<IReadOnlyList<ProveedorDto>> BuscarAsync(string? termino, bool? activos);
    Task<ProveedorDto?> PorIdAsync(int id);
    Task<ProveedorDto> CrearAsync(ProveedorCrearDto dto);
    Task<ProveedorDto> ActualizarAsync(int id, ProveedorActualizarDto dto);
    Task<IReadOnlyList<CompraDto>> ComprasAsync(int proveedorId);
    Task<CompraDto> RegistrarCompraAsync(int proveedorId, CompraRegistroDto dto);
    Task<IReadOnlyList<PagoProveedorDto>> PagosAsync(int proveedorId);
    Task<PagoProveedorDto> RegistrarPagoAsync(int proveedorId, PagoProveedorRegistroDto dto);
}

public class ProveedorService : IProveedorService
{
    private readonly IApplicationDbContext _db;

    public ProveedorService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProveedorDto>> BuscarAsync(string? termino, bool? activos)
    {
        var q = _db.Proveedores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            termino = termino.Trim();
            q = q.Where(p =>
                p.Nombre.Contains(termino) ||
                (p.RNC != null && p.RNC.Contains(termino)) ||
                (p.Telefono != null && p.Telefono.Contains(termino)));
        }

        if (activos.HasValue)
            q = q.Where(p => p.Activo == activos.Value);

        return await q.OrderBy(p => p.Nombre).Select(p => Proyectar(p)).ToListAsync();
    }

    public async Task<ProveedorDto?> PorIdAsync(int id)
    {
        var proveedor = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.ProveedorId == id);
        return proveedor is null ? null : Proyectar(proveedor);
    }

    public async Task<ProveedorDto> CrearAsync(ProveedorCrearDto dto)
    {
        var proveedor = new Proveedor
        {
            Nombre = dto.Nombre.Trim(),
            RNC = Normalizar(dto.RNC),
            Telefono = Normalizar(dto.Telefono),
            Email = Normalizar(dto.Email),
            Direccion = Normalizar(dto.Direccion),
            Balance = 0,
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        return Proyectar(proveedor);
    }

    public async Task<ProveedorDto> ActualizarAsync(int id, ProveedorActualizarDto dto)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.ProveedorId == id)
            ?? throw new ProveedorException("El proveedor no existe.");

        proveedor.Nombre = dto.Nombre.Trim();
        proveedor.RNC = Normalizar(dto.RNC);
        proveedor.Telefono = Normalizar(dto.Telefono);
        proveedor.Email = Normalizar(dto.Email);
        proveedor.Direccion = Normalizar(dto.Direccion);
        proveedor.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return Proyectar(proveedor);
    }

    public async Task<IReadOnlyList<CompraDto>> ComprasAsync(int proveedorId)
    {
        await ExigirProveedorAsync(proveedorId);

        return await _db.Compras.AsNoTracking()
            .Where(c => c.ProveedorId == proveedorId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new CompraDto
            {
                CompraId = c.CompraId,
                ProveedorId = c.ProveedorId,
                SucursalId = c.SucursalId,
                NumeroFactura = c.NumeroFactura,
                Fecha = c.Fecha,
                Total = c.Total,
                Itbis = c.Itbis,
                Notas = c.Notas,
                Contado = _db.PagosProveedor.Any(p => p.CompraId == c.CompraId)
            })
            .ToListAsync();
    }

    public async Task<CompraDto> RegistrarCompraAsync(int proveedorId, CompraRegistroDto dto)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.ProveedorId == proveedorId)
            ?? throw new ProveedorException("El proveedor no existe.");

        if (!await _db.Sucursales.AnyAsync(s => s.SucursalId == dto.SucursalId))
            throw new ProveedorException("La sucursal indicada no existe.");

        // El método de pago solo aplica a compras al contado
        int? metodoPagoId = null;
        if (dto.Contado && dto.MetodoPagoId.HasValue)
        {
            if (!await _db.MetodosPago.AnyAsync(m => m.MetodoPagoId == dto.MetodoPagoId.Value))
                throw new ProveedorException("El método de pago indicado no existe.");
            metodoPagoId = dto.MetodoPagoId;
        }

        var itbis = Math.Min(dto.Itbis, dto.Total);
        var compra = new Compra
        {
            ProveedorId = proveedorId,
            SucursalId = dto.SucursalId,
            NumeroFactura = Normalizar(dto.NumeroFactura),
            Fecha = DateTime.Now,
            Total = dto.Total,
            Subtotal = dto.Total - itbis,
            Itbis = itbis,
            TipoBienServicio = string.IsNullOrWhiteSpace(dto.TipoBienServicio) ? "09" : dto.TipoBienServicio.Trim(),
            MetodoPagoId = metodoPagoId,
            Notas = Normalizar(dto.Notas)
        };
        _db.Compras.Add(compra);

        if (dto.Contado)
        {
            // Compra al contado: se paga completa de inmediato, el balance no se afecta
            // (la compra y el pago se netean). Se deja registro del pago vinculado a la compra.
            _db.PagosProveedor.Add(new PagoProveedor
            {
                Proveedor = proveedor,
                Compra = compra,
                Monto = dto.Total,
                Fecha = compra.Fecha,
                Referencia = "Pago al contado"
            });
        }
        else
        {
            // La compra a crédito aumenta lo adeudado al proveedor
            proveedor.Balance += dto.Total;
        }

        await _db.SaveChangesAsync();

        return new CompraDto
        {
            CompraId = compra.CompraId,
            ProveedorId = compra.ProveedorId,
            SucursalId = compra.SucursalId,
            NumeroFactura = compra.NumeroFactura,
            Fecha = compra.Fecha,
            Total = compra.Total,
            Notas = compra.Notas,
            Contado = dto.Contado
        };
    }

    public async Task<IReadOnlyList<PagoProveedorDto>> PagosAsync(int proveedorId)
    {
        await ExigirProveedorAsync(proveedorId);

        return await _db.PagosProveedor.AsNoTracking()
            .Where(p => p.ProveedorId == proveedorId)
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PagoProveedorDto
            {
                PagoProveedorId = p.PagoProveedorId,
                ProveedorId = p.ProveedorId,
                CompraId = p.CompraId,
                Monto = p.Monto,
                Fecha = p.Fecha,
                Referencia = p.Referencia
            })
            .ToListAsync();
    }

    public async Task<PagoProveedorDto> RegistrarPagoAsync(int proveedorId, PagoProveedorRegistroDto dto)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.ProveedorId == proveedorId)
            ?? throw new ProveedorException("El proveedor no existe.");

        if (dto.Monto > proveedor.Balance)
            throw new ProveedorException($"El pago ({dto.Monto}) excede el balance adeudado ({proveedor.Balance}).");

        if (dto.CompraId.HasValue &&
            !await _db.Compras.AnyAsync(c => c.CompraId == dto.CompraId.Value && c.ProveedorId == proveedorId))
            throw new ProveedorException("La compra indicada no existe o no pertenece a este proveedor.");

        var pago = new PagoProveedor
        {
            ProveedorId = proveedorId,
            CompraId = dto.CompraId,
            Monto = dto.Monto,
            Fecha = DateTime.Now,
            Referencia = Normalizar(dto.Referencia)
        };
        _db.PagosProveedor.Add(pago);

        proveedor.Balance -= dto.Monto;
        await _db.SaveChangesAsync();

        return new PagoProveedorDto
        {
            PagoProveedorId = pago.PagoProveedorId,
            ProveedorId = pago.ProveedorId,
            CompraId = pago.CompraId,
            Monto = pago.Monto,
            Fecha = pago.Fecha,
            Referencia = pago.Referencia,
            BalanceProveedor = proveedor.Balance
        };
    }

    private async Task ExigirProveedorAsync(int proveedorId)
    {
        if (!await _db.Proveedores.AnyAsync(p => p.ProveedorId == proveedorId))
            throw new ProveedorException("El proveedor no existe.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ProveedorDto Proyectar(Proveedor p) => new()
    {
        ProveedorId = p.ProveedorId,
        Nombre = p.Nombre,
        RNC = p.RNC,
        Telefono = p.Telefono,
        Email = p.Email,
        Direccion = p.Direccion,
        Balance = p.Balance,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion
    };
}
