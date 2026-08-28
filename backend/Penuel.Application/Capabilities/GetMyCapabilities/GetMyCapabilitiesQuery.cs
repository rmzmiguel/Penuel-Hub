using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Capabilities.GetMyCapabilities;

public sealed record CapabilityRef(Guid Id, string Name);

public sealed record MyCapabilitiesResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    string? Email,
    bool IsOfficialMember,
    bool IsExecutiveBodyMember,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<CapabilityRef> Positions,
    IReadOnlyCollection<CapabilityRef> LedMinistries,
    IReadOnlyCollection<CapabilityRef> LedSocieties);

/// <summary>
/// Todo lo que la persona autenticada puede hacer HOY, en una sola llamada.
/// </summary>
/// <remarks>
/// Sección 8.4: el frontend arma su navegación a partir de esta respuesta y NUNCA de una lista
/// fija programada de antemano, porque en esta iglesia los liderazgos y cargos rotan. Cuando el
/// Pastor reasigna un liderazgo, el cambio se refleja la próxima vez que esa persona abra la
/// app, sin tocar una línea de código.
/// No lleva <see cref="IRequirePastor"/>: basta estar autenticado (Sección 8.2). Los tres ejes
/// se devuelven por separado justamente porque no se infieren entre sí (Sección 3.4).
/// </remarks>
public sealed record GetMyCapabilitiesQuery : IRequest<Result<MyCapabilitiesResponse>>;

public sealed class GetMyCapabilitiesQueryHandler
    : IRequestHandler<GetMyCapabilitiesQuery, Result<MyCapabilitiesResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyCapabilitiesQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MyCapabilitiesResponse>> Handle(
        GetMyCapabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.PersonId is not Guid personId)
        {
            return Result.Failure<MyCapabilitiesResponse>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<MyCapabilitiesResponse>(ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.PersonId == personId, cancellationToken);

        // Los roles se leen de la BASE, no de los claims del token: si al usuario le acaban de
        // revocar un rol, esta respuesta lo refleja de inmediato aunque su token aún lo mencione.
        var roles = account is null
            ? []
            : await _db.UserRoles
                .Where(ur => ur.UserAccountId == account.Id && ur.RevokedAt == null)
                .Select(ur => ur.Role.Name)
                .OrderBy(name => name)
                .ToListAsync(cancellationToken);

        var positions = await _db.PersonPositions
            .Where(pp => pp.PersonId == personId && pp.RevokedAt == null)
            .OrderBy(pp => pp.Position.Name)
            .Select(pp => new CapabilityRef(pp.PositionId, pp.Position.Name))
            .ToListAsync(cancellationToken);

        var isExecutiveBodyMember = await _db.PersonPositions
            .AnyAsync(pp => pp.PersonId == personId
                            && pp.RevokedAt == null
                            && pp.Position.IsExecutiveBody,
                cancellationToken);

        var ledMinistries = await _db.MinistryLeaderships
            .Where(l => l.PersonId == personId && l.RevokedAt == null)
            .OrderBy(l => l.Ministry.Name)
            .Select(l => new CapabilityRef(l.MinistryId, l.Ministry.Name))
            .ToListAsync(cancellationToken);

        var ledSocieties = await _db.SocietyLeaderships
            .Where(l => l.PersonId == personId && l.RevokedAt == null)
            .OrderBy(l => l.Society.Name)
            .Select(l => new CapabilityRef(l.SocietyId, l.Society.Name))
            .ToListAsync(cancellationToken);

        // Miembro es quien tiene la membresía ACTIVA. Que la fila exista no basta: al dar de
        // baja se conserva —para no perder la fecha de ingreso ni quién la registró—, así que
        // preguntar solo por su existencia dejaría a un exmiembro como miembro para siempre.
        var isOfficialMember = await _db.Memberships
            .AnyAsync(m => m.PersonId == personId && m.Status == MembershipStatus.Active,
                cancellationToken);

        var response = new MyCapabilitiesResponse(
            person.Id,
            person.FirstName,
            person.LastName,
            account?.Email,
            isOfficialMember,
            isExecutiveBodyMember,
            roles,
            positions,
            ledMinistries,
            ledSocieties);

        return Result.Success(response);
    }
}
