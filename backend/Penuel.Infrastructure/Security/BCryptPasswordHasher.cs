using Penuel.Application.Abstractions;

namespace Penuel.Infrastructure.Security;

/// <summary>
/// Hash de contraseñas con BCrypt (Sección 5.1). El work factor 12 es el estándar
/// recomendado actual: suficientemente costoso para un atacante, imperceptible en un login.
/// La sal la genera y la incrusta BCrypt en el propio hash; no hay columna de sal aparte.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Un hash corrupto o con formato ajeno no es una excepción de negocio:
            // simplemente no verifica.
            return false;
        }
    }
}
