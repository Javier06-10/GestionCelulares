namespace GestionCelulares.Domain.Entities;

public class Empresa
{
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? RNC { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string Moneda { get; set; } = "DOP";
    public decimal PorcentajeItbis { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class Sucursal
{
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<InventarioImei> Inventario { get; set; } = new List<InventarioImei>();
}
