using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Ministries.RevokeMinistryLeader;

/// <summary>Retira el liderazgo vigente de un ministerio. La fila se conserva, revocada (regla 7.3).</summary>
public sealed record RevokeMinistryLeaderCommand(Guid MinistryId) : IRequest<Result>, IRequirePastor;

public sealed class RevokeMinistryLeaderCommandValidator : AbstractValidator<RevokeMinistryLeaderCommand>
{
    public RevokeMinistryLeaderCommandValidator()
    {
        RuleFor(c => c.MinistryId).NotEmpty().WithMessage("El identificador del ministerio es obligatorio.");
    }
}

public sealed class RevokeMinistryLeaderCommandHandler : IRequestHandler<RevokeMinistryLeaderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RevokeMinistryLeaderCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokeMinistryLeaderCommand request, CancellationToken cancellationToken)
    {
        var ministryExists = await _db.Ministries
            .AnyAsync(m => m.Id == request.MinistryId, cancellationToken);

        if (!ministryExists)
        {
            return Result.Failure(ApplicationErrors.Ministry.NotFound(request.MinistryId));
        }

        var leadership = await _db.MinistryLeaderships.FirstOrDefaultAsync(
            l => l.MinistryId == request.MinistryId && l.RevokedAt == null,
            cancellationToken);

        if (leadership is null)
        {
            return Result.Failure(ApplicationErrors.Ministry.NoActiveLeader);
        }

        leadership.Revoke(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
