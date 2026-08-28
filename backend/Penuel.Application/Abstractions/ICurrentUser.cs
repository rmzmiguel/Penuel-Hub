namespace Penuel.Application.Abstractions;

/// <summary>
/// Identidad de quien ejecuta la petición actual, leída de los claims del JWT.
/// Es la fuente del <c>PersonId</c> que exige la auditoría de la regla 7.4.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserAccountId { get; }
    Guid? PersonId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}
