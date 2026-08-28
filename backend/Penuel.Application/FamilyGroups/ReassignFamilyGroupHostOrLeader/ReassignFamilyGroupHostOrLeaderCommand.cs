using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.ReassignFamilyGroupHostOrLeader;

/// <summary>
/// Cambia quién es Anfitrión y quién Encargado. Exclusivo del Pastor (Sección 8.1).
/// </summary>
/// <remarks>
/// Se reciben LOS DOS a la vez, no uno u otro por separado. Cambiar de casa suele implicar
/// también cambiar de Encargado, y dos comandos independientes dejarían un instante en que el
/// grupo tiene la casa nueva con el Encargado viejo — un estado que nadie quiso.
/// </remarks>
public sealed record ReassignFamilyGroupHostOrLeaderCommand(
    Guid FamilyGroupId,
    Guid HostPersonId,
    Guid? LeaderPersonId) : IRequest<Result>, IRequirePastor;

public sealed class ReassignFamilyGroupHostOrLeaderCommandValidator
    : AbstractValidator<ReassignFamilyGroupHostOrLeaderCommand>
{
    public ReassignFamilyGroupHostOrLeaderCommandValidator()
    {
        RuleFor(c => c.FamilyGroupId)
            .NotEmpty().WithMessage("El identificador del grupo es obligatorio.");

        RuleFor(c => c.HostPersonId)
            .NotEmpty().WithMessage("Hay que indicar quién es el Anfitrión.");
    }
}

public sealed class ReassignFamilyGroupHostOrLeaderCommandHandler
    : IRequestHandler<ReassignFamilyGroupHostOrLeaderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ReassignFamilyGroupHostOrLeaderCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        ReassignFamilyGroupHostOrLeaderCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _db.FamilyGroups
            .FirstOrDefaultAsync(g => g.Id == request.FamilyGroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(FamilyGroupErrors.Group.NotFound(request.FamilyGroupId));
        }

        foreach (var personId in new[] { request.HostPersonId, request.LeaderPersonId }
                     .OfType<Guid>().Distinct())
        {
            var person = await _db.Persons
                .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

            if (person is null)
            {
                return Result.Failure(ApplicationErrors.Person.NotFound(personId));
            }

            if (person.Status != PersonStatus.Active)
            {
                return Result.Failure(ApplicationErrors.Person.NotActive);
            }
        }

        group.Reassign(
            request.HostPersonId, request.LeaderPersonId, _currentUser.PersonId, _clock.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
