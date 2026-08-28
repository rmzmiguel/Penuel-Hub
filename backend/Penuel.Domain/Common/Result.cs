namespace Penuel.Domain.Common;

/// <summary>
/// Resultado de una operación que no devuelve valor.
/// Ningún caso de uso lanza excepciones para flujo de negocio esperado (Sección 5.3).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null && error != Error.None)
        {
            throw new InvalidOperationException("Un Result exitoso no puede llevar un Error.");
        }

        if (!isSuccess && (error is null || error == Error.None))
        {
            throw new InvalidOperationException("Un Result fallido debe llevar un Error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Resultado de una operación que devuelve un valor de tipo <typeparamref name="TValue"/>.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Valor de la operación. Acceder a él en un resultado fallido es un error de programación,
    /// no un caso de negocio: por eso aquí sí se lanza excepción.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede leer el Value de un Result fallido.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
