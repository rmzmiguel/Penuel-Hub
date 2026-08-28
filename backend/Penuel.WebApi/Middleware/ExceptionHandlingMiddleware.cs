using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Penuel.Domain.Common;

namespace Penuel.WebApi.Middleware;

/// <summary>
/// Middleware global de errores (Sección 8.3). Captura toda excepción NO controlada —
/// las de infraestructura genuinamente inesperadas, nunca las de negocio, que viajan
/// como <see cref="Result"/> — y las traduce a la misma forma de respuesta.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Excepción no controlada en {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                // La respuesta ya se empezó a escribir: no se puede reemplazar sin corromperla.
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var status = StatusCodes.Status500InternalServerError;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = "Error inesperado",
            Type = $"https://httpstatuses.io/{status}",
            // En producción no se filtra el detalle de la excepción al cliente.
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : "Ocurrió un error inesperado en el servidor."
        };

        problem.Extensions["code"] = "Server.Unexpected";
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().FullName;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
