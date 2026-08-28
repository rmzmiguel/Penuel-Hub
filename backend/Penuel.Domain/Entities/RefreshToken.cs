namespace Penuel.Domain.Entities;

/// <summary>
/// Token de refresco. Se almacena únicamente su hash, nunca el token en claro (Sección 6.5).
/// Permite renovar el access token sin pedir contraseña y, sobre todo, revocar sesiones
/// de inmediato cuando a alguien se le retira un rol sensible (Sección 8.1).
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    public static RefreshToken Issue(
        Guid userAccountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            RevokedAt = null,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Expuesto como método y no como propiedad calculada a propósito: EF Core nunca intenta
    /// mapear un método, y así queda claro que las consultas deben filtrar por las columnas
    /// reales (<c>RevokedAt</c> / <c>ExpiresAt</c>), no por esto.
    /// </summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
