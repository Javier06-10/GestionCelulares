namespace GestionCelulares.Domain.Entities;

public class Cliente
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
