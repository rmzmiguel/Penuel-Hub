using Penuel.Application.Abstractions;
using Penuel.Domain.Constants;

namespace Penuel.Application.Services.Abstractions;

/// <summary>
/// Permite operar la captura de Escuela Dominical: el rol <c>SundaySchoolRecorder</c>
/// (Sección 8.1) o el Pastor.
/// </summary>
/// <remarks>
/// El rol es deliberadamente amplio y no está atado a ninguna Sociedad: un grupo pequeño de
/// personas de confianza rota entre los distintos grupos.
/// El Pastor entra también porque tiene control absoluto del sistema (Core, Sección 1);
/// obligarlo a otorgarse a sí mismo un segundo rol para capturar un reporte no tendría sentido.
/// </remarks>
public interface IRequireSundaySchoolRecorder : IRequireAuthorization
{
    IReadOnlyCollection<string> IRequireAuthorization.AcceptedRoles =>
        [RoleNames.Pastor, RoleNames.SundaySchoolRecorder];

    IReadOnlyCollection<string> IRequireAuthorization.AcceptedPositions => [];
}
