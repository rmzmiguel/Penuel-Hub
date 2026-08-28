using Microsoft.IdentityModel.JsonWebTokens;
using Penuel.Application.Abstractions;
using Penuel.Infrastructure.Security;

namespace Penuel.WebApi.Services;

/// <summary>
/// Lee la identidad del JWT ya validado de la petición en curso. Es la fuente del
/// <c>PersonId</c> que exige la auditoría de la regla 7.4.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserAccountId => ReadGuid(JwtRegisteredClaimNames.Sub);

    public Guid? PersonId => ReadGuid(JwtProvider.PersonIdClaim);

    public string? Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(JwtProvider.RoleClaim).Select(c => c.Value).ToArray() ?? [];

    private Guid? ReadGuid(string claimType)
    {
        var raw = Principal?.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out var value) ? value : null;
    }
}
