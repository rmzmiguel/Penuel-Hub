using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.GetAllFamilyGroups;

/// <summary>Un grupo en la lista general del Pastor. Resumen, no detalle.</summary>
public sealed record FamilyGroupSummary(
    Guid FamilyGroupId,
    string Address,
    DayOfWeek DefaultMeetingDayOfWeek,
    bool IsActive,
    string HostFirstName,
    string HostLastName,
    string LeaderFirstName,
    string LeaderLastName,
    int ActiveMemberCount,
    DateOnly? LastMeetingDate);

/// <summary>
/// Todos los Grupos Familiares de la iglesia. Exclusivo del Pastor (Sección 8.1).
/// </summary>
/// <remarks>
/// Incluye los inactivos, marcados: un grupo que dejó de reunirse sigue teniendo historia, y
/// esconderlo haría creer que se borró. El conteo de integrantes y la fecha del último reporte
/// van aquí porque son justo las dos cosas que el Pastor mira para saber si una casa sigue
/// viva sin tener que entrar en cada una.
/// </remarks>
public sealed record GetAllFamilyGroupsQuery
    : IRequest<Result<IReadOnlyCollection<FamilyGroupSummary>>>, IRequirePastor;

public sealed class GetAllFamilyGroupsQueryHandler
    : IRequestHandler<GetAllFamilyGroupsQuery, Result<IReadOnlyCollection<FamilyGroupSummary>>>
{
    private readonly IApplicationDbContext _db;

    public GetAllFamilyGroupsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyCollection<FamilyGroupSummary>>> Handle(
        GetAllFamilyGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var grupos = await _db.FamilyGroups
            // Activos primero, y dentro de cada bloque por dirección: la lista se lee como
            // "lo que está funcionando" seguido de "lo que se detuvo".
            .OrderBy(g => g.Status)
            .ThenBy(g => g.Address)
            .Select(g => new FamilyGroupSummary(
                g.Id,
                g.Address,
                g.DefaultMeetingDayOfWeek,
                g.Status == FamilyGroupStatus.Active,
                g.Host.FirstName,
                g.Host.LastName,
                g.Leader.FirstName,
                g.Leader.LastName,
                _db.GroupMembers.Count(m => m.FamilyGroupId == g.Id && m.LeftAt == null),
                _db.FamilyGroupMeetings
                    .Where(m => m.FamilyGroupId == g.Id)
                    .OrderByDescending(m => m.MeetingDate)
                    .Select(m => (DateOnly?)m.MeetingDate)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<FamilyGroupSummary>>(grupos);
    }
}
