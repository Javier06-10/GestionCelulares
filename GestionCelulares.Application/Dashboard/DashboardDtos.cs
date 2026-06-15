namespace GestionCelulares.Application.Dashboard;

public class DashboardDto
{
    public VentasIndicadorDto VentasHoy { get; set; } = new();
    public VentasIndicadorDto VentasMes { get; set; } = new();
    public InventarioIndicadorDto Inventario { get; set; } = new();
    public TallerIndicadorDto Taller { get; set; } = new();
    public CreditosIndicadorDto Creditos { get; set; } = new();
    public CajaIndicadorDto Caja { get; set; } = new();
    public List<TopProductoDto> TopProductosMes { get; set; } = new();
    public List<TopVendedorDto> TopVendedoresMes { get; set; } = new();
    /// <summary>Serie de ventas de los últimos 14 días (para la gráfica de tendencia).</summary>
    public List<VentaDiaDto> VentasUltimos14Dias { get; set; } = new();
}

public class VentaDiaDto
{
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal Ganancia { get; set; }
}

public class VentasIndicadorDto
{
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
    /// <summary>Venta menos descuento e impuesto, menos el costo de los equipos/accesorios.</summary>
    public decimal Ganancia { get; set; }
}

public class InventarioIndicadorDto
{
    public int EquiposDisponibles { get; set; }
    public int AccesoriosDisponibles { get; set; }
    public decimal ValorCosto { get; set; }
}

public class TallerIndicadorDto
{
    public int Recibidos { get; set; }
    public int EnReparacion { get; set; }
    public int Reparados { get; set; }
}

public class CreditosIndicadorDto
{
    public int Activos { get; set; }
    public int EnMora { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int CuotasVencidas { get; set; }
    public decimal MontoVencido { get; set; }
}

public class CajaIndicadorDto
{
    public bool SesionAbierta { get; set; }
    public int? SesionCajaId { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
}

public class TopProductoDto
{
    public string Producto { get; set; } = null!;
    public int Unidades { get; set; }
    public decimal Total { get; set; }
}

public class TopVendedorDto
{
    public int UsuarioId { get; set; }
    public string Vendedor { get; set; } = null!;
    public int Ventas { get; set; }
    public decimal Total { get; set; }
}
