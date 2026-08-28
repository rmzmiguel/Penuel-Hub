using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;

namespace Penuel.Application.Behaviors;

/// <summary>
/// Resuelve la autorización dentro de Penuel.Application (Sección 5.4 del Core). Atiende dos
/// marcadores:
///   <see cref="IRequirePastor"/>        — exige el rol Pastor (regla 7.5 y default de 8.2).
///   <see cref="IRequireAuthorization"/> — exige alguno de los roles aceptados, o alguno de
///                                          los cargos aceptados ACTIVO (Sección 8.3 de la
///                                          rama de Servicios).
///   <see cref="IAuthorizeInHandler"/>   — la decisión depende del RECURSO y solo puede
///                                          tomarla el handler (rama de Grupos Familiares).
/// </summary>
/// <remarks>
/// Corre ANTES de <c>ValidationBehavior</c>: si quien llama no tiene permiso, no tiene sentido
/// gastar tiempo validando su petición ni devolverle pistas sobre ella.
/// La consulta a la base solo ocurre cuando la comprobación de roles NO bastó y el marcador
/// declara cargos aceptados; el camino común no paga nada.
/// </remarks>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _db;

    public AuthorizationBehavior(ICurrentUser currentUser, IApplicationDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not (IRequirePastor or IRequireAuthorization or IAuthorizeInHandler))
        {
            return await next();
        }

        if (!_currentUser.IsAuthenticated || _currentUser.PersonId is not Guid personId)
        {
            return ResultFactory.Failure<TResponse>(ApplicationErrors.Auth.NotAuthenticated);
        }

        // Superusuario: NO se le comprueba nada, se le deja pasar. Va antes de repartir por
        // marcador y no dentro de cada rama a propósito — si fuera "un rol aceptado más",
        // habría que acordarse de añadirlo a cada marcador nuevo, y el día que a alguien se le
        // olvidara, el rol quedaría silenciosamente incompleto. Aquí no hay nada que olvidar:
        // cualquier marcador que se invente en el futuro queda cubierto por construcción.
        //
        // Los roles salen de los claims, pero eso NO lo convierte en un permiso que sobreviva a
        // su revocación: OnTokenValidated revalida los roles contra la base en cada petición,
        // así que quitar el rol corta el acceso al instante, igual que con cualquier otro.
        if (RoleNames.Superusers.Any(
                role => _currentUser.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            return await next();
        }

        // Tercer patrón: no hay nada estático que comprobar. Lo único que este behavior
        // puede garantizar —y garantiza— es que hay una sesión válida; quién puede tocar ESTE
        // recurso concreto solo se sabe habiéndolo cargado, y de eso responde el handler.
        if (request is IAuthorizeInHandler)
        {
            return await next();
        }

        if (request is IRequirePastor)
        {
            return _currentUser.Roles.Contains(RoleNames.Pastor, StringComparer.OrdinalIgnoreCase)
                ? await next()
                : ResultFactory.Failure<TResponse>(ApplicationErrors.Auth.PastorRoleRequired);
        }

        var requirement = (IRequireAuthorization)request;

        // 1. Roles: salen de los claims del token, sin tocar la base.
        if (requirement.AcceptedRoles.Any(
                role => _currentUser.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            return await next();
        }

        // 2. Cargos: NO viajan en el token, así que se resuelven contra la base. Es la razón
        //    por la que revocar un cargo surte efecto de inmediato, igual que revocar un rol.
        if (requirement.AcceptedPositions.Count > 0)
        {
            var names = requirement.AcceptedPositions.ToArray();

            var holdsPosition = await _db.PersonPositions
                .AnyAsync(
                    pp => pp.PersonId == personId
                          && pp.RevokedAt == null
                          && names.Contains(pp.Position.Name),
                    cancellationToken);

            if (holdsPosition)
            {
                return await next();
            }
        }

        return ResultFactory.Failure<TResponse>(
            ApplicationErrors.Auth.InsufficientPermissions(
                requirement.AcceptedRoles, requirement.AcceptedPositions));
    }
}
