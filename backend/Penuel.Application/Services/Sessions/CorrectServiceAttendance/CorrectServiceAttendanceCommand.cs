using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.Sessions.CorrectServiceAttendance;

/// <summary>
/// Corrige una asistencia ya capturada. Regla 7.1: se corrige con un UPDATE controlado,
/// nunca borrando y recapturando.
/// </summary>
public sealed record CorrectServiceAttendanceCommand(
    Guid ServiceAttendanceId,
    bool WasPresent,
    bool? WasPunctual,
    bool? BroughtBible,
    int? ChaptersRead) : IRequest<Result>, IRequireSundaySchoolRecorder;

public sealed class CorrectServiceAttendanceCommandValidator
    : AbstractValidator<CorrectServiceAttendanceCommand>
{
    public CorrectServiceAttendanceCommandValidator()
    {
        RuleFor(c => c.ServiceAttendanceId).NotEmpty()
            .WithMessage("El identificador de la asistencia es obligatorio.");

        RuleFor(c => c.ChaptersRead)
            .GreaterThanOrEqualTo(0)
            .When(c => c.ChaptersRead.HasValue)
            .WithMessage("Los capítulos leídos no pueden ser negativos.");
    }
}

public sealed class CorrectServiceAttendanceCommandHandler
    : IRequestHandler<CorrectServiceAttendanceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CorrectServiceAttendanceCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        CorrectServiceAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await _db.ServiceAttendances
            .Include(a => a.ServiceSession)
            .ThenInclude(s => s.ServiceType)
            .FirstOrDefaultAsync(a => a.Id == request.ServiceAttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure(ServiceErrors.Attendance.NotFound(request.ServiceAttendanceId));
        }

        // Regla 7.3: los campos granulares solo existen donde el tipo de servicio los admite.
        var granulares = request.WasPunctual.HasValue
                         || request.BroughtBible.HasValue
                         || request.ChaptersRead.HasValue;

        if (granulares && !attendance.ServiceSession.ServiceType.RequiresSocietyGrouping)
        {
            return Result.Failure(ServiceErrors.Attendance.GranularFieldsNotAllowed);
        }

        attendance.Correct(
            request.WasPresent,
            request.WasPunctual,
            request.BroughtBible,
            request.ChaptersRead,
            _currentUser.PersonId,
            _clock.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
