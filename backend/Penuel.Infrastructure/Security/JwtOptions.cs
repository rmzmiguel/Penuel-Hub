namespace Penuel.Infrastructure.Security;

/// <summary>Configuración de emisión y validación de tokens (sección "Jwt" de appsettings).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Longitud mínima de la clave simétrica para HMAC-SHA256: 32 bytes.</summary>
    public const int MinimumSecretKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave de firma. NUNCA se versiona: va en user-secrets o variable de entorno.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Vida del access token. Sugerido 15–30 minutos (Sección 8.1).</summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Vida del refresh token. Sugerido 7–14 días (Sección 8.1).</summary>
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>Falla temprano y con un mensaje claro si la configuración es inservible.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Issuer)} no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Audience)} no está configurado.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(SecretKey) < MinimumSecretKeyBytes)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(SecretKey)} debe tener al menos {MinimumSecretKeyBytes} bytes " +
                "para firmar con HMAC-SHA256. Configúralo con 'dotnet user-secrets' o una variable de entorno.");
        }

        if (AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(AccessTokenMinutes)} debe ser mayor que cero.");
        }

        if (RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(RefreshTokenDays)} debe ser mayor que cero.");
        }
    }
}
