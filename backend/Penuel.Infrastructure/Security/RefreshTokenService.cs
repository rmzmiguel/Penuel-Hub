using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Penuel.Application.Abstractions;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Security;

/// <summary>
/// Emisión, validación y revocación de refresh tokens (Secciones 6.5 y 8.1).
/// </summary>
/// <remarks>
/// El token en claro son 32 bytes criptográficamente aleatorios codificados en Base64Url,
/// y de él solo se persiste un hash SHA-256. Aquí SHA-256 es correcto y BCrypt sería un error:
/// BCrypt existe para defender secretos de baja entropía (contraseñas que la gente elige) contra
/// fuerza bruta, a costa de ser lento a propósito. Un token de 256 bits de entropía real no es
/// forzable por diccionario, y necesita verificarse rápido en cada renovación.
/// Ningún método guarda cambios: eso lo hace el handler (ver <see cref="IRefreshTokenService"/>).
/// </remarks>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenBytes = 32;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly JwtOptions _options;

    public RefreshTokenService(
        IApplicationDbContext db,
        IDateTimeProvider clock,
        IOptions<JwtOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public IssuedRefreshToken Issue(Guid userAccountId)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddDays(_options.RefreshTokenDays);

        var plainToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

        var entity = RefreshToken.Issue(userAccountId, ComputeHash(plainToken), expiresAt, now);
        _db.RefreshTokens.Add(entity);

        return new IssuedRefreshToken(plainToken, expiresAt);
    }

    public async Task<RefreshToken?> FindAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var hash = ComputeHash(refreshToken);

        // Sin filtrar por RevokedAt ni por ExpiresAt: el handler necesita ver el estado real
        // para poder reaccionar al reuso de un token ya revocado.
        return await _db.RefreshTokens
            .Include(t => t.UserAccount)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public async Task<int> RevokeAllForUserAccountAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var live = await _db.RefreshTokens
            .Where(t => t.UserAccountId == userAccountId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in live)
        {
            token.Revoke(now);
        }

        return live.Count;
    }

    /// <summary>Hash determinista del token. Público para que el bootstrap y las pruebas lo reutilicen.</summary>
    public static string ComputeHash(string plainToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
