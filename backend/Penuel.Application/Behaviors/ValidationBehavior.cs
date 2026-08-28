using FluentValidation;
using MediatR;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Behaviors;

/// <summary>
/// Ejecuta los <c>FluentValidation</c> registrados para el request antes de llegar al handler.
/// </summary>
/// <remarks>
/// Una validación fallida NO lanza excepción: devuelve un <see cref="Result"/> fallido de tipo
/// <see cref="ErrorType.Validation"/>, que el middleware de la WebApi traduce a HTTP 400
/// (Sección 5.3). La restricción <c>where TResponse : Result</c> es lo que permite construir
/// ese resultado sin conocer el tipo concreto.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var message = string.Join(" | ", failures.Select(f => f.ErrorMessage).Distinct());

        return ResultFactory.Failure<TResponse>(ApplicationErrors.Validation.Failed(message));
    }
}
