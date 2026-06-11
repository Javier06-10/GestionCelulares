namespace GestionCelulares.Application.Common;

/// <summary>Credenciales o token inválidos -> HTTP 401 en el controlador.</summary>
public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}

/// <summary>Regla de negocio de inventario violada -> HTTP 400/404 en el controlador.</summary>
public class InventarioException : Exception
{
    public InventarioException(string message) : base(message) { }
}

/// <summary>Regla de negocio de clientes violada -> HTTP 400/404 en el controlador.</summary>
public class ClienteException : Exception
{
    public ClienteException(string message) : base(message) { }
}

/// <summary>Regla de negocio de proveedores violada -> HTTP 400/404 en el controlador.</summary>
public class ProveedorException : Exception
{
    public ProveedorException(string message) : base(message) { }
}

/// <summary>Regla de negocio del catálogo violada -> HTTP 400/404 en el controlador.</summary>
public class CatalogoException : Exception
{
    public CatalogoException(string message) : base(message) { }
}

/// <summary>Regla de negocio de caja violada -> HTTP 400/404 en el controlador.</summary>
public class CajaException : Exception
{
    public CajaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de ventas violada -> HTTP 400/404 en el controlador.</summary>
public class VentaException : Exception
{
    public VentaException(string message) : base(message) { }
}

/// <summary>Regla de negocio de créditos violada -> HTTP 400/404 en el controlador.</summary>
public class CreditoException : Exception
{
    public CreditoException(string message) : base(message) { }
}
