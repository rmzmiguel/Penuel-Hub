using Microsoft.AspNetCore.Mvc;
using Penuel.Domain.Common;

namespace Penuel.WebApi.Extensions;

/// <summary>Identificador del recurso recién creado.</summary>
public sealed record CreatedResourceResponse(Guid Id);

/// <summary>
/// Traduce un <see cref="Result"/> al código HTTP que le corresponde según su
/// <see cref="ErrorType"/> (Sección 5.3). Ningún controlador decide códigos por su cuenta:
/// el mapeo vive aquí, una sola vez.
/// </summary>
public static class ResultExtensions
{
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>Éxito sin valor de retorno -> 204 No Content.</summary>
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToProblem(result.Error!);

    /// <summary>Éxito con valor -> 200 OK con el valor en el cuerpo.</summary>
    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToProblem(result.Error!);

    /// <summary>Creación exitosa -> 201 Created con el identificador del recurso.</summary>
    public static IActionResult ToCreatedResult(this Result<Guid> result) =>
        result.IsSuccess
            ? new ObjectResult(new CreatedResourceResponse(result.Value))
            {
                StatusCode = StatusCodes.Status201Created
            }
            : ToProblem(result.Error!);

    /// <summary>
    /// Forma única de respuesta de error de toda la API. El middleware global (Sección 8.3)
    /// produce exactamente esta misma forma para las excepciones no controladas, de modo que
    /// el frontend nunca tiene que distinguir dos formatos.
    /// </summary>
    public static IActionResult ToProblem(Error error)
    {
        var status = error.Type.ToStatusCode();

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(error.Type),
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{status}"
        };

        // El código estable es lo que el frontend debe leer; el Detail es para humanos.
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status };
    }

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "La petición no es válida",
        ErrorType.NotFound => "Recurso no encontrado",
        ErrorType.Conflict => "Conflicto con el estado actual",
        ErrorType.Unauthorized => "No autenticado",
        ErrorType.Forbidden => "Permisos insuficientes",
        _ => "Error inesperado"
    };
}
