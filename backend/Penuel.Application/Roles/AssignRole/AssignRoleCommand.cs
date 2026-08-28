using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Roles.AssignRole;

/// <summary>
/// Otorga un rol de sistema a una cuenta. Exclusivo del Pastor (regla 7.5).
/// </summary>
/// <remarks>
/// El rol se identifica por NOMBRE y no por Id: los nombres ya están centralizados en
/// <c>RoleNames</c> (regla 7.7), son únicos por iglesia, y así la operación es ejecutable
/// desde Swagger sin tener que buscar antes un Guid en la base.
/// </remarks>
public sealed record AssignRoleCommand(
    Guid UserAccountId,
    string RoleName) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(c => c.UserAccountId)
            .NotEmpty().WithMessage("El identificador de la cuenta es obligatorio.");

        RuleFor(c => c.RoleName)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre del rol no puede exceder 50 caracteres.");
    }
}

public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AssignRoleCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.UserAccounts
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == request.UserAccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.UserAccount.NotFound(request.UserAccountId));
        }

        var roleName = request.RoleName.Trim();

        var role = await _db.Roles.FirstOrDefaultAsync(
            r => r.ChurchId == account.Person.ChurchId && r.Name.ToLower() == roleName.ToLower(),
            cancellationToken);

        if (role is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Role.NotFound(roleName));
        }

        // Respaldado además por el índice único parcial ux_user_roles_active.
        var alreadyActive = await _db.UserRoles.AnyAsync(
            ur => ur.UserAccountId == account.Id && ur.RoleId == role.Id && ur.RevokedAt == null,
            cancellationToken);

        if (alreadyActive)
        {
            return Result.Failure<Guid>(ApplicationErrors.Role.AlreadyAssigned);
        }

        var userRole = UserRole.Assign(account.Id, role.Id, _currentUser.PersonId, _clock.UtcNow);

        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(userRole.Id);
    }
}
