namespace GestionCelulares.Application.Common;

/// <summary>Base de las violaciones de reglas de negocio. El handler global las
/// traduce a HTTP 400 con un cuerpo de error consistente.</summary>
public abstract class DominioException : Exception
{
    protected DominioException(string message) : base(message) { }
}

/// <summary>Credenciales o token inválidos -> HTTP 401.</summary>
public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}

/// <summary>Regla de negocio de inventario violada.</summary>
public class InventarioException : DominioException
{
    public InventarioException(string message) : base(message) { }
}

/// <summary>Regla de negocio de clientes violada.</summary>
public class ClienteException : DominioException
{
    public ClienteException(string message) : base(message) { }
}

/// <summary>Regla de negocio de proveedores violada.</summary>
public class ProveedorException : DominioException
{
    public ProveedorException(string message) : base(message) { }
}

/// <summary>Regla de negocio del catálogo violada.</summary>
public class CatalogoException : DominioException
{
    public CatalogoException(string message) : base(message) { }
}

/// <summary>Regla de negocio de caja violada.</summary>
public class CajaException : DominioException
{
    public CajaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de ventas violada.</summary>
public class VentaException : DominioException
{
    public VentaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de créditos violada.</summary>
public class CreditoException : DominioException
{
    public CreditoException(string message) : base(message) { }
}

/// <summary>Regla de negocio de gestión de usuarios violada.</summary>
public class UsuarioException : DominioException
{
    public UsuarioException(string message) : base(message) { }
}

/// <summary>Regla de negocio del taller violada.</summary>
public class TallerException : DominioException
{
    public TallerException(string message) : base(message) { }
}

/// <summary>Regla de negocio de garantías/RMA violada.</summary>
public class GarantiaException : DominioException
{
    public GarantiaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de faltantes violada.</summary>
public class FaltanteException : DominioException
{
    public FaltanteException(string message) : base(message) { }
}

/// <summary>Regla de negocio de nómina violada.</summary>
public class NominaException : DominioException
{
    public NominaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de apartados violada.</summary>
public class ApartadoException : DominioException
{
    public ApartadoException(string message) : base(message) { }
}

/// <summary>Regla de negocio de NCF violada.</summary>
public class NcfException : DominioException
{
    public NcfException(string message) : base(message) { }
}

/// <summary>Regla de negocio de contabilidad violada.</summary>
public class ContabilidadException : DominioException
{
    public ContabilidadException(string message) : base(message) { }
}
