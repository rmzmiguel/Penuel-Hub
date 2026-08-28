using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Persons.GetPersons;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Societies.GetSocietyMembers;

public sealed record SocietyMembersResponse(
    Guid SocietyId,
    string SocietyName,
    IReadOnlyCollection<PersonOption> Members);

/// <summary>
/// Integrantes activos de una Sociedad. Es lo que precarga la lista de asistencia dominical:
/// el maestro abre el reporte y ya ve a su grupo, en vez de reencontrarlo cada domingo entre
/// toda la congregación.
/// </summary>
public sealed record GetSocietyMembersQuery(Guid SocietyId)
    : IRequest<Result<SocietyMembersResponse>>, IRequireDirectoryAccess;

public sealed class GetSocietyMembersQueryHandler
    : IRequestHandler<GetSocietyMembersQuery, Result<SocietyMembersResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetSocietyMembersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SocietyMembersResponse>> Handle(
        GetSocietyMembersQuery request,
        CancellationToken cancellationToken)
    {
        var society = await _db.Societies
            .Where(s => s.Id == request.SocietyId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (society is null)
        {
            return Result.Failure<SocietyMembersResponse>(
                ApplicationErrors.Society.NotFound(request.SocietyId));
        }

        var members = await _db.SocietyMemberships
            .Where(m => m.SocietyId == request.SocietyId
                        && m.RevokedAt == null
                        && m.Person.Status == PersonStatus.Active)
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName)
            .Select(m => new PersonOption(m.PersonId, m.Person.FirstName, m.Person.LastName))
            .ToListAsync(cancellationToken);

        return Result.Success(new SocietyMembersResponse(society.Id, society.Name, members));
    }
}
