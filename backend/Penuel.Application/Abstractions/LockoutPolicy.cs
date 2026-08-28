namespace Penuel.Application.Abstractions;

/// <summary>
/// Política de bloqueo temporal tras intentos de acceso fallidos (Sección 6.4).
/// Se registra como singleton desde la configuración; el Dominio la recibe como parámetro
/// en <c>UserAccount.RegisterFailedLogin</c> en lugar de codificarla.
/// </summary>
public sealed record LockoutPolicy(int MaxFailedAttempts, TimeSpan LockoutDuration)
{
    /// <summary>5 intentos, 15 minutos de bloqueo.</summary>
    public static readonly LockoutPolicy Default = new(5, TimeSpan.FromMinutes(15));
}
