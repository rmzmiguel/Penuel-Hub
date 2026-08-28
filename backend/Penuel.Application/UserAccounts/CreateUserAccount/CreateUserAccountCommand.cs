using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.UserAccounts.CreateUserAccount;

/// <summary>
/// Le da credenciales a una persona. Ortogonal a la membresía: tener cuenta no implica ser
/// miembro oficial ni al revés (Sección 3.3).
/// </summary>
public sealed record CreateUserAccountCommand(
    Guid PersonId,
    string Email,
    string Password) : IRequest<Result<Guid>>, IRequirePastor;

public sealed class CreateUserAccountCommandValidator : AbstractValidator<CreateUserAccountCommand>
{
    /// <summary>
    /// BCrypt trunca en silencio todo lo que pase de 72 bytes: aceptar contraseñas más largas
    /// daría una falsa sensación de seguridad, así que se rechazan explícitamente.
    /// </summary>
    public const int MaxPasswordBytes = 72;

    public const int MinPasswordLength = 8;

    public CreateUserAccountCommandValidator()
    {
        RuleFor(c => c.PersonId)
            .NotEmpty().WithMessage("El identificador de la persona es obligatorio.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(320).WithMessage("El correo electrónico excede la longitud permitida.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {MinPasswordLength} caracteres.")
            .Must(p => p is null || System.Text.Encoding.UTF8.GetByteCount(p) <= MaxPasswordBytes)
            .WithMessage($"La contraseña no puede exceder {MaxPasswordBytes} bytes.");
    }
}

public sealed class CreateUserAccountCommandHandler
    : IRequestHandler<CreateUserAccountCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public CreateUserAccountCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        CreateUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var person = await _db.Persons
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(request.PersonId));
        }

        if (person.Status != PersonStatus.Active)
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotActive);
        }

        // Regla 7.1: una Person tiene como máximo un UserAccount.
        var alreadyHasAccount = await _db.UserAccounts
            .AnyAsync(u => u.PersonId == request.PersonId, cancellationToken);

        if (alreadyHasAccount)
        {
            return Result.Failure<Guid>(ApplicationErrors.UserAccount.AlreadyExists);
        }

        var email = UserAccount.NormalizeEmail(request.Email);

        var emailTaken = await _db.UserAccounts
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailTaken)
        {
            return Result.Failure<Guid>(ApplicationErrors.UserAccount.EmailAlreadyExists);
        }

        var account = UserAccount.Create(
            request.PersonId,
            email,
            _passwordHasher.Hash(request.Password),
            _clock.UtcNow);

        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(account.Id);
    }
}
