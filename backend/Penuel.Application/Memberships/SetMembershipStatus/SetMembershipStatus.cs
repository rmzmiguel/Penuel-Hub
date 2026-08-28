using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Memberships.SetMembershipStatus;

/// <summary>
/// Da de baja o restituye la membresía oficial de una persona.
/// </summary>
/// <remarks>
/// NUNCA borra la fila ni crea una nueva, y eso no es una preferencia: la regla 7.2 dice que
/// una persona tiene como máximo UN <c>Membership</c>, así que la baja sólo puede ser una
/// transición de estado sobre la fila que ya existe. Si se borrara, se perdería la fecha de
/// ingreso y quién la registró — justo lo que un libro de miembros no puede perder.
///
/// La baja usa <c>MarkAsFormerMember</c> y no <c>Deactivate</c>: el Dominio distingue entre
/// "inactivo" y "ya no es miembro", y una baja administrativa es lo segundo. El estado
/// <c>Inactive</c> queda libre para lo que significa de verdad, que es una pausa.
/// </remarks>
public sealed record SetMembershipStatusCommand(Guid PersonId, bool IsMember)
    : IRequest<Result>, IRequirePastor;

public sealed class SetMembershipStatusCommandValidator : AbstractValidator<SetMembershipStatusCommand>
{
    public SetMembershipStatusCommandValidator()
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class SetMembershipStatusCommandHandler
    : IRequestHandler<SetMembershipStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SetMembershipStatusCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(SetMembershipStatusCommand request, CancellationToken cancellationToken)
    {
        var membership = await _db.Memberships
            .FirstOrDefaultAsync(m => m.PersonId == request.PersonId, cancellationToken);

        if (membership is null)
        {
            // Sin fila no hay nada que restituir: para hacer miembro a alguien por primera vez
            // está CreateMembership, que es donde se registra la fecha de ingreso.
            return Result.Failure(ApplicationErrors.Membership.NotFound);
        }

        var esMiembro = membership.Status == MembershipStatus.Active;

        if (esMiembro == request.IsMember)
        {
            return Result.Failure(request.IsMember
                ? ApplicationErrors.Membership.AlreadyActive
                : ApplicationErrors.Membership.AlreadyRevoked);
        }

        if (request.IsMember)
        {
            membership.Activate(_clock.UtcNow);
        }
        else
        {
            membership.MarkAsFormerMember(_clock.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
