using Penuel.Domain.Constants;

namespace Penuel.Application.Abstractions;

/// <summary>
/// Consultar el directorio de personas y los catálogos que alimentan los selectores de captura.
/// </summary>
/// <remarks>
/// Entran el Pastor, el Tesorero General (por su cargo) y quien tenga el rol
/// <c>SundaySchoolRecorder</c>: exactamente quienes necesitan elegir personas para levantar
/// un reporte. Se deja fuera a cualquier otra cuenta futura que no capture nada.
/// Las respuestas devuelven solo nombre y apellido — nunca teléfono ni fecha de nacimiento —
/// porque su único propósito es poblar selectores, no exponer el padrón.
/// </remarks>
public interface IRequireDirectoryAccess : IRequireAuthorization
{
    IReadOnlyCollection<string> IRequireAuthorization.AcceptedRoles =>
        [RoleNames.Pastor, RoleNames.SundaySchoolRecorder];

    IReadOnlyCollection<string> IRequireAuthorization.AcceptedPositions =>
        [PositionNames.TesoreroGeneral];
}
