using Penuel.Domain.Common;

namespace Penuel.Application.Services.Common;

/// <summary>
/// Catálogo de errores de la rama de Servicios. Separado de <c>ApplicationErrors</c> del Core
/// para que cada rama sea dueña de sus propios códigos, con el mismo criterio: el código es
/// estable y lo lee el frontend; el mensaje es para humanos.
/// </summary>
public static class ServiceErrors
{
    public static class ServiceType
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ServiceType.NotFound", $"No existe un tipo de servicio con el identificador {id}.");

        public static readonly Error NameAlreadyExists = Error.Conflict(
            "ServiceType.NameAlreadyExists", "Ya existe un tipo de servicio con ese nombre en esta iglesia.");

        /// <summary>Regla 7.4: una sesión con Sociedad exige un tipo agrupado por Sociedad.</summary>
        public static readonly Error DoesNotRequireSocietyGrouping = Error.Conflict(
            "ServiceType.DoesNotRequireSocietyGrouping",
            "Este tipo de servicio no se agrupa por Sociedad, así que no admite un reporte por grupo.");

        public static readonly Error RequiresSocietyGrouping = Error.Conflict(
            "ServiceType.RequiresSocietyGrouping",
            "Este tipo de servicio se agrupa por Sociedad: usa el reporte de Escuela Dominical.");

        /// <summary>Regla 7.3: sin CollectsTithe no se acepta TotalTithe.</summary>
        public static readonly Error DoesNotCollectTithe = Error.Conflict(
            "ServiceType.DoesNotCollectTithe",
            "En este tipo de servicio no se recoge diezmo, así que no se acepta un total de diezmo.");
    }

    public static class Session
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ServiceSession.NotFound", $"No existe una sesión con el identificador {id}.");

        public static readonly Error AlreadyExistsForSociety = Error.Conflict(
            "ServiceSession.AlreadyExistsForSociety",
            "Ya se levantó el reporte de ese grupo para esa fecha. Corrígelo en vez de capturarlo de nuevo.");

        public static readonly Error AlreadyExistsForDate = Error.Conflict(
            "ServiceSession.AlreadyExistsForDate",
            "Ya se levantó el reporte de ese servicio para esa fecha. Corrígelo en vez de capturarlo de nuevo.");
    }

    public static class Attendance
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ServiceAttendance.NotFound", $"No existe un registro de asistencia con el identificador {id}.");

        public static readonly Error DuplicatePersonInReport = Error.Validation(
            "ServiceAttendance.DuplicatePersonInReport",
            "La misma persona aparece más de una vez en el reporte.");

        /// <summary>Regla 7.3.</summary>
        public static readonly Error GranularFieldsNotAllowed = Error.Conflict(
            "ServiceAttendance.GranularFieldsNotAllowed",
            "Puntualidad, Biblia y capítulos leídos solo se capturan en servicios agrupados por Sociedad.");
    }

    public static class Tithe
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "TitheEntry.NotFound", $"No existe un registro de diezmo con el identificador {id}.");

        public static readonly Error AlreadyRecorded = Error.Conflict(
            "TitheEntry.AlreadyRecorded",
            "Esa persona ya tiene un diezmo registrado en esta sesión. Corrígelo en vez de agregar otro.");
    }

    public static class Teaching
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "SundaySchoolTeachingAssignment.NotFound",
            $"No existe una asignación de maestro con el identificador {id}.");

        /// <summary>
        /// Solo se rechaza el duplicado EXACTO (misma persona, misma Sociedad, ambas activas).
        /// Que una persona tenga varias Sociedades, o una Sociedad varios maestros, es legítimo
        /// y frecuente (regla 7.7).
        /// </summary>
        public static readonly Error AlreadyAssigned = Error.Conflict(
            "SundaySchoolTeachingAssignment.AlreadyAssigned",
            "Esa persona ya tiene una asignación activa a ese grupo.");

        public static readonly Error AlreadyRevoked = Error.Conflict(
            "SundaySchoolTeachingAssignment.AlreadyRevoked", "Esa asignación ya estaba revocada.");
    }
}
