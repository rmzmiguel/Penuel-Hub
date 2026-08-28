using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Societies.CreateSociety;

public sealed record CreateSocietyCommand(
    string Name,
    string? Description) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateSocietyCommandValidator : AbstractValidator<CreateSocietyCommand>
{
    public CreateSocietyCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("El nombre de la sociedad es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");
    }
}

public sealed class CreateSocietyCommandHandler : IRequestHandler<CreateSocietyCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateSocietyCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateSocietyCommand request, CancellationToken cancellationToken)
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

        var exists = await _db.Societies.AnyAsync(
            m => m.ChurchId == churchId.Value && m.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Society.NameAlreadyExists);
        }

        var society = Society.Create(churchId.Value, name, request.Description, _clock.UtcNow);

        _db.Societies.Add(society);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(society.Id);
    }
}
