using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Positions.CreatePosition;

/// <summary>
/// Crea un cargo eclesiástico. <c>IsExecutiveBody</c> determina si sus titulares forman
/// parte del Cuerpo Ejecutivo (regla 7.9) — no otorga ningún permiso de sistema (regla 7.10).
/// </summary>
public sealed record CreatePositionCommand(
    string Name,
    string? Description,
    bool IsExecutiveBody) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("El nombre del cargo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");
    }
}

public sealed class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreatePositionCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
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

        var name = request.Name.Trim();

        var exists = await _db.Positions.AnyAsync(
            p => p.ChurchId == churchId.Value && p.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Position.NameAlreadyExists);
        }

        var position = Position.Create(
            churchId.Value, name, request.Description, request.IsExecutiveBody, _clock.UtcNow);

        _db.Positions.Add(position);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(position.Id);
    }
}
