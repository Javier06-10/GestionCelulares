using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCelulares.Application.Clientes;

public interface IClienteService
{
    Task<ResultadoPaginado<ClienteDto>> BuscarAsync(string? termino, bool? morosos, bool? bloqueados, int pagina = 1, int tamano = 50);
    Task<ClienteDto?> PorIdAsync(int id);
    Task<ClienteDto> CrearAsync(ClienteCrearDto dto);
    Task<ClienteDto> ActualizarAsync(int id, ClienteActualizarDto dto);
    Task BloquearAsync(int id, bool bloqueado);
}

public class ClienteService : IClienteService
{
    private readonly IApplicationDbContext _db;

    public ClienteService(IApplicationDbContext db) => _db = db;

    public async Task<ResultadoPaginado<ClienteDto>> BuscarAsync(string? termino, bool? morosos, bool? bloqueados, int pagina = 1, int tamano = 50)
    {
        var q = _db.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            termino = termino.Trim();
            q = q.Where(c =>
                c.Nombre.Contains(termino) ||
                (c.Cedula != null && c.Cedula.Contains(termino)) ||
                (c.Telefono != null && c.Telefono.Contains(termino)));
        }

        if (morosos.HasValue)
            q = q.Where(c => c.EsMoroso == morosos.Value);

        if (bloqueados.HasValue)
            q = q.Where(c => c.Bloqueado == bloqueados.Value);

        // Proyección inline (no el método Proyectar) para que EF traduzca Skip/Take/Count a SQL.
        var proyeccion = q.OrderBy(c => c.Nombre).Select(c => new ClienteDto
        {
            ClienteId = c.ClienteId,
            Cedula = c.Cedula,
            Nombre = c.Nombre,
            Telefono = c.Telefono,
            Email = c.Email,
            Direccion = c.Direccion,
            EsMoroso = c.EsMoroso,
            Bloqueado = c.Bloqueado,
            FechaCreacion = c.FechaCreacion
        });
        return await ResultadoPaginado<ClienteDto>.DesdeConsultaAsync(proyeccion, pagina, tamano);
    }

    public async Task<ClienteDto?> PorIdAsync(int id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.ClienteId == id);
        return cliente is null ? null : Proyectar(cliente);
    }

    public async Task<ClienteDto> CrearAsync(ClienteCrearDto dto)
    {
        await ValidarCedulaUnicaAsync(dto.Cedula, clienteId: null);

        var cliente = new Cliente
        {
            Cedula = Normalizar(dto.Cedula),
            Nombre = dto.Nombre.Trim(),
            Telefono = Normalizar(dto.Telefono),
            Email = Normalizar(dto.Email),
            Direccion = Normalizar(dto.Direccion),
            EsMoroso = false,
            Bloqueado = false,
            FechaCreacion = DateTime.Now
        };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        return Proyectar(cliente);
    }

    public async Task<ClienteDto> ActualizarAsync(int id, ClienteActualizarDto dto)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == id)
            ?? throw new ClienteException("El cliente no existe.");

        await ValidarCedulaUnicaAsync(dto.Cedula, cliente.ClienteId);

        cliente.Cedula = Normalizar(dto.Cedula);
        cliente.Nombre = dto.Nombre.Trim();
        cliente.Telefono = Normalizar(dto.Telefono);
        cliente.Email = Normalizar(dto.Email);
        cliente.Direccion = Normalizar(dto.Direccion);
        await _db.SaveChangesAsync();

        return Proyectar(cliente);
    }

    public async Task BloquearAsync(int id, bool bloqueado)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == id)
            ?? throw new ClienteException("El cliente no existe.");

        cliente.Bloqueado = bloqueado;
        await _db.SaveChangesAsync();
    }

    private async Task ValidarCedulaUnicaAsync(string? cedula, int? clienteId)
    {
        cedula = Normalizar(cedula);
        if (cedula is null)
            return;

        var duplicada = await _db.Clientes
            .AnyAsync(c => c.Cedula == cedula && (clienteId == null || c.ClienteId != clienteId));
        if (duplicada)
            throw new ClienteException($"Ya existe un cliente con la cédula {cedula}.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ClienteDto Proyectar(Cliente c) => new()
    {
        ClienteId = c.ClienteId,
        Cedula = c.Cedula,
        Nombre = c.Nombre,
        Telefono = c.Telefono,
        Email = c.Email,
        Direccion = c.Direccion,
        EsMoroso = c.EsMoroso,
        Bloqueado = c.Bloqueado,
        FechaCreacion = c.FechaCreacion
    };
}
