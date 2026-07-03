using GestionCelulares.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestionCelulares.Api;

/// <summary>
/// Manejo centralizado de excepciones: traduce las de negocio a HTTP 400/401 con
/// un cuerpo consistente y convierte cualquier otra en un 500 genérico (sin filtrar
/// detalles), registrando la excepción real. Actúa como red de seguridad debajo de
/// los try/catch de los controladores.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title, detail) = ex switch
        {
            AuthException => (StatusCodes.Status401Unauthorized, "No autorizado", ex.Message),
            DominioException => (StatusCodes.Status400BadRequest, "Solicitud inválida", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
        };

        // Solo las inesperadas se registran con todo el detalle (las de negocio son esperadas).
        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Excepción no controlada en {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        // Campo 'error' para mantener compatibilidad con el frontend (lee err.error.error).
        problem.Extensions["error"] = detail;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
