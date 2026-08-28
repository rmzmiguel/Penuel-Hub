namespace Penuel.Domain.Entities;

/// <summary>
/// Credenciales para entrar al sistema. Ortogonal a <see cref="Person"/> y a <see cref="Membership"/>
/// (Sección 3.3): un miembro oficial normal casi nunca tendrá cuenta, y quien tiene cuenta
/// no es necesariamente miembro oficial.
/// </summary>
public sealed class UserAccount
{
    private UserAccount() { }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Person Person { get; private set; } = null!;

    public static UserAccount Create(
        Guid personId,
        string email,
        string passwordHash,
        DateTimeOffset now)
    {
        return new UserAccount
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>El email se normaliza al guardarse para que el índice único sea insensible a mayúsculas.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil.HasValue && LockedUntil.Value > now;

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// La política de bloqueo (cuántos intentos y por cuánto tiempo) se recibe como parámetro
    /// en lugar de estar codificada aquí: el Dominio no depende de configuración.
    /// </summary>
    public void RegisterFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntil = now.Add(lockoutDuration);
            FailedLoginAttempts = 0;
        }

        UpdatedAt = now;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    public void ChangeEmail(string email, DateTimeOffset now)
    {
        Email = NormalizeEmail(email);
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    /// <summary>Desactivación lógica (regla 7.3). La fila nunca se elimina.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
