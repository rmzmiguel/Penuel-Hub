using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.GetFamilyGroupDetail;

public sealed record FamilyGroupMemberDetail(
    Guid PersonId,
    string FirstName,
    string LastName,
    DateOnly JoinedAt);

public sealed record FamilyGroupMeetingSummary(
    Guid MeetingId,
    DateOnly MeetingDate,
    decimal TotalOffering,
    int PresentCount,
    int MemberCount);

public sealed record FamilyGroupDetail(
    Guid FamilyGroupId,
    string Address,
    DayOfWeek DefaultMeetingDayOfWeek,
    bool IsActive,
    Guid HostPersonId,
    string HostFirstName,
    string HostLastName,
    Guid LeaderPersonId,
    string LeaderFirstName,
    string LeaderLastName,
    IReadOnlyCollection<FamilyGroupMemberDetail> Members,
    IReadOnlyCollection<FamilyGroupMeetingSummary> RecentMeetings);

/// <summary>
/// Todo sobre un grupo: dirección, Anfitrión, Encargado, integrantes y últimos reportes.
/// Exclusivo del Pastor (Sección 8.1).
/// </summary>
/// <remarks>
/// Es del Pastor y no del Anfitrión, aunque parezca que el Anfitrión debería ver "su" grupo.
/// El Anfitrión ya tiene <c>GetMyFamilyGroupsQuery</c>, que le da lo suyo sin exponer la
/// forma "cualquier grupo por identificador" — que es justo la que permitiría probar
/// identificadores ajenos y descubrir que existen otras casas (Sección 2.1).
/// </remarks>
public sealed record GetFamilyGroupDetailQuery(Guid FamilyGroupId)
    : IRequest<Result<FamilyGroupDetail>>, IRequirePastor;

public sealed class GetFamilyGroupDetailQueryHandler
    : IRequestHandler<GetFamilyGroupDetailQuery, Result<FamilyGroupDetail>>
{
    /// <summary>Últimos reportes que se muestran. Lo suficiente para ver el ritmo del grupo.</summary>
    private const int ReportesRecientes = 12;

    private readonly IApplicationDbContext _db;

    public GetFamilyGroupDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<FamilyGroupDetail>> Handle(
        GetFamilyGroupDetailQuery request,
        CancellationToken cancellationToken)
    {
        var group = await _db.FamilyGroups
            .Include(g => g.Host)
            .Include(g => g.Leader)
            .FirstOrDefaultAsync(g => g.Id == request.FamilyGroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<FamilyGroupDetail>(
                FamilyGroupErrors.Group.NotFound(request.FamilyGroupId));
        }

        var members = await _db.GroupMembers
            .Where(m => m.FamilyGroupId == group.Id && m.LeftAt == null)
            .OrderBy(m => m.Person.FirstName).ThenBy(m => m.Person.LastName)
            .Select(m => new FamilyGroupMemberDetail(
                m.PersonId, m.Person.FirstName, m.Person.LastName, m.JoinedAt))
            .ToListAsync(cancellationToken);

        var meetings = await _db.FamilyGroupMeetings
            .Where(m => m.FamilyGroupId == group.Id)
            .OrderByDescending(m => m.MeetingDate)
            .Take(ReportesRecientes)
            .Select(m => new FamilyGroupMeetingSummary(
                m.Id,
                m.MeetingDate,
                m.TotalOffering,
                m.Attendances.Count(a => a.WasPresent),
                m.Attendances.Count))
            .ToListAsync(cancellationToken);

        var detalle = new FamilyGroupDetail(
            group.Id,
            group.Address,
            group.DefaultMeetingDayOfWeek,
            group.Status == FamilyGroupStatus.Active,
            group.HostPersonId, group.Host.FirstName, group.Host.LastName,
            group.LeaderPersonId, group.Leader.FirstName, group.Leader.LastName,
            members,
            meetings);

        return Result.Success(detalle);
    }
}
