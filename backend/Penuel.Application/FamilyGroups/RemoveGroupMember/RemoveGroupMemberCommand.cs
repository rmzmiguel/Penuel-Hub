using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.FamilyGroups.RemoveGroupMember;

/// <summary>
/// Quita a alguien del grupo (Sección 8.2).
/// </summary>
/// <remarks>
/// Cierra la fila con <c>LeftAt</c>; no la borra (regla 7.6). Eso conserva el historial —qué
/// reuniones tuvo mientras estuvo— y además es lo que permite MOVER a alguien de un grupo a
/// otro: la fila cerrada deja de contar para el índice único, así que la siguiente alta no
/// choca (regla 7.3).
/// </remarks>
public sealed record RemoveGroupMemberCommand(Guid FamilyGroupId, Guid PersonId)
    : IRequest<Result>, IRequireFamilyGroupOwnership;

public sealed class RemoveGroupMemberCommandValidator : AbstractValidator<RemoveGroupMemberCommand>
{
    public RemoveGroupMemberCommandValidator()
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");

        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class RemoveGroupMemberCommandHandler
    : IRequestHandler<RemoveGroupMemberCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RemoveGroupMemberCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        RemoveGroupMemberCommand request,
        CancellationToken cancellationToken)
    {
        var acceso = await FamilyGroupPermissions.LoadOwnedAsync(
            _db, _currentUser, request.FamilyGroupId, cancellationToken);

        if (!acceso.IsSuccess)
        {
            return Result.Failure(acceso.Error!);
        }

        var member = await _db.GroupMembers.FirstOrDefaultAsync(
            m => m.FamilyGroupId == request.FamilyGroupId
                 && m.PersonId == request.PersonId
                 && m.LeftAt == null,
            cancellationToken);

        if (member is null)
        {
            return Result.Failure(FamilyGroupErrors.Member.NotFound(request.PersonId));
        }

        member.Leave(DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime));
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
