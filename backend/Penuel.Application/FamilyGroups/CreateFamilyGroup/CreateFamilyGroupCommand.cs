using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.CreateFamilyGroup;

/// <summary>
/// Da de alta una casa como punto de reunión oficial. Exclusivo del Pastor (Sección 8.1).
/// </summary>
/// <remarks>
/// Es un acto ORGANIZACIONAL, del mismo rango que crear un <c>Ministry</c> o un
/// <c>Position</c>: decide que una casa pasa a formar parte de la estructura de la iglesia.
/// La Sección 12 documenta la extensión futura —que el propio Anfitrión capture los datos de
/// su casa—, que no se construye aquí.
///
/// <c>LeaderPersonId</c> nulo significa "el Anfitrión también dirige" (regla 7.1), no
/// "pendiente de asignar": el grupo nunca queda sin Encargado.
/// </remarks>
public sealed record CreateFamilyGroupCommand(
    Guid HostPersonId,
    Guid? LeaderPersonId,
    string Address,
    DayOfWeek? DefaultMeetingDayOfWeek) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateFamilyGroupCommandValidator : AbstractValidator<CreateFamilyGroupCommand>
{
    public CreateFamilyGroupCommandValidator()
    {
        RuleFor(c => c.HostPersonId)
            .NotEmpty().WithMessage("Hay que indicar quién es el Anfitrión.");

        RuleFor(c => c.Address)
            .NotEmpty().WithMessage("La dirección del grupo es obligatoria.")
            .MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres.");

        RuleFor(c => c.DefaultMeetingDayOfWeek)
            .IsInEnum().When(c => c.DefaultMeetingDayOfWeek.HasValue)
            .WithMessage("El día de reunión no es válido.");
    }
}

public sealed class CreateFamilyGroupCommandHandler
    : IRequestHandler<CreateFamilyGroupCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateFamilyGroupCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        CreateFamilyGroupCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var host = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.HostPersonId, cancellationToken);

        if (host is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(request.HostPersonId));
        }

        if (host.Status != PersonStatus.Active)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotActive);
        }

        // Solo se comprueba si de verdad hay un Encargado DISTINTO: cuando es el mismo
        // Anfitrión, ya quedó validado arriba y volver a consultarlo sería una consulta de más.
        if (request.LeaderPersonId is Guid leaderId && leaderId != request.HostPersonId)
        {
            var leader = await _db.Persons
                .FirstOrDefaultAsync(p => p.Id == leaderId, cancellationToken);

            if (leader is null)
            {
                return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(leaderId));
            }

            if (leader.Status != PersonStatus.Active)
            {
                return Result.Failure<Guid>(ApplicationErrors.Person.NotActive);
            }
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, actorId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var group = FamilyGroup.Create(
            churchId.Value,
            request.HostPersonId,
            request.LeaderPersonId,
            request.Address,
            // Jueves por defecto porque es el ritmo real de la iglesia, pero es un valor
            // informativo: nunca se usa para validar la fecha de un reporte (regla 7.7).
            request.DefaultMeetingDayOfWeek ?? DayOfWeek.Thursday,
            actorId,
            _clock.UtcNow);

        _db.FamilyGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(group.Id);
    }
}
