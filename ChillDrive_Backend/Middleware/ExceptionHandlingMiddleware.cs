using System.Net;
using System.Text.Json;

namespace ChillDrive_Backend.Middleware;

/// <summary>
/// Middleware que actúa como red de seguridad global: captura cualquier excepción
/// no manejada que suba por el pipeline y devuelve un JSON { mensaje } consistente
/// con el formato del resto de la API, sin exponer detalles internos en producción.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate                      _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment                     _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate                      next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment                     env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pasa el request al siguiente eslabón del pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Registra el error completo (stack trace incluido) para diagnóstico
            _logger.LogError(ex, "Excepción no manejada — {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await EscribirRespuestaDeError(context, ex);
        }
    }

    private async Task EscribirRespuestaDeError(HttpContext context, Exception ex)
    {
        // Si ASP.NET Core ya empezó a enviar bytes de respuesta no podemos reemplazarla;
        // en ese caso solo queda cortar la conexión (el logger ya registró el error).
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        // En desarrollo mostramos la cadena completa de excepciones (incluida la inner, que es
        // donde EF Core esconde la causa real, p.ej. el error de Postgres) para facilitar el debug.
        // En producción usamos un texto genérico para no filtrar información interna al cliente.
        var mensaje = _env.IsDevelopment()
            ? $"Error interno: {ObtenerCadenaDeMensajes(ex)}"
            : "Ocurrió un error interno del servidor. Por favor, inténtalo más tarde.";

        // Serializar con camelCase para coincidir con la convención de la API
        var json = JsonSerializer.Serialize(
            new { mensaje },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(json);
    }

    // Recorre la excepción y todas sus inner exceptions y concatena sus mensajes.
    // Así la causa raíz (que EF Core envuelve) queda visible en desarrollo.
    private static string ObtenerCadenaDeMensajes(Exception ex)
    {
        var mensajes = new List<string>();
        Exception? actual = ex;
        while (actual is not null)
        {
            mensajes.Add(actual.Message);
            actual = actual.InnerException;
        }
        return string.Join(" → ", mensajes);
    }
}
