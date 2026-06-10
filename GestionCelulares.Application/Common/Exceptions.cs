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
