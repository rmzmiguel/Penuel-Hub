using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Roles.RevokeRole;

/// <summary>Retira un rol de sistema a una cuenta. Exclusivo del Pastor (regla 7.5).</summary>
public sealed record RevokeRoleCommand(
    Guid UserAccountId,
    string RoleName) : IRequest<Result>, IRequirePastor;

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(c => c.UserAccountId)
            .NotEmpty().WithMessage("El identificador de la cuenta es obligatorio.");

        RuleFor(c => c.RoleName)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre del rol no puede exceder 50 caracteres.");
    }
}

public sealed class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RevokeRoleCommandHandler(
        IApplicationDbContext db,
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var roleName = request.RoleName.Trim();

        var userRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(
                ur => ur.UserAccountId == request.UserAccountId
                      && ur.RevokedAt == null
                      && ur.Role.Name.ToLower() == roleName.ToLower(),
                cancellationToken);

        if (userRole is null)
        {
            return Result.Failure(ApplicationErrors.Role.NotAssigned);
        }

        userRole.Revoke(_currentUser.PersonId, _clock.UtcNow);

        // Corte de acceso inmediato (Sección 8.1). Dos mitades del mismo candado:
        //  - aquí se matan las sesiones vivas, para que no pueda renovar y obtener un token nuevo;
        //  - en cada petición, OnTokenValidated (Paso 6) revalida los roles contra la base,
        //    de modo que el access token que ya tenía en la mano tampoco le sirve.
        await _refreshTokenService.RevokeAllForUserAccountAsync(request.UserAccountId, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
