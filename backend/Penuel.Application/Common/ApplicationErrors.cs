using Penuel.Domain.Common;

namespace Penuel.Application.Common;

/// <summary>
/// Catálogo único de errores de negocio. Centralizarlos evita que el mismo error se escriba
/// con dos códigos distintos en dos handlers, y deja el contrato de la API estable para el
/// frontend (Sección 5.3).
/// </summary>
public static class ApplicationErrors
{
    public static class Auth
    {
        public static readonly Error NotAuthenticated = Error.Unauthorized(
            "Auth.NotAuthenticated", "Se requiere una sesión iniciada.");

        /// <summary>
        /// 403, no 401: hay sesión válida, lo que falta es el rol. Mandar a iniciar sesión
        /// de nuevo no resolvería nada.
        /// </summary>
        public static readonly Error PastorRoleRequired = Error.Forbidden(
            "Auth.PastorRoleRequired", "Esta operación es exclusiva del rol Pastor.");

        /// <summary>
        /// Mismo error para email inexistente y contraseña incorrecta: no revelar cuál de
        /// los dos falló impide enumerar cuentas válidas.
        /// </summary>
        public static readonly Error InvalidCredentials = Error.Unauthorized(
            "Auth.InvalidCredentials", "Correo o contraseña incorrectos.");

        public static readonly Error AccountInactive = Error.Unauthorized(
            "Auth.AccountInactive", "La cuenta está desactivada.");

        public static readonly Error PersonInactive = Error.Unauthorized(
            "Auth.PersonInactive", "La persona asociada a esta cuenta no está activa.");

        public static readonly Error AccountLocked = Error.Unauthorized(
            "Auth.AccountLocked", "La cuenta está bloqueada temporalmente por intentos fallidos.");

        public static readonly Error InvalidRefreshToken = Error.Unauthorized(
            "Auth.InvalidRefreshToken", "El refresh token no es válido o expiró.");

        /// <summary>
        /// Se presentó un refresh token YA revocado. Señal de robo (alguien más lo usó antes)
        /// o de un cliente reusando uno viejo. En ambos casos se cierran todas las sesiones
        /// de la cuenta; el código es distinto para que el frontend pueda explicar por qué
        /// se cerró la sesión en vez de mostrar un error genérico.
        /// </summary>
        public static readonly Error RefreshTokenReuseDetected = Error.Unauthorized(
            "Auth.RefreshTokenReuseDetected",
            "Se detectó el reuso de un token de sesión. Por seguridad se cerraron todas las sesiones de esta cuenta.");

        public static readonly Error OperatorPersonNotFound = Error.Unauthorized(
            "Auth.OperatorPersonNotFound", "No se encontró la persona asociada a la sesión actual.");

        /// <summary>
        /// 403: hay sesión válida, pero ni sus roles ni sus cargos alcanzan. El mensaje dice
        /// QUÉ haría falta, porque quien lo lee suele ser quien tiene que pedir el permiso.
        /// </summary>
        public static Error InsufficientPermissions(
            IReadOnlyCollection<string> acceptedRoles,
            IReadOnlyCollection<string> acceptedPositions)
        {
            var requisitos = new List<string>();

            if (acceptedRoles.Count > 0)
            {
                requisitos.Add($"el rol {string.Join(" o ", acceptedRoles)}");
            }

            if (acceptedPositions.Count > 0)
            {
                requisitos.Add($"el cargo de {string.Join(" o ", acceptedPositions)}");
            }

            return Error.Forbidden(
                "Auth.InsufficientPermissions",
                $"Esta operación requiere {string.Join(", o bien ", requisitos)}.");
        }
    }

    public static class Person
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "Person.NotFound", $"No existe una persona con el identificador {id}.");

        public static readonly Error NotActive = Error.Conflict(
            "Person.NotActive", "La persona no está activa.");

        public static readonly Error AlreadyActive = Error.Conflict(
            "Person.AlreadyActive", "La persona ya está activa.");

        public static readonly Error AlreadyInactive = Error.Conflict(
            "Person.AlreadyInactive", "La persona ya está inactiva.");

        public static readonly Error Deceased = Error.Conflict(
            "Person.Deceased", "La persona está registrada como fallecida; su estado no se cambia desde aquí.");
    }

    public static class UserAccount
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "UserAccount.NotFound", $"No existe una cuenta de usuario con el identificador {id}.");

        public static readonly Error AlreadyExists = Error.Conflict(
            "UserAccount.AlreadyExists", "Esta persona ya tiene una cuenta de usuario (regla 7.1).");

        public static Error NotFoundForPerson(Guid personId) => Error.NotFound(
            "UserAccount.NotFoundForPerson",
            $"La persona {personId} no tiene una cuenta de acceso.");

        public static readonly Error AlreadyActive = Error.Conflict(
            "UserAccount.AlreadyActive",
            "La cuenta ya está activa.");

        public static readonly Error AlreadyInactive = Error.Conflict(
            "UserAccount.AlreadyInactive",
            "La cuenta ya está desactivada.");

        /// <summary>
        /// Único candado irreversible del panel: desactivar la propia cuenta deja a quien lo
        /// hace fuera del sistema en el acto y sin forma de deshacerlo por sí mismo. Revocarse
        /// un rol sí se permite —siempre queda otra cuenta administradora que lo devuelva—,
        /// pero esto no.
        /// </summary>
        public static readonly Error CannotDeactivateOwnAccount = Error.Conflict(
            "UserAccount.CannotDeactivateOwn",
            "No puedes desactivar tu propia cuenta: te dejaría fuera del sistema.");

        public static readonly Error EmailAlreadyExists = Error.Conflict(
            "UserAccount.EmailAlreadyExists", "Ya existe una cuenta con ese correo electrónico.");
    }

    public static class Membership
    {
        public static readonly Error NotFound = Error.NotFound(
            "Membership.NotFound",
            "Esta persona no es miembro oficial.");

        public static readonly Error AlreadyActive = Error.Conflict(
            "Membership.AlreadyActive",
            "La membresía ya está activa.");

        public static readonly Error AlreadyRevoked = Error.Conflict(
            "Membership.AlreadyRevoked",
            "La membresía ya estaba dada de baja.");

        public static readonly Error AlreadyExists = Error.Conflict(
            "Membership.AlreadyExists", "Esta persona ya tiene un registro de membresía (regla 7.2).");
    }

    public static class Role
    {
        public static Error NotFound(string name) => Error.NotFound(
            "Role.NotFound", $"No existe el rol '{name}' en esta iglesia.");

        public static readonly Error AlreadyAssigned = Error.Conflict(
            "Role.AlreadyAssigned", "La cuenta ya tiene ese rol activo.");

        public static readonly Error NotAssigned = Error.NotFound(
            "Role.NotAssigned", "La cuenta no tiene ese rol activo.");
    }

    public static class Society
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "Society.NotFound", $"No existe una sociedad con el identificador {id}.");

        public static readonly Error NameAlreadyExists = Error.Conflict(
            "Society.NameAlreadyExists", "Ya existe una sociedad con ese nombre en esta iglesia.");

        public static readonly Error AlreadyHasActiveLeader = Error.Conflict(
            "Society.AlreadyHasActiveLeader",
            "La sociedad ya tiene un líder activo (regla 7.11). Revoca el liderazgo vigente antes de asignar otro.");

        public static readonly Error NoActiveLeader = Error.NotFound(
            "Society.NoActiveLeader", "La sociedad no tiene un líder activo que revocar.");

        public static readonly Error MemberAlreadyAdded = Error.Conflict(
            "Society.MemberAlreadyAdded", "Esa persona ya pertenece a esta sociedad.");

        public static Error MembershipNotFound(Guid id) => Error.NotFound(
            "Society.MembershipNotFound", $"No existe una pertenencia a sociedad con el identificador {id}.");

        public static readonly Error MembershipAlreadyRemoved = Error.Conflict(
            "Society.MembershipAlreadyRemoved", "Esa pertenencia ya estaba dada de baja.");
    }

    public static class Ministry
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "Ministry.NotFound", $"No existe un ministerio con el identificador {id}.");

        public static readonly Error NameAlreadyExists = Error.Conflict(
            "Ministry.NameAlreadyExists", "Ya existe un ministerio con ese nombre en esta iglesia.");

        public static readonly Error AlreadyHasActiveLeader = Error.Conflict(
            "Ministry.AlreadyHasActiveLeader",
            "El ministerio ya tiene un líder activo (regla 7.11). Revoca el liderazgo vigente antes de asignar otro.");

        public static readonly Error NoActiveLeader = Error.NotFound(
            "Ministry.NoActiveLeader", "El ministerio no tiene un líder activo que revocar.");
    }

    public static class Position
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "Position.NotFound", $"No existe un cargo con el identificador {id}.");

        public static readonly Error NameAlreadyExists = Error.Conflict(
            "Position.NameAlreadyExists", "Ya existe un cargo con ese nombre en esta iglesia.");

        /// <summary>
        /// Un cargo SÍ admite varios titulares activos (Sección 6.13); lo único que se impide
        /// es que la misma persona lo ostente dos veces a la vez.
        /// </summary>
        public static readonly Error AlreadyHeldByPerson = Error.Conflict(
            "Position.AlreadyHeldByPerson", "Esta persona ya ostenta ese cargo de forma activa.");

        public static readonly Error NotHeldByPerson = Error.NotFound(
            "Position.NotHeldByPerson", "Esta persona no ostenta ese cargo de forma activa.");
    }

    public static class Validation
    {
        public static Error Failed(string message) => Error.Validation("Validation.Failed", message);
    }
}
