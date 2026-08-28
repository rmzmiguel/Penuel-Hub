using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Ministries.AssignMinistryLeader;

/// <summary>
/// Asigna el líder de un ministerio.
/// </summary>
/// <remarks>
/// Regla 7.11: a lo sumo un liderazgo ACTIVO por ministerio. Si ya hay uno vigente, esta
/// operación falla con conflicto en lugar de reemplazarlo en silencio — reasignar es
/// revocar y luego asignar, dos actos que quedan registrados por separado en la auditoría.
/// Regla 7.13: no hay límite sobre cuántos ministerios puede liderar una misma persona.
/// </remarks>
public sealed record AssignMinistryLeaderCommand(
    Guid MinistryId,
    Guid PersonId) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AssignMinistryLeaderCommandValidator : AbstractValidator<AssignMinistryLeaderCommand>
{
    public AssignMinistryLeaderCommandValidator()
    {
        RuleFor(c => c.MinistryId).NotEmpty().WithMessage("El identificador del ministerio es obligatorio.");
        RuleFor(c => c.PersonId).NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class AssignMinistryLeaderCommandHandler
    : IRequestHandler<AssignMinistryLeaderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AssignMinistryLeaderCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        AssignMinistryLeaderCommand request,
        CancellationToken cancellationToken)
    {
        var ministryExists = await _db.Ministries
            .AnyAsync(m => m.Id == request.MinistryId, cancellationToken);

        if (!ministryExists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Ministry.NotFound(request.MinistryId));
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

        var hasActiveLeader = await _db.MinistryLeaderships.AnyAsync(
            l => l.MinistryId == request.MinistryId && l.RevokedAt == null,
            cancellationToken);

        if (hasActiveLeader)
        {
            return Result.Failure<Guid>(ApplicationErrors.Ministry.AlreadyHasActiveLeader);
        }

        var leadership = MinistryLeadership.Assign(
            request.MinistryId, request.PersonId, _currentUser.PersonId, _clock.UtcNow);

        _db.MinistryLeaderships.Add(leadership);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(leadership.Id);
    }
}
