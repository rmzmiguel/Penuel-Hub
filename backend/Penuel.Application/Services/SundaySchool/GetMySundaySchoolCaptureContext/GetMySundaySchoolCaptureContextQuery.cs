using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.SundaySchool.GetMySundaySchoolCaptureContext;

/// <summary>Qué debe preguntarle el frontend a quien va a capturar (Sección 8.2).</summary>
public enum SundaySchoolCaptureMode
{
    /// <summary>Tiene exactamente un grupo fijo: no hay nada que preguntar, se va directo al reporte.</summary>
    SingleFixedGroup = 0,

    /// <summary>Tiene varios grupos fijos (clases combinadas): hay que preguntarle cuál va a reportar.</summary>
    MultipleFixedGroups = 1,

    /// <summary>
    /// No tiene grupo fijo — o es sustituto flotante, o solo digitaliza reportes ajenos.
    /// Hay que preguntarle primero la Sociedad y luego quién dio la clase.
    /// </summary>
    NoFixedGroup = 2
}

public sealed record SundaySchoolTeacherOption(
    Guid PersonId,
    string FirstName,
    string LastName,
    /// <summary>Distingue al titular del grupo del sustituto flotante, para que la UI pueda ordenarlos.</summary>
    bool HasFixedGroup);

public sealed record SundaySchoolSocietyOption(
    Guid SocietyId,
    string SocietyName,
    IReadOnlyCollection<SundaySchoolTeacherOption> TeacherCandidates);

public sealed record MySundaySchoolCaptureContextResponse(
    Guid PersonId,
    SundaySchoolCaptureMode Mode,
    bool IsFloatingSubstitute,
    IReadOnlyCollection<SundaySchoolSocietyOption> MySocieties,
    IReadOnlyCollection<SundaySchoolSocietyOption> AllSocieties);

/// <summary>
/// Extensión del espíritu de <c>GetMyCapabilitiesQuery</c> del Core, específica de esta rama:
/// el frontend NUNCA asume quién da qué clase — lo pregunta contra los datos vigentes.
/// </summary>
/// <remarks>
/// <c>AllSocieties</c> se devuelve siempre, no solo en el tercer escenario: aunque alguien
/// tenga grupo fijo, puede haber cubierto a otro maestro ese domingo, y obligarlo a una
/// segunda llamada para eso sería fricción sin motivo. Los candidatos de cada Sociedad
/// incluyen a sus maestros fijos y a TODOS los sustitutos flotantes.
/// </remarks>
public sealed record GetMySundaySchoolCaptureContextQuery
    : IRequest<Result<MySundaySchoolCaptureContextResponse>>, IRequireSundaySchoolRecorder;

public sealed class GetMySundaySchoolCaptureContextQueryHandler
    : IRequestHandler<GetMySundaySchoolCaptureContextQuery, Result<MySundaySchoolCaptureContextResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMySundaySchoolCaptureContextQueryHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MySundaySchoolCaptureContextResponse>> Handle(
        GetMySundaySchoolCaptureContextQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid personId)
        {
            return Result.Failure<MySundaySchoolCaptureContextResponse>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, personId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<MySundaySchoolCaptureContextResponse>(
                ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        // Todas las asignaciones ACTIVAS de la iglesia: son pocas y se necesitan enteras
        // para armar los candidatos de cada Sociedad.
        var assignments = await _db.SundaySchoolTeachingAssignments
            .Where(a => a.RevokedAt == null)
            .Select(a => new
            {
                a.SocietyId,
                a.PersonId,
                a.Person.FirstName,
                a.Person.LastName
            })
            .ToListAsync(cancellationToken);

        var societies = await _db.Societies
            .Where(s => s.ChurchId == churchId.Value)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        // Sustitutos flotantes: asignación activa SIN Sociedad. Son candidatos para cualquier grupo.
        var floating = assignments
            .Where(a => a.SocietyId is null)
            .Select(a => new SundaySchoolTeacherOption(a.PersonId, a.FirstName, a.LastName, false))
            .ToList();

        SundaySchoolSocietyOption BuildOption(Guid societyId, string societyName)
        {
            var fixedTeachers = assignments
                .Where(a => a.SocietyId == societyId)
                .Select(a => new SundaySchoolTeacherOption(a.PersonId, a.FirstName, a.LastName, true));

            var candidates = fixedTeachers
                .Concat(floating)
                .GroupBy(t => t.PersonId)
                // Si alguien figura como titular y como flotante, gana "titular".
                .Select(g => g.OrderByDescending(t => t.HasFixedGroup).First())
                .OrderBy(t => t.LastName)
                .ThenBy(t => t.FirstName)
                .ToList();

            return new SundaySchoolSocietyOption(societyId, societyName, candidates);
        }

        var allSocieties = societies
            .Select(s => BuildOption(s.Id, s.Name))
            .ToList();

        var myFixedSocietyIds = assignments
            .Where(a => a.PersonId == personId && a.SocietyId is not null)
            .Select(a => a.SocietyId!.Value)
            .Distinct()
            .ToHashSet();

        var mySocieties = allSocieties
            .Where(s => myFixedSocietyIds.Contains(s.SocietyId))
            .ToList();

        var isFloatingSubstitute = assignments
            .Any(a => a.PersonId == personId && a.SocietyId is null);

        var mode = mySocieties.Count switch
        {
            1 => SundaySchoolCaptureMode.SingleFixedGroup,
            > 1 => SundaySchoolCaptureMode.MultipleFixedGroups,
            _ => SundaySchoolCaptureMode.NoFixedGroup
        };

        return Result.Success(new MySundaySchoolCaptureContextResponse(
            personId, mode, isFloatingSubstitute, mySocieties, allSocieties));
    }
}
