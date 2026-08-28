using Penuel.Domain.Common;

namespace Penuel.Application.FamilyGroups.Common;

/// <summary>
/// Catálogo de errores de la rama de Grupos Familiares. Separado de <c>ApplicationErrors</c>
/// del Core con el mismo criterio que <c>ServiceErrors</c>: cada rama es dueña de sus códigos.
/// El código es estable y lo lee el frontend; el mensaje es para humanos.
/// </summary>
public static class FamilyGroupErrors
{
    public static class Group
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "FamilyGroup.NotFound", $"No existe un Grupo Familiar con el identificador {id}.");

        public static readonly Error NotActive = Error.Conflict(
            "FamilyGroup.NotActive", "Este Grupo Familiar ya no está activo.");

        /// <summary>
        /// Se devuelve como 403 y NO revela nada del grupo. Que exista o no, y quién lo lleva,
        /// no es asunto de quien no lo lleva (Sección 2.1).
        /// </summary>
        public static readonly Error NotYours = Error.Forbidden(
            "FamilyGroup.NotYours",
            "No tienes permiso para operar este Grupo Familiar.");
    }

    public static class Member
    {
        /// <summary>
        /// Mensaje GENÉRICO a propósito (regla 7.5): no dice a qué otro grupo pertenece la
        /// persona. Quien lleva una casa no tiene por qué enterarse de quién va a las demás,
        /// y decirlo convertiría una lista de personas en un mapa de la congregación.
        /// </summary>
        public static readonly Error AlreadyInAnotherGroup = Error.Conflict(
            "GroupMember.AlreadyInAnotherGroup",
            "Esta persona ya pertenece a un Grupo Familiar. Para moverla, primero hay que quitarla del suyo.");

        public static readonly Error AlreadyInThisGroup = Error.Conflict(
            "GroupMember.AlreadyInThisGroup", "Esta persona ya está en este grupo.");

        public static Error NotFound(Guid personId) => Error.NotFound(
            "GroupMember.NotFound", $"La persona {personId} no está en este grupo.");
    }

    public static class Meeting
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "FamilyGroupMeeting.NotFound", $"No existe un reporte con el identificador {id}.");

        public static readonly Error DateInFuture = Error.Validation(
            "FamilyGroupMeeting.DateInFuture", "La fecha de la reunión no puede estar en el futuro.");

        public static readonly Error AlreadyReported = Error.Conflict(
            "FamilyGroupMeeting.AlreadyReported",
            "Ya hay un reporte de este grupo para esa fecha. Corrige el existente en vez de levantar otro.");

        /// <summary>
        /// La lista de asistencia solo puede hablar de quien está en el grupo. Si llega alguien
        /// de fuera, es que la pantalla trabajó con una lista vieja.
        /// </summary>
        public static readonly Error AttendeeNotInGroup = Error.Validation(
            "FamilyGroupMeeting.AttendeeNotInGroup",
            "La lista incluye a alguien que no pertenece al grupo. Vuelve a cargar la pantalla.");

        public static readonly Error DuplicateAttendee = Error.Validation(
            "FamilyGroupMeeting.DuplicateAttendee",
            "La lista incluye a la misma persona dos veces.");
    }
}
