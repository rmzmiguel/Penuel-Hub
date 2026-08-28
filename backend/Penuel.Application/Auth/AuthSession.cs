using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Domain.Entities;

namespace Penuel.Application.Auth;

/// <summary>Sesión emitida tras un login o una renovación (Sección 8.1).</summary>
public sealed record AuthSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid UserAccountId,
    Guid PersonId,
    string Email,
    IReadOnlyCollection<string> Roles);

/// <summary>
/// Emite el par access token + refresh token para una cuenta. Compartido por el login y la
/// renovación para que ambos caminos produzcan exactamente la misma forma de sesión y lean
/// los roles de la misma manera.
/// </summary>
internal static class AuthSession
{
    /// <summary>
    /// No confirma la transacción: quien lo llama decide cuándo hacer <c>SaveChangesAsync</c>.
    /// Los roles se leen SIEMPRE de la base en este momento, nunca de un token anterior —
    /// es lo que hace que renovar un token refleje una revocación de rol reciente.
    /// </summary>
    public static async Task<AuthSessionResponse> IssueAsync(
        IApplicationDbContext db,
        IJwtProvider jwtProvider,
        IRefreshTokenService refreshTokenService,
        UserAccount account,
        CancellationToken cancellationToken)
    {
        var roles = await db.UserRoles
            .Where(ur => ur.UserAccountId == account.Id && ur.RevokedAt == null)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);

        var accessToken = jwtProvider.GenerateAccessToken(
            account.Id, account.PersonId, account.Email, roles);

        var refreshToken = refreshTokenService.Issue(account.Id);

        return new AuthSessionResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            account.Id,
            account.PersonId,
            account.Email,
            roles);
    }
}
