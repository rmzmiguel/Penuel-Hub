using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Auth.Login;

/// <summary>Inicio de sesión. No requiere autenticación previa (Sección 8.2).</summary>
public sealed record LoginQuery(string Email, string Password) : IRequest<Result<AuthSessionResponse>>;

public sealed class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(q => q.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(320).WithMessage("El correo electrónico excede la longitud permitida.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(q => q.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public sealed class LoginQueryHandler : IRequestHandler<LoginQuery, Result<AuthSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly LockoutPolicy _lockoutPolicy;

    public LoginQueryHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider clock,
        LockoutPolicy lockoutPolicy)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _lockoutPolicy = lockoutPolicy;
    }

    public async Task<Result<AuthSessionResponse>> Handle(
        LoginQuery request,
        CancellationToken cancellationToken)
    {
        var email = UserAccount.NormalizeEmail(request.Email);

        var account = await _db.UserAccounts
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Correo inexistente y contraseña incorrecta devuelven el MISMO error a propósito.
        if (account is null)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.InvalidCredentials);
        }

        var now = _clock.UtcNow;

        if (account.IsLockedOut(now))
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.AccountLocked);
        }

        if (!account.IsActive)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.AccountInactive);
        }

        if (account.Person.Status != PersonStatus.Active)
        {
            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.PersonInactive);
        }

        if (!_passwordHasher.Verify(request.Password, account.PasswordHash))
        {
            account.RegisterFailedLogin(now, _lockoutPolicy.MaxFailedAttempts, _lockoutPolicy.LockoutDuration);
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthSessionResponse>(ApplicationErrors.Auth.InvalidCredentials);
        }

        account.RegisterSuccessfulLogin(now);

        var session = await AuthSession.IssueAsync(
            _db, _jwtProvider, _refreshTokenService, account, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
