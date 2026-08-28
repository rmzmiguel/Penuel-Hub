using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Penuel.Application.Abstractions;

namespace Penuel.Infrastructure.Security;

/// <summary>
/// Emisión del access token con los claims exactos de la Sección 8.1.
/// </summary>
/// <remarks>
/// Usa <see cref="JsonWebTokenHandler"/> y no el antiguo <c>JwtSecurityTokenHandler</c>: además
/// de ser el recomendado en .NET 8, no reescribe los nombres de claim a URIs largas de
/// WS-Federation, así que el token lleva literalmente <c>sub</c>, <c>email</c> y <c>role</c>.
/// </remarks>
public sealed class JwtProvider : IJwtProvider
{
    public const string PersonIdClaim = "personId";
    public const string RoleClaim = "role";

    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public JwtProvider(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken GenerateAccessToken(
        Guid userAccountId,
        Guid personId,
        string email,
        IReadOnlyCollection<string> roleNames)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userAccountId.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [JwtRegisteredClaimNames.Email] = email,
            [PersonIdClaim] = personId.ToString(),
            // Un claim "role" por cada rol activo; se serializa como arreglo JSON.
            [RoleClaim] = roleNames.ToArray()
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }
}
