using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Auth.Refresh;

/// <summary>Renueva la sesión sin volver a pedir contraseña. No requiere rol (Sección 8.2).</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthSessionResponse>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio.");
    }
}

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtProvider _jwtProvider;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        IRefreshTokenService refreshTokenService,
        IJwtProvider jwtProvider,
        IDateTimeProvider clock)
    {
        _db = db;
        _refreshTokenService = refreshTokenService;
        _jwtProvider = jwtProvider;
        _clock = clock;
    }

    public async Task<Result<AuthSessionResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var stored = await _refreshTokenService.FindAsync(request.RefreshToken, cancellationToken);

        if (stored is null)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.InvalidRefreshToken);
        }

        var now = _clock.UtcNow;

        // REUSO DETECTADO (Sección 8.1): el token existe pero ya estaba revocado. O alguien lo
        // robó y lo usó antes que su dueño, o el dueño está reusando uno viejo. En cualquiera de
        // los dos casos no basta con rechazar esta petición: se cierran TODAS las sesiones vivas
        // de la cuenta y se obliga a iniciar sesión de nuevo. Detectar un robo sin actuar en
        // consecuencia dejaría la ventana abierta más tiempo del necesario.
        if (stored.RevokedAt is not null)
        {
            await _refreshTokenService.RevokeAllForUserAccountAsync(stored.UserAccountId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.RefreshTokenReuseDetected);
        }

        // Expirado sin más: es el final normal de la vida de un token, no una señal de robo.
        if (stored.ExpiresAt <= now)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.InvalidRefreshToken);
        }

        var account = stored.UserAccount;

        if (!account.IsActive)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.AccountInactive);
        }

        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == account.PersonId, cancellationToken);

        if (person is null || person.Status != PersonStatus.Active)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.PersonInactive);
        }

        // Rotación: el token usado se revoca y se emite uno nuevo. Es lo que convierte un
        // segundo uso del mismo token en la señal detectable de arriba.
        stored.Revoke(now);

        var session = await AuthSession.IssueAsync(
            _db, _jwtProvider, _refreshTokenService, account, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
