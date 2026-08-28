using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Application.FamilyGroups.Common;

/// <summary>
/// Resuelve el permiso operativo sobre un Grupo Familiar concreto (Sección 8.2).
/// </summary>
/// <remarks>
/// Existe por lo mismo que <c>ServiceCapturePermissions</c> en la rama de Servicios: la
/// comprobación se repite en seis casos de uso, y duplicarla es la forma más fácil de que un
/// día quede mal en uno solo. Aquí es además la ÚNICA autorización que hay — no hay marcador
/// estático que la respalde, así que si un handler se la salta, el endpoint queda abierto a
/// cualquier persona autenticada.
/// </remarks>
internal static class FamilyGroupPermissions
{
    /// <summary>
    /// Carga el grupo y comprueba de una vez que quien llama puede operarlo: o es Pastor
    /// (incluidos los superusuarios), o es literalmente esa casa.
    /// </summary>
    /// <remarks>
    /// Devuelve el MISMO error tanto si el grupo no existe como si existe y no es suyo. Es
    /// deliberado (Sección 2.1): distinguir ambos casos le diría a un Anfitrión que hay otros
    /// grupos ahí fuera, que es exactamente lo que esta rama no quiere que sepa.
    /// </remarks>
    public static async Task<Result<FamilyGroup>> LoadOwnedAsync(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        Guid familyGroupId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<FamilyGroup>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var group = await db.FamilyGroups
            .FirstOrDefaultAsync(g => g.Id == familyGroupId, cancellationToken);

        if (group is null || !CanOperate(currentUser, group, actorId))
        {
            return Result.Failure<FamilyGroup>(FamilyGroupErrors.Group.NotYours);
        }

        return Result.Success(group);
    }

    /// <summary>
    /// Pastor —o superusuario— pasa siempre; el resto solo si es el Anfitrión o el Encargado
    /// de ESTE grupo. Anfitrión y Encargado tienen permisos idénticos (Sección 3.1): la
    /// distinción entre ambos es informativa, no jerárquica.
    /// </summary>
    public static bool CanOperate(ICurrentUser currentUser, FamilyGroup group, Guid actorId)
    {
        var esAdministrador =
            currentUser.Roles.Contains(RoleNames.Pastor, StringComparer.OrdinalIgnoreCase)
            || RoleNames.Superusers.Any(
                r => currentUser.Roles.Contains(r, StringComparer.OrdinalIgnoreCase));

        return esAdministrador || group.IsOwnedBy(actorId);
    }
}
