using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Persons.GetPersonAdministration;

/// <summary>Un rol del catálogo y si esta persona lo tiene ahora mismo.</summary>
public sealed record AdminRole(string Name, string Description, bool Granted);

/// <summary>Un cargo del catálogo y si esta persona lo ostenta ahora mismo.</summary>
public sealed record AdminPosition(Guid PositionId, string Name, bool IsExecutiveBody, bool Held);

/// <summary>
/// Un ministerio del catálogo, con su liderazgo actual.
/// </summary>
/// <remarks>
/// Se devuelven TODOS y no solo los que esta persona lidera, por la misma razón que los roles:
/// para poder ponerla al frente de uno hay que poder listarlos. <c>CurrentLeaderName</c> evita
/// la única sorpresa posible del panel — asignar líder a un grupo que ya tiene uno falla, y sin
/// este dato el error aparecería sin explicación.
/// </remarks>
public sealed record AdminMinistry(
    Guid MinistryId,
    string Name,
    bool LedByThisPerson,
    string? CurrentLeaderName);

/// <summary>
/// Una sociedad del catálogo. Reúne los DOS vínculos que una persona puede tener con ella
/// —liderarla y pertenecer a ella—, que son independientes: el líder no es necesariamente
/// integrante, y un integrante no lidera.
/// </summary>
/// <remarks>
/// <c>SocietyMembershipId</c> viaja porque quitar a alguien de un grupo se hace contra el Id de
/// LA PERTENENCIA (<c>DELETE /api/societies/members/{id}</c>), no contra el de la sociedad.
/// </remarks>
public sealed record AdminSociety(
    Guid SocietyId,
    string Name,
    bool LedByThisPerson,
    string? CurrentLeaderName,
    bool IsMember,
    Guid? SocietyMembershipId);

/// <summary>
/// La casa a la que asiste, con quién la lleva. Nulo si no va a ninguna.
/// </summary>
/// <remarks>
/// Anfitrión y Encargado viajan como NOMBRE y no como identificador: esto se pinta, no se
/// navega. Y viajan los dos aunque sean la misma persona, para que la pantalla decida si
/// dice "su casa" o "en casa de X".
/// </remarks>
public sealed record AdminFamilyGroup(
    Guid FamilyGroupId,
    string Address,
    DateOnly JoinedAt,
    bool IsHost,
    bool IsLeader,
    string HostName,
    string LeaderName);

/// <summary>
/// Una marca de asistencia, sea de donde sea.
/// </summary>
/// <remarks>
/// Se mezclan las dos fuentes —cultos y Grupos Familiares— a propósito. La pregunta que
/// contesta esta lista es "¿qué tan constante es esta persona?", y responderla mirando solo
/// una mitad de su vida en la iglesia daría una respuesta falsa: alguien que no falta a su
/// grupo del jueves pero nunca va al culto no es "inconstante", es otra cosa.
/// </remarks>
public sealed record AdminAttendance(DateOnly Date, bool WasPresent, string Source);

public sealed record PersonAdministrationResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    /// <summary>Los tres campos editables de la ficha (UpdatePersonCommand).</summary>
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    bool IsActive,
    Guid? UserAccountId,
    string? Email,
    bool HasAccount,
    bool AccountIsActive,
    bool IsOfficialMember,
    /// <summary>
    /// Si existe la fila de membresía, aunque esté dada de baja. Distingue "nunca fue miembro"
    /// —hay que crearla, con su fecha de ingreso— de "lo fue y se le dio de baja" —basta
    /// restituirla—. Con <c>MemberSince</c> no se puede saber: la fecha es opcional, así que un
    /// nulo no significa que no haya fila.
    /// </summary>
    bool HasMembershipRecord,
    DateOnly? MemberSince,
    IReadOnlyCollection<AdminRole> Roles,
    IReadOnlyCollection<AdminPosition> Positions,
    IReadOnlyCollection<AdminMinistry> Ministries,
    IReadOnlyCollection<AdminSociety> Societies,
    AdminFamilyGroup? FamilyGroup,
    /// <summary>De la más antigua a la más reciente, lista para dibujarse en una fila.</summary>
    IReadOnlyCollection<AdminAttendance> RecentAttendance);

/// <summary>
/// Todo el estado administrativo de UNA persona, en una sola llamada.
/// </summary>
/// <remarks>
/// Es la lectura que le faltaba al sistema. Sin ella, una pantalla de permisos sólo puede
/// escribir a ciegas: sabe otorgar un rol, pero no si ya lo tiene, así que no puede ofrecer
/// retirarlo. Toda operación de escritura del panel ya existía; lo que no existía era la
/// forma de saber qué está encendido.
///
/// Devuelve los CATÁLOGOS COMPLETOS de roles y cargos, cada uno con su marca de si la persona
/// lo tiene, en lugar de solo lo que tiene. Así el frontend dibuja los interruptores sin
/// necesitar una segunda llamada y —más importante— sin escribir un solo nombre de rol en su
/// código: la lista de lo que se puede otorgar la manda el servidor, que es el que la sabe.
/// Es el mismo patrón de <c>GetMySundaySchoolCaptureContext</c>.
///
/// Lleva <see cref="IRequirePastor"/>: expone correo, cuenta y permisos de otra persona, que
/// es bastante más que el directorio. Los superusuarios pasan por el atajo del
/// <c>AuthorizationBehavior</c>, sin que este marcador tenga que enterarse.
/// </remarks>
public sealed record GetPersonAdministrationQuery(Guid PersonId)
    : IRequest<Result<PersonAdministrationResponse>>, IRequirePastor;

public sealed class GetPersonAdministrationQueryHandler
    : IRequestHandler<GetPersonAdministrationQuery, Result<PersonAdministrationResponse>>
{
    /// <summary>Cuántas marcas se devuelven. Suficientes para ver una racha sin ser un historial.</summary>
    private const int AsistenciasRecientes = 24;

    private readonly IApplicationDbContext _db;

    public GetPersonAdministrationQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PersonAdministrationResponse>> Handle(
        GetPersonAdministrationQuery request,
        CancellationToken cancellationToken)
    {
        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<PersonAdministrationResponse>(
                ApplicationErrors.Person.NotFound(request.PersonId));
        }

        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.PersonId == person.Id, cancellationToken);

        // Los roles vivos de la cuenta. Sin cuenta no puede haber roles: un rol se otorga a
        // unas credenciales, no a una persona (regla 7.4).
        var granted = account is null
            ? new List<string>()
            : await _db.UserRoles
                .Where(ur => ur.UserAccountId == account.Id && ur.RevokedAt == null)
                .Select(ur => ur.Role.Name)
                .ToListAsync(cancellationToken);

        var roles = await _db.Roles
            .Where(r => r.ChurchId == person.ChurchId)
            .OrderBy(r => r.Name)
            .Select(r => new { r.Name, r.Description })
            .ToListAsync(cancellationToken);

        var heldPositionIds = await _db.PersonPositions
            .Where(pp => pp.PersonId == person.Id && pp.RevokedAt == null)
            .Select(pp => pp.PositionId)
            .ToListAsync(cancellationToken);

        var positions = await _db.Positions
            .Where(p => p.ChurchId == person.ChurchId)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.IsExecutiveBody })
            .ToListAsync(cancellationToken);

        // Liderazgos VIVOS de cada grupo, con el nombre de quien lo ostenta. Se traen todos
        // de una vez en lugar de una consulta por grupo: son catálogos de una decena de filas.
        var ministryLeaders = await _db.MinistryLeaderships
            .Where(l => l.RevokedAt == null)
            .Select(l => new { l.MinistryId, l.PersonId, l.Person.FirstName, l.Person.LastName })
            .ToListAsync(cancellationToken);

        var societyLeaders = await _db.SocietyLeaderships
            .Where(l => l.RevokedAt == null)
            .Select(l => new { l.SocietyId, l.PersonId, l.Person.FirstName, l.Person.LastName })
            .ToListAsync(cancellationToken);

        var ministries = await _db.Ministries
            .Where(m => m.ChurchId == person.ChurchId)
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name })
            .ToListAsync(cancellationToken);

        var societyRows = await _db.Societies
            .Where(sc => sc.ChurchId == person.ChurchId)
            .OrderBy(sc => sc.Name)
            .Select(sc => new { sc.Id, sc.Name })
            .ToListAsync(cancellationToken);

        var myGroupMemberships = await _db.SocietyMemberships
            .Where(sm => sm.PersonId == person.Id && sm.RevokedAt == null)
            .Select(sm => new { sm.Id, sm.SocietyId })
            .ToListAsync(cancellationToken);

        var membership = await _db.Memberships
            .FirstOrDefaultAsync(m => m.PersonId == person.Id, cancellationToken);

        var familyGroup = await _db.GroupMembers
            .Where(gm => gm.PersonId == person.Id && gm.LeftAt == null)
            .Select(gm => new AdminFamilyGroup(
                gm.FamilyGroupId,
                gm.FamilyGroup.Address,
                gm.JoinedAt,
                gm.FamilyGroup.HostPersonId == person.Id,
                gm.FamilyGroup.LeaderPersonId == person.Id,
                gm.FamilyGroup.Host.FirstName + " " + gm.FamilyGroup.Host.LastName,
                gm.FamilyGroup.Leader.FirstName + " " + gm.FamilyGroup.Leader.LastName))
            .FirstOrDefaultAsync(cancellationToken);

        // Las dos fuentes se consultan por separado y se mezclan en memoria: unirlas en SQL
        // pediría un UNION sobre proyecciones distintas, que EF traduce mal, y son como mucho
        // un par de docenas de filas por lado.
        var deCultos = await _db.ServiceAttendances
            .Where(a => a.PersonId == person.Id)
            .OrderByDescending(a => a.ServiceSession.SessionDate)
            .Take(AsistenciasRecientes)
            .Select(a => new AdminAttendance(
                a.ServiceSession.SessionDate, a.WasPresent, a.ServiceSession.ServiceType.Name))
            .ToListAsync(cancellationToken);

        var deGrupo = await _db.FamilyGroupAttendances
            .Where(a => a.PersonId == person.Id)
            .OrderByDescending(a => a.FamilyGroupMeeting.MeetingDate)
            .Take(AsistenciasRecientes)
            .Select(a => new AdminAttendance(
                a.FamilyGroupMeeting.MeetingDate, a.WasPresent, "Grupo Familiar"))
            .ToListAsync(cancellationToken);

        // Se toman las más recientes de la mezcla y se devuelven en orden ascendente: la fila
        // de puntos se lee de izquierda a derecha, del pasado hacia hoy.
        var asistencia = deCultos.Concat(deGrupo)
            .OrderByDescending(a => a.Date)
            .Take(AsistenciasRecientes)
            .OrderBy(a => a.Date)
            .ToList();

        var response = new PersonAdministrationResponse(
            person.Id,
            person.FirstName,
            person.LastName,
            person.DateOfBirth,
            person.PhoneNumber,
            person.Status == PersonStatus.Active,
            account?.Id,
            account?.Email,
            account is not null,
            account?.IsActive ?? false,
            // Miembro es quien tiene la membresía ACTIVA. Que exista la fila no basta: al dar
            // de baja se conserva para no perder la fecha de ingreso ni quién la registró.
            membership is not null && membership.Status == MembershipStatus.Active,
            membership is not null,
            membership?.JoinedAt,
            [.. roles.Select(r => new AdminRole(
                r.Name,
                r.Description,
                granted.Contains(r.Name, StringComparer.OrdinalIgnoreCase)))],
            [.. positions.Select(p => new AdminPosition(
                p.Id,
                p.Name,
                p.IsExecutiveBody,
                heldPositionIds.Contains(p.Id)))],
            [.. ministries.Select(m =>
            {
                var leader = ministryLeaders.FirstOrDefault(l => l.MinistryId == m.Id);
                return new AdminMinistry(
                    m.Id,
                    m.Name,
                    leader is not null && leader.PersonId == person.Id,
                    leader is null ? null : $"{leader.FirstName} {leader.LastName}");
            })],
            [.. societyRows.Select(sc =>
            {
                var leader = societyLeaders.FirstOrDefault(l => l.SocietyId == sc.Id);
                var membership = myGroupMemberships.FirstOrDefault(sm => sm.SocietyId == sc.Id);
                return new AdminSociety(
                    sc.Id,
                    sc.Name,
                    leader is not null && leader.PersonId == person.Id,
                    leader is null ? null : $"{leader.FirstName} {leader.LastName}",
                    membership is not null,
                    membership?.Id);
            })],
            familyGroup,
            asistencia);

        return Result.Success(response);
    }
}
