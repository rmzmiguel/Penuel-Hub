using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;

namespace Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;

/// <summary>Una fila de la hoja de reporte: una persona y su detalle de esa clase.</summary>
public sealed record SundaySchoolAttendanceInput(
    Guid PersonId,
    bool WasPresent,
    bool? WasPunctual,
    bool? BroughtBible,
    int? ChaptersRead);

/// <summary>
/// Levanta el reporte completo de un grupo de Escuela Dominical: la sesión, su ofrenda y
/// todas las asistencias, en una sola transacción.
/// </summary>
/// <remarks>
/// Es el equivalente digital de la hoja física que hoy llena el maestro. Que Damas y Varones
/// se impartan juntas no necesita representarse: simplemente se levantan dos reportes ese
/// domingo, y el mismo <c>TeacherPersonId</c> aparece en los dos (Sección 4).
/// </remarks>
public sealed record SubmitSundaySchoolReportCommand(
    Guid ServiceTypeId,
    Guid SocietyId,
    DateOnly SessionDate,
    decimal TotalOffering,
    Guid? TeacherPersonId,
    IReadOnlyList<SundaySchoolAttendanceInput> Attendances)
    : IRequest<Result<Guid>>, IRequireSundaySchoolRecorder;

public sealed class SubmitSundaySchoolReportCommandValidator
    : AbstractValidator<SubmitSundaySchoolReportCommand>
{
    public SubmitSundaySchoolReportCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.ServiceTypeId).NotEmpty()
            .WithMessage("El tipo de servicio es obligatorio.");

        RuleFor(c => c.SocietyId).NotEmpty()
            .WithMessage("La Sociedad es obligatoria en Escuela Dominical.");

        RuleFor(c => c.SessionDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .WithMessage("No se puede levantar un reporte con fecha futura.");

        RuleFor(c => c.TotalOffering)
            .GreaterThanOrEqualTo(0).WithMessage("La ofrenda no puede ser negativa.");

        RuleFor(c => c.Attendances).NotNull()
            .WithMessage("La lista de asistencia es obligatoria (puede ir vacía, pero no nula).");

        RuleForEach(c => c.Attendances).ChildRules(a =>
        {
            a.RuleFor(x => x.PersonId).NotEmpty()
                .WithMessage("Cada asistencia debe indicar a la persona.");

            a.RuleFor(x => x.ChaptersRead)
                .GreaterThanOrEqualTo(0)
                .When(x => x.ChaptersRead.HasValue)
                .WithMessage("Los capítulos leídos no pueden ser negativos.");
        });
    }
}

public sealed class SubmitSundaySchoolReportCommandHandler
    : IRequestHandler<SubmitSundaySchoolReportCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SubmitSundaySchoolReportCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        SubmitSundaySchoolReportCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var serviceType = await _db.ServiceTypes
            .FirstOrDefaultAsync(t => t.Id == request.ServiceTypeId, cancellationToken);

        if (serviceType is null)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.NotFound(request.ServiceTypeId));
        }

        // Regla 7.4: solo un tipo agrupado por Sociedad admite un reporte por grupo.
        if (!serviceType.RequiresSocietyGrouping)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.DoesNotRequireSocietyGrouping);
        }

        var societyExists = await _db.Societies
            .AnyAsync(s => s.Id == request.SocietyId, cancellationToken);

        if (!societyExists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.NotFound(request.SocietyId));
        }

        // El maestro puede ser CUALQUIER persona: no se exige que tenga asignación previa,
        // porque cubrir a alguien sin asignación formal es normal (Sección 6.2).
        if (request.TeacherPersonId is Guid teacherId
            && !await _db.Persons.AnyAsync(p => p.Id == teacherId, cancellationToken))
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(teacherId));
        }

        var duplicated = request.Attendances
            .GroupBy(a => a.PersonId)
            .Any(g => g.Count() > 1);

        if (duplicated)
        {
            return Result.Failure<Guid>(ServiceErrors.Attendance.DuplicatePersonInReport);
        }

        var personIds = request.Attendances.Select(a => a.PersonId).Distinct().ToArray();

        var existingCount = await _db.Persons
            .CountAsync(p => personIds.Contains(p.Id), cancellationToken);

        if (existingCount != personIds.Length)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "Person.NotFound", "Alguna de las personas del reporte no existe."));
        }

        var alreadyReported = await _db.ServiceSessions.AnyAsync(
            s => s.ServiceTypeId == request.ServiceTypeId
                 && s.SessionDate == request.SessionDate
                 && s.SocietyId == request.SocietyId,
            cancellationToken);

        if (alreadyReported)
        {
            return Result.Failure<Guid>(ServiceErrors.Session.AlreadyExistsForSociety);
        }

        var now = _clock.UtcNow;

        var session = ServiceSession.ForSundaySchool(
            request.ServiceTypeId,
            request.SocietyId,
            request.SessionDate,
            request.TotalOffering,
            request.TeacherPersonId,
            actorId,
            now);

        _db.ServiceSessions.Add(session);

        foreach (var input in request.Attendances)
        {
            _db.ServiceAttendances.Add(ServiceAttendance.Record(
                session.Id,
                input.PersonId,
                input.WasPresent,
                input.WasPunctual,
                input.BroughtBible,
                input.ChaptersRead,
                now));
        }

        // Un solo SaveChanges: la sesión y todas sus asistencias entran juntas o no entra nada.
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(session.Id);
    }
}
