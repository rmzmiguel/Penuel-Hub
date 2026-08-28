using Penuel.Domain.Entities;

namespace Penuel.Application.Abstractions;

/// <summary>Refresh token recién emitido: el valor en claro se entrega una sola vez al cliente.</summary>
public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Emisión, validación y revocación de refresh tokens (Secciones 6.5 y 8.1).
/// </summary>
/// <remarks>
/// IMPORTANTE: ningún método de este servicio llama a <c>SaveChangesAsync</c>. Los cambios se
/// registran en el contexto y es el handler quien confirma la transacción, para que la emisión
/// del token y el resto del caso de uso se guarden juntos o no se guarden.
/// </remarks>
public interface IRefreshTokenService
{
    /// <summary>Genera un token aleatorio, guarda solo su hash y devuelve el valor en claro.</summary>
    IssuedRefreshToken Issue(Guid userAccountId);

    /// <summary>
    /// Busca el token por su hash en CUALQUIER estado: activo, revocado o expirado.
    /// Devolver también los revocados es deliberado — es la única forma de distinguir
    /// "este token nunca existió" de "este token ya se usó", que es la señal de robo
    /// que dispara la revocación masiva (Sección 8.1).
    /// </summary>
    Task<RefreshToken?> FindAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoca todas las sesiones vivas de una cuenta. Es lo que permite que retirar un rol
    /// corte el acceso de inmediato y no al expirar el token (Sección 8.1).
    /// </summary>
    Task<int> RevokeAllForUserAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default);
}
