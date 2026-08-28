using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Societies.AddSocietyMember;

/// <summary>
/// Registra que una persona pertenece a una Sociedad. Acto organizacional: solo el Pastor.
/// </summary>
/// <remarks>
/// No convierte a nadie en miembro oficial de la iglesia (eso es <c>Membership</c>, Sección 3.2
/// del Core) ni le da acceso al sistema. Pertenecer a un grupo de Escuela Dominical sin ser
/// miembro oficial es el caso normal de quien está siendo alcanzado.
/// </remarks>
public sealed record AddSocietyMemberCommand(Guid SocietyId, Guid PersonId)
    : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AddSocietyMemberCommandValidator : AbstractValidator<AddSocietyMemberCommand>
{
    public AddSocietyMemberCommandValidator()
    {
        RuleFor(c => c.SocietyId).NotEmpty().WithMessage("La Sociedad es obligatoria.");
        RuleFor(c => c.PersonId).NotEmpty().WithMessage("La persona es obligatoria.");
    }
}

public sealed class AddSocietyMemberCommandHandler
    : IRequestHandler<AddSocietyMemberCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AddSocietyMemberCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        AddSocietyMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _db.Societies.AnyAsync(s => s.Id == request.SocietyId, cancellationToken))
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

        var alreadyMember = await _db.SocietyMemberships.AnyAsync(
            m => m.SocietyId == request.SocietyId
                 && m.PersonId == request.PersonId
                 && m.RevokedAt == null,
            cancellationToken);

        if (alreadyMember)
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.MemberAlreadyAdded);
        }

        var membership = SocietyMembership.Add(
            request.SocietyId, request.PersonId, _currentUser.PersonId, _clock.UtcNow);

        _db.SocietyMemberships.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(membership.Id);
    }
}
