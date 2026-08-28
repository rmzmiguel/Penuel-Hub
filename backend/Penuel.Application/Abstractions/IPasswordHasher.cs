namespace Penuel.Application.Abstractions;

/// <summary>Hash y verificación de contraseñas (BCrypt, Sección 5.1).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Devuelve false ante un hash corrupto o con formato inválido; nunca lanza.</summary>
    bool Verify(string password, string passwordHash);
}
