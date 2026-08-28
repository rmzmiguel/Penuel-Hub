using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Societies.RevokeSocietyLeader;

/// <summary>Retira el liderazgo vigente de una sociedad. La fila se conserva, revocada (regla 7.3).</summary>
public sealed record RevokeSocietyLeaderCommand(Guid SocietyId) : IRequest<Result>, IRequirePastor;

public sealed class RevokeSocietyLeaderCommandValidator : AbstractValidator<RevokeSocietyLeaderCommand>
{
    public RevokeSocietyLeaderCommandValidator()
    {
        RuleFor(c => c.SocietyId).NotEmpty().WithMessage("El identificador de la sociedad es obligatorio.");
    }
}

public sealed class RevokeSocietyLeaderCommandHandler : IRequestHandler<RevokeSocietyLeaderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RevokeSocietyLeaderCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokeSocietyLeaderCommand request, CancellationToken cancellationToken)
    {
        var societyExists = await _db.Societies
            .AnyAsync(m => m.Id == request.SocietyId, cancellationToken);

        if (!societyExists)
        {
            return Result.Failure(ApplicationErrors.Society.NotFound(request.SocietyId));
        }

        var leadership = await _db.SocietyLeaderships.FirstOrDefaultAsync(
            l => l.SocietyId == request.SocietyId && l.RevokedAt == null,
            cancellationToken);

        if (leadership is null)
        {
            return Result.Failure(ApplicationErrors.Society.NoActiveLeader);
        }

        leadership.Revoke(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
