using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Memberships.CreateMembership;

/// <summary>
/// Convierte a una persona en miembro oficial de la Comunidad Cristiana Penuel.
/// </summary>
/// <remarks>
/// Es la decisión administrativa que la Sección 3.2 describe como separada y posterior:
/// asistir a un Grupo Familiar no hace miembro a nadie. La existencia de esta fila es lo
/// único que determina la membresía.
/// </remarks>
public sealed record CreateMembershipCommand(
    Guid PersonId,
    DateOnly? JoinedAt) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateMembershipCommandValidator : AbstractValidator<CreateMembershipCommand>
{
    public CreateMembershipCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");

        RuleFor(c => c.JoinedAt)
            .LessThanOrEqualTo(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .When(c => c.JoinedAt.HasValue)
            .WithMessage("La fecha de ingreso no puede estar en el futuro.");
    }
}

public sealed class CreateMembershipCommandHandler
    : IRequestHandler<CreateMembershipCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateMembershipCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        CreateMembershipCommand request,
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

        // Regla 7.2: una Person tiene como máximo un Membership.
        var alreadyMember = await _db.Memberships
            .AnyAsync(m => m.PersonId == request.PersonId, cancellationToken);

        if (alreadyMember)
        {
            return Result.Failure<Guid>(ApplicationErrors.Membership.AlreadyExists);
        }

        var membership = Domain.Entities.Membership.Create(
            person.Id,
            person.ChurchId,
            request.JoinedAt,
            _currentUser.PersonId,
            _clock.UtcNow);

        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(membership.Id);
    }
}
