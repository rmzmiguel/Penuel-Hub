using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Positions.RevokePosition;

/// <summary>
/// Retira a UNA persona la titularidad de un cargo. Requiere ambos identificadores porque
/// un mismo cargo puede tener varios titulares activos (Sección 6.13).
/// </summary>
public sealed record RevokePositionCommand(
    Guid PositionId,
    Guid PersonId) : IRequest<Result>, IRequirePastor;

public sealed class RevokePositionCommandValidator : AbstractValidator<RevokePositionCommand>
{
    public RevokePositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty().WithMessage("El identificador del cargo es obligatorio.");
        RuleFor(c => c.PersonId).NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class RevokePositionCommandHandler : IRequestHandler<RevokePositionCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RevokePositionCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokePositionCommand request, CancellationToken cancellationToken)
    {
        var positionExists = await _db.Positions
            .AnyAsync(p => p.Id == request.PositionId, cancellationToken);

        if (!positionExists)
        {
            return Result.Failure(ApplicationErrors.Position.NotFound(request.PositionId));
        }

        var personPosition = await _db.PersonPositions.FirstOrDefaultAsync(
            pp => pp.PositionId == request.PositionId
                  && pp.PersonId == request.PersonId
                  && pp.RevokedAt == null,
            cancellationToken);

        if (personPosition is null)
        {
            return Result.Failure(ApplicationErrors.Position.NotHeldByPerson);
        }

        personPosition.Revoke(_currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
