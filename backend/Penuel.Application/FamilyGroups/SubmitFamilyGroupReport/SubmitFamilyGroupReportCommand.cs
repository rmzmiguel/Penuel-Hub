using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.SubmitFamilyGroupReport;

/// <summary>Una persona del grupo y si estuvo. Un solo dato, a propósito.</summary>
public sealed record FamilyGroupAttendanceInput(Guid PersonId, bool WasPresent);

/// <summary>
/// Levanta el reporte de una reunión: fecha, ofrenda total y la lista de asistencia
/// (Sección 8.2).
/// </summary>
/// <remarks>
/// Es lo que sustituye la hoja de papel, y por eso el formulario es de dos campos y una lista
/// de casillas. Nada de puntualidad, Biblia ni capítulos: eso es de Escuela Dominical.
///
/// La reunión y todas sus asistencias se guardan en UNA transacción — un solo
/// <c>SaveChangesAsync</c>. Media lista guardada sería peor que ninguna, porque nadie sabría
/// dónde se cortó.
/// </remarks>
public sealed record SubmitFamilyGroupReportCommand(
    Guid FamilyGroupId,
    DateOnly MeetingDate,
    decimal TotalOffering,
    IReadOnlyCollection<FamilyGroupAttendanceInput> Attendances)
    : IRequest<Result<Guid>>, IRequireFamilyGroupOwnership;

public sealed class SubmitFamilyGroupReportCommandValidator
    : AbstractValidator<SubmitFamilyGroupReportCommand>
{
    public SubmitFamilyGroupReportCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");

        // Regla 7.8, mismo criterio que la rama de Servicios. Nótese que NO se valida el día
        // de la semana: el grupo puede reunirse cualquier día si esa semana hizo falta (7.7).
        RuleFor(c => c.MeetingDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .WithMessage("La fecha de la reunión no puede estar en el futuro.");

        RuleFor(c => c.TotalOffering)
            .GreaterThanOrEqualTo(0).WithMessage("La ofrenda no puede ser negativa.");

        RuleFor(c => c.Attendances)
            .NotNull().WithMessage("Falta la lista de asistencia.");
    }
}

public sealed class SubmitFamilyGroupReportCommandHandler
    : IRequestHandler<SubmitFamilyGroupReportCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SubmitFamilyGroupReportCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        SubmitFamilyGroupReportCommand request,
        CancellationToken cancellationToken)
    {
        var acceso = await FamilyGroupPermissions.LoadOwnedAsync(
            _db, _currentUser, request.FamilyGroupId, cancellationToken);

        if (!acceso.IsSuccess)
        {
            return Result.Failure<Guid>(acceso.Error!);
        }

        var group = acceso.Value;

        if (group.Status != FamilyGroupStatus.Active)
        {
            return Result.Failure<Guid>(FamilyGroupErrors.Group.NotActive);
        }

        var entradas = request.Attendances;

        if (entradas.Select(a => a.PersonId).Distinct().Count() != entradas.Count)
        {
            return Result.Failure<Guid>(FamilyGroupErrors.Meeting.DuplicateAttendee);
        }

        var yaHay = await _db.FamilyGroupMeetings.AnyAsync(
            m => m.FamilyGroupId == group.Id && m.MeetingDate == request.MeetingDate,
            cancellationToken);

        if (yaHay)
        {
            return Result.Failure<Guid>(FamilyGroupErrors.Meeting.AlreadyReported);
        }

        // La lista solo puede hablar de quien está en el grupo. Si llega alguien de fuera es
        // que la pantalla trabajó con una lista vieja, y guardarlo silenciosamente dejaría una
        // asistencia que no cuadra con ningún integrante.
        var integrantes = await _db.GroupMembers
            .Where(m => m.FamilyGroupId == group.Id && m.LeftAt == null)
            .Select(m => m.PersonId)
            .ToListAsync(cancellationToken);

        if (entradas.Any(a => !integrantes.Contains(a.PersonId)))
        {
            return Result.Failure<Guid>(FamilyGroupErrors.Meeting.AttendeeNotInGroup);
        }

        var now = _clock.UtcNow;

        var meeting = FamilyGroupMeeting.Create(
            group.Id, request.MeetingDate, request.TotalOffering,
            _currentUser.PersonId!.Value, now);

        foreach (var entrada in entradas)
        {
            meeting.AddAttendance(entrada.PersonId, entrada.WasPresent, now);
        }

        _db.FamilyGroupMeetings.Add(meeting);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(meeting.Id);
    }
}
