using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.AddExistingPersonToGroup;

/// <summary>
/// Suma al grupo a alguien que ya está en el directorio (Sección 8.2).
/// </summary>
/// <remarks>
/// Comprueba en código que la persona no esté ya en otro grupo ANTES de intentar el INSERT,
/// aunque el índice único parcial de la base sea quien lo garantiza al final. No es
/// redundancia por gusto: la base devolvería una violación de índice, que se traduce en un
/// 500 y un mensaje que nadie entiende; la comprobación previa devuelve un conflicto con una
/// frase que dice qué hacer.
/// </remarks>
public sealed record AddExistingPersonToGroupCommand(Guid FamilyGroupId, Guid PersonId)
    : IRequest<Result<Guid>>, IRequireFamilyGroupOwnership;

public sealed class AddExistingPersonToGroupCommandValidator
    : AbstractValidator<AddExistingPersonToGroupCommand>
{
    public AddExistingPersonToGroupCommandValidator()
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");

        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class AddExistingPersonToGroupCommandHandler
    : IRequestHandler<AddExistingPersonToGroupCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AddExistingPersonToGroupCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        AddExistingPersonToGroupCommand request,
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

        // Regla 7.2: a lo sumo un grupo activo por persona, EN TODO EL SISTEMA. La consulta
        // no filtra por FamilyGroupId a propósito — hacerlo la convertiría en la regla del
        // Core ("uno por recurso"), que es justo la contraria.
        var yaEnAlguno = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.PersonId == request.PersonId && m.LeftAt == null,
                cancellationToken);

        if (yaEnAlguno is not null)
        {
            // Se distingue "ya está aquí" de "está en otro" solo cuando el grupo es ESTE,
            // porque decirlo no revela nada que quien pregunta no viera ya en su pantalla.
            return Result.Failure<Guid>(yaEnAlguno.FamilyGroupId == request.FamilyGroupId
                ? FamilyGroupErrors.Member.AlreadyInThisGroup
                : FamilyGroupErrors.Member.AlreadyInAnotherGroup);
        }

        var hoy = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var member = GroupMember.Add(group.Id, person.Id, hoy, _currentUser.PersonId!.Value);

        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(member.Id);
    }
}
