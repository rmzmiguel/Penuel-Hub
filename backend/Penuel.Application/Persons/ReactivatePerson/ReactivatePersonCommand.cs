using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Persons.ReactivatePerson;

/// <summary>
/// Reactiva a una persona. Contempla el caso real de la Sección 3.1: alguien que asistió,
/// se fue, y años después regresa.
/// </summary>
public sealed record ReactivatePersonCommand(Guid PersonId) : IRequest<Result>, IRequirePastor;

public sealed class ReactivatePersonCommandValidator : AbstractValidator<ReactivatePersonCommand>
{
    public ReactivatePersonCommandValidator()
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class ReactivatePersonCommandHandler : IRequestHandler<ReactivatePersonCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ReactivatePersonCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(ReactivatePersonCommand request, CancellationToken cancellationToken)
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

        if (person.Status == PersonStatus.Active)
        {
            return Result.Failure(ApplicationErrors.Person.AlreadyActive);
        }

        person.Reactivate(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
