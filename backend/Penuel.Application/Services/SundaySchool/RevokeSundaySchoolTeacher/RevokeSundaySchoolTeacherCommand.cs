using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.SundaySchool.RevokeSundaySchoolTeacher;

/// <summary>
/// Retira una asignación de maestro. Se identifica por el Id de la asignación y no por
/// (persona, Sociedad), porque el mismo par puede tener varias filas a lo largo del tiempo
/// y hay que poder señalar exactamente cuál se revoca.
/// </summary>
public sealed record RevokeSundaySchoolTeacherCommand(Guid AssignmentId)
    : IRequest<Result>, IRequirePastor;

public sealed class RevokeSundaySchoolTeacherCommandValidator
    : AbstractValidator<RevokeSundaySchoolTeacherCommand>
{
    public RevokeSundaySchoolTeacherCommandValidator()
    {
        RuleFor(c => c.AssignmentId).NotEmpty()
            .WithMessage("El identificador de la asignación es obligatorio.");
    }
}

public sealed class RevokeSundaySchoolTeacherCommandHandler
    : IRequestHandler<RevokeSundaySchoolTeacherCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RevokeSundaySchoolTeacherCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        RevokeSundaySchoolTeacherCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _db.SundaySchoolTeachingAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure(ServiceErrors.Teaching.NotFound(request.AssignmentId));
        }

        if (!assignment.IsActive())
        {
            return Result.Failure(ServiceErrors.Teaching.AlreadyRevoked);
        }

        assignment.Revoke(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
