using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;

namespace Penuel.Application.Common;

/// <summary>
/// Resuelve a qué iglesia pertenece quien ejecuta la operación, para propagar el
/// <c>ChurchId</c> a las entidades organizacionales que se creen.
/// </summary>
/// <remarks>
/// En esta fase existe una sola iglesia, así que tomarlo del operador o de la única fila
/// da el mismo resultado; se hace desde el operador porque es lo que seguirá siendo correcto
/// si algún día se activa el multi-tenant que la Sección 5.4 deja preparado.
/// </remarks>
internal static class ChurchScope
{
    public static async Task<Guid?> ResolveChurchIdAsync(
        IApplicationDbContext db,
        Guid actorPersonId,
        CancellationToken cancellationToken)
    {
        return await db.Persons
            .Where(p => p.Id == actorPersonId)
            .Select(p => (Guid?)p.ChurchId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
