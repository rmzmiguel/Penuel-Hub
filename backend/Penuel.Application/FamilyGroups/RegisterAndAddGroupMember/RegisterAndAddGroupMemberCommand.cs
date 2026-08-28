using FluentValidation;
using MediatR;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.RegisterAndAddGroupMember;

/// <summary>
/// Registra a una persona nueva y la suma al grupo, en un solo paso (Secciones 8.2 y 8.3).
/// </summary>
/// <remarks>
/// <b>Por qué no reutiliza <c>RegisterPersonCommand</c> del Core:</b> ese comando exige el rol
/// Pastor por la regla por defecto de la Sección 8.2 del Core. Abrirle una excepción para el
/// Anfitrión lo abriría en TODOS los contextos donde se usa, no solo en este. Un comando
/// propio deja el del Core exclusivo del Pastor y no obliga a nadie a razonar sobre efectos
/// colaterales.
///
/// <b>Y aquí está lo importante:</b> este comando NO TIENE ningún parámetro relacionado con
/// <c>Membership</c> (regla 7.4). No es una validación que se pueda olvidar ni una bandera que
/// alguien pueda poner en true por descuido — sencillamente no hay dónde. Que alguien sea
/// miembro oficial de la iglesia lo decide el Pastor, después y aparte.
/// </remarks>
public sealed record RegisterAndAddGroupMemberCommand(
    Guid FamilyGroupId,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<Result<Guid>>, IRequireFamilyGroupOwnership;

public sealed class RegisterAndAddGroupMemberCommandValidator
    : AbstractValidator<RegisterAndAddGroupMemberCommand>
{
    public RegisterAndAddGroupMemberCommandValidator()
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");

        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden exceder 100 caracteres.");

        RuleFor(c => c.PhoneNumber)
            .MaximumLength(30).When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
            .WithMessage("El teléfono no puede exceder 30 caracteres.");
    }
}

public sealed class RegisterAndAddGroupMemberCommandHandler
    : IRequestHandler<RegisterAndAddGroupMemberCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RegisterAndAddGroupMemberCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        RegisterAndAddGroupMemberCommand request,
        CancellationToken cancellationToken)
    {
        var acceso = await FamilyGroupPermissions.LoadOwnedAsync(
            _db, _currentUser, request.FamilyGroupId, cancellationToken);

        if (!acceso.IsSuccess)
        {
            return Result.Failure<Guid>(acceso.Error!);
        }

        var group = acceso.Value;

        if (group.Status != FamilyGroupStatus.Active)
        {
            return Result.Failure<Guid>(FamilyGroupErrors.Group.NotActive);
        }

        var actorId = _currentUser.PersonId!.Value;

        // La persona nace en la MISMA iglesia que el grupo, no en la de quien la registra:
        // el grupo es el contexto de la operación y es lo que seguirá siendo correcto si
        // algún día hay más de una iglesia.
        var person = Person.Register(
            group.ChurchId,
            request.FirstName,
            request.LastName,
            dateOfBirth: null,
            request.PhoneNumber,
            actorId,
            _clock.UtcNow);

        var hoy = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var member = GroupMember.Add(group.Id, person.Id, hoy, actorId);

        // Recién creada, la persona no puede estar en ningún otro grupo, así que la regla 7.2
        // no necesita comprobarse aquí. Un solo SaveChanges deja ambas filas en la misma
        // transacción: nunca queda una Person suelta sin grupo si algo falla.
        _db.Persons.Add(person);
        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(person.Id);
    }
}
