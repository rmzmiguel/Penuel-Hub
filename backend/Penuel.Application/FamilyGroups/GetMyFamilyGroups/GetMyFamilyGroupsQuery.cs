using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.GetMyFamilyGroups;

/// <summary>Una persona del grupo, tal como aparece en la lista de asistencia.</summary>
public sealed record MyGroupMember(Guid PersonId, string FirstName, string LastName);

/// <summary>Un grupo que la persona autenticada lleva, con todo lo que su pantalla necesita.</summary>
public sealed record MyFamilyGroup(
    Guid FamilyGroupId,
    string Address,
    DayOfWeek DefaultMeetingDayOfWeek,
    /// <summary>Informativo: deja que la pantalla diga "tu casa" o "diriges en casa de…".</summary>
    bool IsHost,
    bool IsLeader,
    string HostFirstName,
    string HostLastName,
    /// <summary>Fecha del último reporte, para no volver a levantar el mismo por error.</summary>
    DateOnly? LastMeetingDate,
    IReadOnlyCollection<MyGroupMember> Members);

/// <summary>
/// Los Grupos Familiares donde la persona autenticada es Anfitriona o Encargada
/// (Sección 8.4).
/// </summary>
/// <remarks>
/// Es la consulta que decide qué aplicación ve alguien. Vive aparte de
/// <c>GetMyCapabilities</c> del Core —igual que <c>GetMySundaySchoolCaptureContext</c> de
/// Servicios— para no meter el vocabulario de esta rama en el contrato compartido.
///
/// Normalmente devuelve cero o uno, pero no se impone ese límite: nada en el dominio impide
/// que alguien lleve dos casas, y una restricción artificial obligaría a inventar una
/// migración el día que ocurra. Si devuelve exactamente uno, el frontend entra directo a esa
/// pantalla sin selector.
///
/// Lleva <see cref="IAuthorizeInHandler"/> y no queda sin marcador: no está "abierta", está
/// ACOTADA POR EL HANDLER, que solo devuelve los grupos de quien pregunta. Es exactamente lo
/// que ese marcador significa, y decirlo evita que el guardián estructural de las pruebas la
/// confunda con un caso de uso al que se le olvidó la autorización.
///
/// Devuelve los integrantes dentro para que la pantalla del Anfitrión se dibuje con UNA
/// llamada; en un teléfono con mala señal dentro de una casa, cada ida y vuelta se nota.
/// </remarks>
public sealed record GetMyFamilyGroupsQuery
    : IRequest<Result<IReadOnlyCollection<MyFamilyGroup>>>, IAuthorizeInHandler;

public sealed class GetMyFamilyGroupsQueryHandler
    : IRequestHandler<GetMyFamilyGroupsQuery, Result<IReadOnlyCollection<MyFamilyGroup>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyFamilyGroupsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<MyFamilyGroup>>> Handle(
        GetMyFamilyGroupsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.PersonId is not Guid personId)
        {
            return Result.Failure<IReadOnlyCollection<MyFamilyGroup>>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var grupos = await _db.FamilyGroups
            .Where(g => g.Status == FamilyGroupStatus.Active
                        && (g.HostPersonId == personId || g.LeaderPersonId == personId))
            .OrderBy(g => g.Address)
            .Select(g => new
            {
                g.Id,
                g.Address,
                g.DefaultMeetingDayOfWeek,
                IsHost = g.HostPersonId == personId,
                IsLeader = g.LeaderPersonId == personId,
                g.Host.FirstName,
                g.Host.LastName,
                LastMeetingDate = _db.FamilyGroupMeetings
                    .Where(m => m.FamilyGroupId == g.Id)
                    .OrderByDescending(m => m.MeetingDate)
                    .Select(m => (DateOnly?)m.MeetingDate)
                    .FirstOrDefault(),
                Members = _db.GroupMembers
                    .Where(m => m.FamilyGroupId == g.Id && m.LeftAt == null)
                    .OrderBy(m => m.Person.FirstName).ThenBy(m => m.Person.LastName)
                    .Select(m => new MyGroupMember(m.PersonId, m.Person.FirstName, m.Person.LastName))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<MyFamilyGroup> resultado = [.. grupos.Select(g => new MyFamilyGroup(
            g.Id, g.Address, g.DefaultMeetingDayOfWeek, g.IsHost, g.IsLeader,
            g.FirstName, g.LastName, g.LastMeetingDate, g.Members))];

        return Result.Success(resultado);
    }
}
