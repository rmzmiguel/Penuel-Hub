using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Societies.AssignSocietyLeader;

/// <summary>
/// Asigna el líder de una sociedad.
/// </summary>
/// <remarks>
/// Regla 7.11: a lo sumo un liderazgo ACTIVO por sociedad. Si ya hay uno vigente, esta
/// operación falla con conflicto en lugar de reemplazarlo en silencio — reasignar es
/// revocar y luego asignar, dos actos que quedan registrados por separado en la auditoría.
/// Regla 7.13: no hay límite sobre cuántas sociedades puede liderar una misma persona.
/// </remarks>
public sealed record AssignSocietyLeaderCommand(
    Guid SocietyId,
    Guid PersonId) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AssignSocietyLeaderCommandValidator : AbstractValidator<AssignSocietyLeaderCommand>
{
    public AssignSocietyLeaderCommandValidator()
    {
        RuleFor(c => c.SocietyId).NotEmpty().WithMessage("El identificador de la sociedad es obligatorio.");
        RuleFor(c => c.PersonId).NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class AssignSocietyLeaderCommandHandler
    : IRequestHandler<AssignSocietyLeaderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AssignSocietyLeaderCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        AssignSocietyLeaderCommand request,
        CancellationToken cancellationToken)
    {
        var societyExists = await _db.Societies
            .AnyAsync(m => m.Id == request.SocietyId, cancellationToken);

        if (!societyExists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.NotFound(request.SocietyId));
        }

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

        var hasActiveLeader = await _db.SocietyLeaderships.AnyAsync(
            l => l.SocietyId == request.SocietyId && l.RevokedAt == null,
            cancellationToken);

        if (hasActiveLeader)
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.AlreadyHasActiveLeader);
        }

        var leadership = SocietyLeadership.Assign(
            request.SocietyId, request.PersonId, _currentUser.PersonId, _clock.UtcNow);

        _db.SocietyLeaderships.Add(leadership);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(leadership.Id);
    }
}
