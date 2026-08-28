namespace Penuel.Domain.Common;

/// <summary>
/// Clasifica un <see cref="Error"/> para que la capa WebApi pueda traducirlo
/// a su código HTTP correspondiente sin que cada controlador lo decida caso por caso
/// (Sección 5.3 del documento maestro).
/// </summary>
public enum ErrorType
{
    /// <summary>HTTP 400 — la petición está mal formada o incumple una regla de validación.</summary>
    Validation = 0,

    /// <summary>HTTP 404 — el recurso solicitado no existe.</summary>
    NotFound = 1,

    /// <summary>HTTP 409 — el estado actual del sistema impide la operación.</summary>
    Conflict = 2,

    /// <summary>HTTP 401 — NO hay sesión: falta el token, es inválido o ya no vale.</summary>
    Unauthorized = 3,

    /// <summary>
    /// HTTP 403 — SÍ hay sesión, pero no alcanza: le falta el rol o el cargo necesario.
    /// Distinguirlo de <see cref="Unauthorized"/> importa porque el frontend reacciona
    /// distinto a cada uno: ante un 401 manda a iniciar sesión, ante un 403 no.
    /// </summary>
    Forbidden = 4,

    /// <summary>HTTP 500 — fallo inesperado de infraestructura.</summary>
    Unexpected = 5
}
