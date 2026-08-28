using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Persons.DeactivatePerson;

/// <summary>Borrado lógico de una persona (regla 7.3). La fila nunca se elimina.</summary>
public sealed record DeactivatePersonCommand(Guid PersonId) : IRequest<Result>, IRequirePastor;

public sealed class DeactivatePersonCommandValidator : AbstractValidator<DeactivatePersonCommand>
{
    public DeactivatePersonCommandValidator()
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class DeactivatePersonCommandHandler : IRequestHandler<DeactivatePersonCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DeactivatePersonCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(DeactivatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure(ApplicationErrors.Person.NotFound(request.PersonId));
        }

        if (person.Status == PersonStatus.Deceased)
        {
            return Result.Failure(ApplicationErrors.Person.Deceased);
        }

        if (person.Status == PersonStatus.Inactive)
        {
            return Result.Failure(ApplicationErrors.Person.AlreadyInactive);
        }

        person.Deactivate(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
