using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Societies.RemoveSocietyMember;

/// <summary>
/// Da de baja la pertenencia de una persona a una Sociedad. Regla 7.3 del Core: la fila se
/// conserva revocada, para que quede el historial de que perteneció.
/// </summary>
public sealed record RemoveSocietyMemberCommand(Guid SocietyMembershipId)
    : IRequest<Result>, IRequirePastor;

public sealed class RemoveSocietyMemberCommandValidator
    : AbstractValidator<RemoveSocietyMemberCommand>
{
    public RemoveSocietyMemberCommandValidator()
    {
        RuleFor(c => c.SocietyMembershipId).NotEmpty()
            .WithMessage("El identificador de la pertenencia es obligatorio.");
    }
}

public sealed class RemoveSocietyMemberCommandHandler
    : IRequestHandler<RemoveSocietyMemberCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RemoveSocietyMemberCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        RemoveSocietyMemberCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await _db.SocietyMemberships
            .FirstOrDefaultAsync(m => m.Id == request.SocietyMembershipId, cancellationToken);

        if (membership is null)
        {
            return Result.Failure(
                ApplicationErrors.Society.MembershipNotFound(request.SocietyMembershipId));
        }

        if (!membership.IsActive())
        {
            return Result.Failure(ApplicationErrors.Society.MembershipAlreadyRemoved);
        }

        membership.Remove(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
