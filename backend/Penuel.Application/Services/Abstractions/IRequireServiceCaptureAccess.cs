using Penuel.Application.Abstractions;
using Penuel.Domain.Constants;

namespace Penuel.Application.Services.Abstractions;

/// <summary>
/// Puerta de entrada para operaciones que sirven a AMBOS mundos de captura: Escuela Dominical
/// y cultos generales. Deja pasar a quien pueda capturar algo, y el handler afina después
/// según el tipo de servicio de la sesión concreta.
/// </summary>
/// <remarks>
/// Existe por <c>CorrectServiceSessionTotalsCommand</c>, que corrige totales de cualquier
/// sesión. Quién puede corregirla depende de QUÉ sesión es, y eso no se sabe hasta cargarla:
///   - sesión de Escuela Dominical  -> Pastor o SundaySchoolRecorder (Sección 8.1)
///   - culto general/oración/jóvenes -> Pastor o Tesorero General   (Sección 8.3)
/// El criterio de fondo: quien puede capturar un tipo de reporte puede corregirlo. Obligar a
/// que un maestro pida ayuda para arreglar un dígito mal tecleado sería fricción sin motivo,
/// y darle acceso a los totales de los cultos donde no participa sería de más.
/// </remarks>
public interface IRequireServiceCaptureAccess : IRequireAuthorization
{
    IReadOnlyCollection<string> IRequireAuthorization.AcceptedRoles =>
        [RoleNames.Pastor, RoleNames.SundaySchoolRecorder];

    IReadOnlyCollection<string> IRequireAuthorization.AcceptedPositions =>
        [PositionNames.TesoreroGeneral];
}
