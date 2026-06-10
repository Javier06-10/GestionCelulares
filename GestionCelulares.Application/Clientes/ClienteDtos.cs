using System.ComponentModel.DataAnnotations;

namespace GestionCelulares.Application.Clientes;

public class ClienteCrearDto
{
    [StringLength(20)] public string? Cedula { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(30)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
}

public class ClienteActualizarDto
{
    [StringLength(20)] public string? Cedula { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(30)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
}

public class ClienteDto
{
    public int ClienteId { get; set; }
    public string? Cedula { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool EsMoroso { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime FechaCreacion { get; set; }
}
