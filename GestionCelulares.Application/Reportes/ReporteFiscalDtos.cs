namespace GestionCelulares.Application.Reportes;

/// <summary>Reporte 607 (Ventas de Bienes y Servicios) en formato TXT de la DGII.</summary>
public class Reporte607Dto
{
    public string Periodo { get; set; } = null!;          // YYYYMM
    public string Rnc { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal TotalMontoFacturado { get; set; }       // base sin ITBIS
    public decimal TotalItbis { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string ContenidoTxt { get; set; } = null!;      // archivo completo (cabecera + detalle)
    /// <summary>Comprobantes sin NCF válido (mientras no exista e-CF formal).</summary>
    public int SinComprobante { get; set; }
}

/// <summary>Reporte 606 (Compras de Bienes y Servicios) en formato TXT de la DGII.</summary>
public class Reporte606Dto
{
    public string Periodo { get; set; } = null!;          // YYYYMM
    public string Rnc { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal TotalMontoFacturado { get; set; }       // base sin ITBIS
    public decimal TotalItbis { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string ContenidoTxt { get; set; } = null!;
    /// <summary>Compras sin RNC del proveedor (no válidas para el 606).</summary>
    public int SinRnc { get; set; }
}

/// <summary>Reporte 608 (Comprobantes Anulados) en formato TXT de la DGII.</summary>
public class Reporte608Dto
{
    public string Periodo { get; set; } = null!;          // YYYYMM
    public string Rnc { get; set; } = null!;
    public int Cantidad { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string ContenidoTxt { get; set; } = null!;
}
