namespace Penuel.Domain.Common;

/// <summary>
/// Error de negocio esperado. No es una excepción: viaja dentro de un <see cref="Result"/>.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>Marcador de "sin error". Solo lo usa un <see cref="Result"/> exitoso.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    /// <summary>Sin sesión (401).</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Con sesión, pero sin el rol o cargo necesario (403).</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);

    public override string ToString() => $"{Code}: {Message}";
}
