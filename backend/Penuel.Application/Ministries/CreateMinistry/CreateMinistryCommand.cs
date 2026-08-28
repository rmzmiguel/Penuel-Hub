using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Ministries.CreateMinistry;

public sealed record CreateMinistryCommand(
    string Name,
    string? Description) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateMinistryCommandValidator : AbstractValidator<CreateMinistryCommand>
{
    public CreateMinistryCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("El nombre del ministerio es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");
    }
}

public sealed class CreateMinistryCommandHandler : IRequestHandler<CreateMinistryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateMinistryCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateMinistryCommand request, CancellationToken cancellationToken)
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

        var exists = await _db.Ministries.AnyAsync(
            m => m.ChurchId == churchId.Value && m.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(ApplicationErrors.Ministry.NameAlreadyExists);
        }

        var ministry = Ministry.Create(churchId.Value, name, request.Description, _clock.UtcNow);

        _db.Ministries.Add(ministry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ministry.Id);
    }
}
