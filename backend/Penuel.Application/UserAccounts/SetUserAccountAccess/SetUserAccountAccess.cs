using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.UserAccounts.SetUserAccountAccess;

/// <summary>
/// Enciende o apaga el acceso de una cuenta, sin borrarla (regla 7.3).
/// </summary>
/// <remarks>
/// Es la operación que faltaba para que crear una cuenta fuera REVERSIBLE. El Dominio ya la
/// modelaba —<c>UserAccount.Activate()</c> y <c>Deactivate()</c> existen desde el Core—; lo
/// que no existía era una forma de invocarla.
///
/// Un solo comando con un booleano y no dos comandos hermanos, al contrario que
/// <c>DeactivatePerson</c>/<c>ReactivatePerson</c>: allí cada sentido tiene reglas propias
/// (una persona fallecida no se reactiva), aquí los dos sentidos son el mismo interruptor y
/// separarlos duplicaría el handler para no decir nada distinto.
///
/// Apagarla mata además las sesiones vivas, por la misma razón que <c>RevokeRole</c>: sin eso
/// la cuenta seguiría renovando su token hasta que expirara solo.
/// </remarks>
public sealed record SetUserAccountAccessCommand(Guid PersonId, bool IsActive)
    : IRequest<Result>, IRequirePastor;

public sealed class SetUserAccountAccessCommandValidator : AbstractValidator<SetUserAccountAccessCommand>
{
    public SetUserAccountAccessCommandValidator()
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class SetUserAccountAccessCommandHandler
    : IRequestHandler<SetUserAccountAccessCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetUserAccountAccessCommandHandler(
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

    public async Task<Result> Handle(SetUserAccountAccessCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.PersonId == request.PersonId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(ApplicationErrors.UserAccount.NotFoundForPerson(request.PersonId));
        }

        if (account.IsActive == request.IsActive)
        {
            return Result.Failure(request.IsActive
                ? ApplicationErrors.UserAccount.AlreadyActive
                : ApplicationErrors.UserAccount.AlreadyInactive);
        }

        if (!request.IsActive && request.PersonId == _currentUser.PersonId)
        {
            return Result.Failure(ApplicationErrors.UserAccount.CannotDeactivateOwnAccount);
        }

        if (request.IsActive)
        {
            account.Activate(_clock.UtcNow);
        }
        else
        {
            account.Deactivate(_clock.UtcNow);

            // Las dos mitades del mismo candado, igual que en RevokeRole: aquí se cierran las
            // sesiones vivas, y OnTokenValidated rechaza en cada petición el access token que
            // esa cuenta ya tuviera en la mano.
            await _refreshTokenService.RevokeAllForUserAccountAsync(account.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
