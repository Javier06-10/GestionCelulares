namespace GestionCelulares.Application.Contabilidad;

/// <summary>Resultado de generar los asientos automáticos de un período.</summary>
public class ContabilizacionResultadoDto
{
    public int Ventas { get; set; }
    public int Nomina { get; set; }
    public int Caja { get; set; }
    public int Total => Ventas + Nomina + Caja;
    public List<string> Mensajes { get; set; } = new();
}

// ---------- Estado de Resultados ----------

public class EstadoResultadosLineaDto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Monto { get; set; }
}

public class EstadoResultadosDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    public List<EstadoResultadosLineaDto> Ingresos { get; set; } = new();
    public List<EstadoResultadosLineaDto> Costos { get; set; } = new();
    public List<EstadoResultadosLineaDto> Gastos { get; set; } = new();

    public decimal TotalIngresos { get; set; }
    public decimal TotalCostos { get; set; }
    public decimal UtilidadBruta { get; set; }     // Ingresos - Costos
    public decimal TotalGastos { get; set; }
    public decimal UtilidadNeta { get; set; }       // Utilidad Bruta - Gastos
}

// ---------- Balance General ----------

public class BalanceGeneralLineaDto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Monto { get; set; }
}

public class BalanceGeneralDto
{
    public DateTime Hasta { get; set; }

    public List<BalanceGeneralLineaDto> Activos { get; set; } = new();
    public List<BalanceGeneralLineaDto> Pasivos { get; set; } = new();
    public List<BalanceGeneralLineaDto> Capital { get; set; } = new();

    public decimal TotalActivos { get; set; }
    public decimal TotalPasivos { get; set; }
    public decimal CapitalAportado { get; set; }      // cuentas de Capital
    public decimal ResultadoEjercicio { get; set; }   // Ingresos - Costos - Gastos acumulados
    public decimal TotalCapital { get; set; }          // CapitalAportado + ResultadoEjercicio
    public decimal TotalPasivoCapital { get; set; }    // Pasivos + TotalCapital
    public bool Cuadrado { get; set; }                  // Activos == Pasivos + Capital
}
