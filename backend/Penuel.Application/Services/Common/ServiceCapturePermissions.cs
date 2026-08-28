using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Domain.Constants;

namespace Penuel.Application.Services.Common;

/// <summary>
/// Resuelve, para la persona autenticada, qué alcance de captura tiene.
/// </summary>
/// <remarks>
/// Existe porque varios casos de uso de esta rama necesitan afinar la autorización DESPUÉS de
/// saber a qué tipo de servicio se refiere la operación — algo que un marcador estático no
/// puede expresar. El patrón es siempre el mismo: el behavior abre una puerta amplia
/// (<c>IRequireServiceCaptureAccess</c>) y el handler estrecha aquí.
/// </remarks>
internal sealed record ServiceScope(bool IsPastor, bool IsTreasurer, bool IsSundaySchoolRecorder)
{
    /// <summary>Ve y toca todo: cultos generales incluidos.</summary>
    public bool HasFullAccess => IsPastor || IsTreasurer;

    /// <summary>Solo lo agrupado por Sociedad, o sea Escuela Dominical.</summary>
    public bool IsSundaySchoolOnly => !HasFullAccess && IsSundaySchoolRecorder;
}

internal static class ServiceCapturePermissions
{
    public static async Task<ServiceScope> ResolveAsync(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var isPastor = currentUser.Roles.Contains(RoleNames.Pastor, StringComparer.OrdinalIgnoreCase);

        var isRecorder = currentUser.Roles.Contains(
            RoleNames.SundaySchoolRecorder, StringComparer.OrdinalIgnoreCase);

        // El cargo no viaja en el token; si el rol ya bastó, ni se consulta.
        var isTreasurer = isPastor || await db.PersonPositions.AnyAsync(
            pp => pp.PersonId == personId
                  && pp.RevokedAt == null
                  && pp.Position.Name == PositionNames.TesoreroGeneral,
            cancellationToken);

        return new ServiceScope(isPastor, isTreasurer, isRecorder);
    }
}
