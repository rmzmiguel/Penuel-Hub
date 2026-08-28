using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;
using Penuel.Domain.Enums;

namespace Penuel.Application.Services.SundaySchool.AssignSundaySchoolTeacher;

/// <summary>
/// Registra que una persona da la clase de un grupo de Escuela Dominical.
/// </summary>
/// <remarks>
/// <c>SocietyId</c> nulo significa <b>maestro sustituto sin grupo fijo</b>, disponible para
/// cualquier Sociedad (Sección 6.5).
/// NO restringe cuántos maestros tiene un grupo ni cuántos grupos acumula una persona
/// (regla 7.7) — lo único que se rechaza es el duplicado exacto: la misma persona con
/// asignación activa al mismo grupo dos veces, que no significaría nada.
/// Es un hecho organizacional, no un permiso: no otorga ningún <c>Role</c> (regla 7.8).
/// </remarks>
public sealed record AssignSundaySchoolTeacherCommand(
    Guid? SocietyId,
    Guid PersonId) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AssignSundaySchoolTeacherCommandValidator
    : AbstractValidator<AssignSundaySchoolTeacherCommand>
{
    public AssignSundaySchoolTeacherCommandValidator()
    {
        RuleFor(c => c.PersonId).NotEmpty()
            .WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class AssignSundaySchoolTeacherCommandHandler
    : IRequestHandler<AssignSundaySchoolTeacherCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AssignSundaySchoolTeacherCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        AssignSundaySchoolTeacherCommand request,
        CancellationToken cancellationToken)
    {
        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(request.PersonId));
        }

        if (person.Status != PersonStatus.Active)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotActive);
        }

        if (request.SocietyId is Guid societyId
            && !await _db.Societies.AnyAsync(s => s.Id == societyId, cancellationToken))
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.NotFound(societyId));
        }

        var duplicated = await _db.SundaySchoolTeachingAssignments.AnyAsync(
            a => a.PersonId == request.PersonId
                 && a.SocietyId == request.SocietyId
                 && a.RevokedAt == null,
            cancellationToken);

        if (duplicated)
        {
            return Result.Failure<Guid>(ServiceErrors.Teaching.AlreadyAssigned);
        }

        var assignment = SundaySchoolTeachingAssignment.Assign(
            request.SocietyId, request.PersonId, _currentUser.PersonId, _clock.UtcNow);

        _db.SundaySchoolTeachingAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(assignment.Id);
    }
}
