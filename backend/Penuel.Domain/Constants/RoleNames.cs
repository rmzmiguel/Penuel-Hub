namespace Penuel.Domain.Constants;

/// <summary>
/// Nombres de roles de sistema (RBAC). Regla 7.7 del Core: nunca se escriben como texto suelto
/// en el código. Un <c>Role</c> es exclusivamente un permiso de software y no debe confundirse
/// con un <c>Position</c> (cargo eclesiástico) ni con liderar un <c>Ministry</c>/<c>Society</c>
/// — son ejes independientes (Sección 3.4 del Core).
/// </summary>
public static class RoleNames
{
    public const string Pastor = "Pastor";

    /// <summary>
    /// Permiso para operar la pantalla de captura de Escuela Dominical.
    /// </summary>
    /// <remarks>
    /// Deliberadamente AMPLIO y no atado a ninguna Sociedad: en la práctica un grupo pequeño de
    /// personas de confianza rota entre los distintos grupos. Tenerlo NO implica ser maestro de
    /// nadie (<c>SundaySchoolTeachingAssignment</c>), y ser maestro NO implica tenerlo —
    /// un maestro fijo puede depender de que otra persona levante su reporte
    /// (Sección 3.2 de la rama de Servicios).
    /// </remarks>
    public const string SundaySchoolRecorder = "SundaySchoolRecorder";

    /// <summary>
    /// Acceso irrestricto al sistema, para quien lo construye y lo mantiene.
    /// </summary>
    /// <remarks>
    /// NO es "un Pastor más". Es una excepción declarada: una cuenta con este rol NO PASA por
    /// la autorización, la SALTA. La diferencia importa porque un rol que fuera solo
    /// "equivalente a Pastor" tendría que ir añadiéndose a cada marcador nuevo que se cree, y
    /// tarde o temprano alguien olvidaría uno; un salto explícito no se puede olvidar.
    ///
    /// Es deliberadamente ajeno a la estructura de la iglesia: quien lo tiene no necesita ser
    /// miembro oficial, ni ostentar un cargo, ni liderar nada — igual que el desarrollador
    /// tampoco lo es. Por eso NO se llama "Administrador": eso sería un puesto dentro de la
    /// congregación, y esto es una llave de servicio.
    ///
    /// Su contrapartida es la auditoría: todo lo que hace queda firmado con su PersonId en las
    /// columnas <c>*_by_person_id</c>, igual que cualquier otra cuenta.
    /// </remarks>
    public const string Developer = "Desarrollador";

    /// <summary>
    /// Roles que superan CUALQUIER comprobación de autorización, sea cual sea el marcador.
    /// </summary>
    /// <remarks>
    /// Se consulta en exactamente dos sitios, que son las dos únicas puertas del sistema:
    /// <c>AuthorizationBehavior</c> en Penuel.Application y el registro de políticas de
    /// <c>Program.cs</c> en Penuel.WebApi. Que sea una lista y no una constante suelta es lo
    /// que hace que añadir otro superusuario mañana no obligue a tocar esas dos puertas.
    /// </remarks>
    public static readonly IReadOnlyList<string> Superusers = new[] { Developer };

    /// <summary>Todos los roles sembrados por el sistema.</summary>
    public static readonly IReadOnlyList<string> All = new[] { Pastor, SundaySchoolRecorder, Developer };
}
