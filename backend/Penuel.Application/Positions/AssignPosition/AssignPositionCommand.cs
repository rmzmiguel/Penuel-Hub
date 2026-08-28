using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Positions.AssignPosition;

/// <summary>
/// Nombra a una persona titular de un cargo.
/// </summary>
/// <remarks>
/// A DIFERENCIA de los liderazgos de Ministerio y Sociedad, un cargo admite varios titulares
/// activos a la vez (Sección 6.13: "hay variedad en cuanto al número" de diáconos). Lo único
/// que se rechaza es que la MISMA persona ostente el MISMO cargo dos veces de forma activa.
/// Tampoco hay límite sobre cuántos cargos acumula una persona (regla 7.13).
/// </remarks>
public sealed record AssignPositionCommand(
    Guid PositionId,
    Guid PersonId) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AssignPositionCommandValidator : AbstractValidator<AssignPositionCommand>
{
    public AssignPositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty().WithMessage("El identificador del cargo es obligatorio.");
        RuleFor(c => c.PersonId).NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class AssignPositionCommandHandler : IRequestHandler<AssignPositionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AssignPositionCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(AssignPositionCommand request, CancellationToken cancellationToken)
    {
        var positionExists = await _db.Positions
            .AnyAsync(p => p.Id == request.PositionId, cancellationToken);

        if (!positionExists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Position.NotFound(request.PositionId));
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

        // Nótese el filtro por PERSONA además de por cargo: no se comprueba si el cargo
        // está ocupado, sino si ESTA persona ya lo ostenta.
        var alreadyHeld = await _db.PersonPositions.AnyAsync(
            pp => pp.PositionId == request.PositionId
                  && pp.PersonId == request.PersonId
                  && pp.RevokedAt == null,
            cancellationToken);

        if (alreadyHeld)
        {
            return Result.Failure<Guid>(ApplicationErrors.Position.AlreadyHeldByPerson);
        }

        var personPosition = PersonPosition.Assign(
            request.PositionId, request.PersonId, _currentUser.PersonId, _clock.UtcNow);

        _db.PersonPositions.Add(personPosition);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(personPosition.Id);
    }
}
