using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Persons.UpdatePerson;

/// <summary>
/// Corrige los datos de una persona: nombre, apellidos, fecha de nacimiento y teléfono.
/// </summary>
/// <remarks>
/// El Dominio ya sabía hacerlo —<c>Person.UpdateDetails()</c> existe desde el Core— pero no
/// había forma de invocarlo, así que un apellido mal tecleado se quedaba mal para siempre.
///
/// Deliberadamente NO toca nada de lo que la persona ES dentro de la iglesia: ni membresía,
/// ni cargos, ni roles, ni grupos. Eso vive en el panel de administración y cada cosa tiene su
/// propio comando. Este arregla la ficha, no la posición.
/// </remarks>
public sealed record UpdatePersonCommand(
    Guid PersonId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber) : IRequest<Result>, IRequirePastor;

public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");

        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden exceder 100 caracteres.");

        RuleFor(c => c.DateOfBirth)
            .LessThan(_ => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .When(c => c.DateOfBirth.HasValue)
            .WithMessage("La fecha de nacimiento no puede estar en el futuro.");

        RuleFor(c => c.PhoneNumber)
            .MaximumLength(30).When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
            .WithMessage("El teléfono no puede exceder 30 caracteres.");
    }
}

public sealed class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdatePersonCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure(ApplicationErrors.Person.NotFound(request.PersonId));
        }

        // Una persona fallecida conserva su ficha tal cual quedó. Corregirla sería tocar un
        // registro que ya nadie va a volver a vivir.
        if (person.Status == PersonStatus.Deceased)
        {
            return Result.Failure(ApplicationErrors.Person.Deceased);
        }

        person.UpdateDetails(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.PhoneNumber,
            _currentUser.PersonId,
            _clock.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
