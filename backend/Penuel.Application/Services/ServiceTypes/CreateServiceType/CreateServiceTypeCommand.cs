using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;

namespace Penuel.Application.Services.ServiceTypes.CreateServiceType;

/// <summary>
/// Da de alta un tipo de servicio. Los cuatro actuales se siembran con la migración; esto
/// existe para que agregar uno nuevo no exija tocar código ni desplegar (Sección 9, punto 1).
/// </summary>
public sealed record CreateServiceTypeCommand(
    string Name,
    bool RequiresSocietyGrouping,
    bool CollectsTithe,
    bool AttendanceCustomary) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateServiceTypeCommandValidator : AbstractValidator<CreateServiceTypeCommand>
{
    public CreateServiceTypeCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("El nombre del tipo de servicio es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}

public sealed class CreateServiceTypeCommandHandler
    : IRequestHandler<CreateServiceTypeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateServiceTypeCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        CreateServiceTypeCommand request,
        CancellationToken cancellationToken)
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

        var exists = await _db.ServiceTypes.AnyAsync(
            t => t.ChurchId == churchId.Value && t.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.NameAlreadyExists);
        }

        var serviceType = ServiceType.Create(
            churchId.Value,
            name,
            request.RequiresSocietyGrouping,
            request.CollectsTithe,
            request.AttendanceCustomary,
            _clock.UtcNow);

        _db.ServiceTypes.Add(serviceType);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(serviceType.Id);
    }
}
