using FluentValidation;
using MediatR;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Persons.RegisterPerson;

/// <summary>
/// Registra a una persona en el directorio. NO la convierte en miembro oficial ni le da
/// acceso al sistema: son tres ejes independientes (Sección 3).
/// </summary>
public sealed record RegisterPersonCommand(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class RegisterPersonCommandValidator : AbstractValidator<RegisterPersonCommand>
{
    public RegisterPersonCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden exceder 100 caracteres.");

        RuleFor(c => c.PhoneNumber)
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres.");

        RuleFor(c => c.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .When(c => c.DateOfBirth.HasValue)
            .WithMessage("La fecha de nacimiento no puede estar en el futuro.");
    }
}

public sealed class RegisterPersonCommandHandler : IRequestHandler<RegisterPersonCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RegisterPersonCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(RegisterPersonCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, actorId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var person = Person.Register(
            churchId.Value,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.PhoneNumber,
            actorId,
            _clock.UtcNow);

        _db.Persons.Add(person);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(person.Id);
    }
}
