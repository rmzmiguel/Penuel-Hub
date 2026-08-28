using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.SetFamilyGroupStatus;

/// <summary>
/// Detiene o reanuda un Grupo Familiar. Exclusivo del Pastor (Sección 8.1).
/// </summary>
/// <remarks>
/// No aparece en la lista de casos de uso de la Sección 9 del documento, pero la regla 7.6
/// dice que "un FamilyGroup que deja de reunirse se marca Inactive" — y sin este comando esa
/// regla no tendría forma de ocurrir: <c>FamilyGroup.Deactivate()</c> quedaría como código
/// que nadie puede invocar.
///
/// Nunca borra. Un grupo detenido conserva sus reportes y sus integrantes, y reanudarlo es
/// volver a encender el mismo, no crear otro.
/// </remarks>
public sealed record SetFamilyGroupStatusCommand(Guid FamilyGroupId, bool IsActive)
    : IRequest<Result>, IRequirePastor;

public sealed class SetFamilyGroupStatusCommandValidator
    : AbstractValidator<SetFamilyGroupStatusCommand>
{
    public SetFamilyGroupStatusCommandValidator()
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");
    }
}

public sealed class SetFamilyGroupStatusCommandHandler
    : IRequestHandler<SetFamilyGroupStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetFamilyGroupStatusCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        SetFamilyGroupStatusCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _db.FamilyGroups
            .FirstOrDefaultAsync(g => g.Id == request.FamilyGroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(FamilyGroupErrors.Group.NotFound(request.FamilyGroupId));
        }

        var estaActivo = group.Status == FamilyGroupStatus.Active;

        if (estaActivo == request.IsActive)
        {
            return Result.Success();   // idempotente: pedir lo que ya es no es un error
        }

        if (request.IsActive)
        {
            group.Reactivate(_currentUser.PersonId, _clock.UtcNow);
        }
        else
        {
            group.Deactivate(_currentUser.PersonId, _clock.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
