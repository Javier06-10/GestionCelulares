namespace GestionCelulares.Domain.Entities;

/// <summary>Contribuyente del padrón de RNC de la DGII (consulta offline).</summary>
public class PadronRnc
{
    public string Rnc { get; set; } = null!;   // RNC/Cédula con ceros a la izquierda (11 dígitos)
    public string Nombre { get; set; } = null!;
    public string? Estado { get; set; }
}
