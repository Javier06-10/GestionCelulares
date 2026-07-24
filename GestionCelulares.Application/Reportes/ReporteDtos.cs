namespace GestionCelulares.Application.Reportes;

/// <summary>Reporte de ventas agrupado por día.</summary>
public class ReporteVentasDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public decimal Ganancia { get; set; }
    // Ventas rápidas (productos provisionales) todavía sin costo real: su margen
    // NO está incluido en Ganancia. Se divulga para que el dueño formalice el costo.
    public int LineasSinCosto { get; set; }
    public decimal MontoSinCosto { get; set; }
    public List<VentaDiaDto> PorDia { get; set; } = new();
}

public class VentaDiaDto
{
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
    public decimal Ganancia { get; set; }
}

/// <summary>Stock disponible valorizado por producto y variante.</summary>
public class ReporteInventarioDto
{
    public int Equipos { get; set; }
    public decimal ValorCosto { get; set; }
    public decimal ValorVenta { get; set; }
    public List<InventarioLineaDto> Lineas { get; set; } = new();
}

public class InventarioLineaDto
{
    public string Producto { get; set; } = null!;
    public string? Marca { get; set; }
    public string? Color { get; set; }
    public string? Almacenamiento { get; set; }
    public int SucursalId { get; set; }
    public int Disponibles { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal ValorCosto { get; set; }
}

/// <summary>Morosidad por cliente.</summary>
public class ReporteMorosidadDto
{
    public int ClientesEnMora { get; set; }
    public decimal MontoVencidoTotal { get; set; }
    public List<MorosidadClienteDto> Clientes { get; set; } = new();
}

public class MorosidadClienteDto
{
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public string? Telefono { get; set; }
    public int CuotasVencidas { get; set; }
    public decimal MontoVencido { get; set; }
    public int DiasMaxVencido { get; set; }
}

/// <summary>Sesiones de caja del período con sus diferencias de arqueo.</summary>
public class ReporteCajaDto
{
    public int Sesiones { get; set; }
    public decimal TotalDiferencias { get; set; }
    public List<CajaSesionDto> Detalle { get; set; } = new();
}

public class CajaSesionDto
{
    public int SesionCajaId { get; set; }
    public int SucursalId { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal? MontoCierre { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = null!;
}

/// <summary>Producción del taller en el período.</summary>
public class ReporteTallerDto
{
    public int Entregadas { get; set; }
    public int Canceladas { get; set; }
    public decimal IngresosPorReparacion { get; set; }
    public decimal CostoRepuestos { get; set; }
    public decimal ComisionesTecnicos { get; set; }
    public List<TallerTecnicoDto> PorTecnico { get; set; } = new();
}

public class TallerTecnicoDto
{
    public int TecnicoId { get; set; }
    public string Tecnico { get; set; } = null!;
    public int OrdenesEntregadas { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Comision { get; set; }
}

/// <summary>Cobros de cuotas de crédito en el período.</summary>
public class ReporteCobrosDto
{
    public int Pagos { get; set; }
    public decimal TotalCobrado { get; set; }
    public List<CobroDiaDto> PorDia { get; set; } = new();
}

public class CobroDiaDto
{
    public DateTime Fecha { get; set; }
    public int Pagos { get; set; }
    public decimal Total { get; set; }
}
