namespace Penuel.Application.Abstractions;

/// <summary>Access token JWT emitido a una cuenta (Sección 8.1).</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Emisión del access token de corta duración, con los claims de la Sección 8.1:
/// <c>sub</c> (UserAccountId), <c>personId</c>, <c>email</c> y un claim <c>role</c>
/// por cada rol ACTIVO de la cuenta.
/// </summary>
public interface IJwtProvider
{
    AccessToken GenerateAccessToken(
        Guid userAccountId,
        Guid personId,
        string email,
        IReadOnlyCollection<string> roleNames);
}
