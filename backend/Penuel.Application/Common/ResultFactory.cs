using System.Reflection;
using Penuel.Domain.Common;

namespace Penuel.Application.Common;

/// <summary>
/// Construye un <see cref="Result"/> fallido cuando el tipo concreto (<c>Result</c> o
/// <c>Result&lt;T&gt;</c>) solo se conoce como parámetro genérico. Lo necesitan los behaviors
/// del pipeline, que interceptan cualquier caso de uso sin saber qué devuelve.
/// </summary>
internal static class ResultFactory
{
    private static readonly MethodInfo GenericFailure = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition);

    public static TResponse Failure<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        return (TResponse)GenericFailure.MakeGenericMethod(valueType).Invoke(null, [error])!;
    }
}
