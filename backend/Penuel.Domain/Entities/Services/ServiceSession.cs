namespace Penuel.Domain.Entities.Services;

/// <summary>
/// Una sesión concreta de un servicio: un domingo de Escuela Dominical de un grupo, un Culto
/// General de una fecha, etc.
/// </summary>
public sealed class ServiceSession
{
    private ServiceSession() { }

    public Guid Id { get; private set; }
    public Guid ServiceTypeId { get; private set; }

    /// <summary>
    /// Nulo salvo en Escuela Dominical, donde apunta a una de las 4 Sociedades sembradas
    /// en el Core. Regla 7.4: llevarlo o no llevarlo lo decide
    /// <see cref="ServiceType.RequiresSocietyGrouping"/>.
    /// </summary>
    public Guid? SocietyId { get; private set; }

    public DateOnly SessionDate { get; private set; }

    /// <summary>
    /// Regla 7.6: SIEMPRE es un total de grupo. Ningún tipo de servicio de esta rama, presente
    /// o futuro, desglosa la ofrenda por persona.
    /// </summary>
    public decimal TotalOffering { get; private set; }

    /// <summary>
    /// Solo cuando <see cref="ServiceType.CollectsTithe"/> es <c>true</c>; nulo en cualquier
    /// otro caso (regla 7.3). Es el total CONFIABLE, el que sí se cuenta completo — distinto
    /// del detalle parcial y voluntario de <see cref="TitheEntry"/> (regla 7.5).
    /// </summary>
    public decimal? TotalTithe { get; private set; }

    /// <summary>
    /// Quién dio la clase ESA sesión específica, indicado al momento de capturar.
    /// </summary>
    /// <remarks>
    /// Acepta CUALQUIER <see cref="Person"/>: no se restringe a quien tenga una
    /// <see cref="SundaySchoolTeachingAssignment"/> previa, ni se infiere de ella. La rotación
    /// real de maestros —sobre todo en Jóvenes— hace que cubrir a alguien sin asignación formal
    /// sea normal, no una excepción. No agregar esa restricción "por sentido común".
    /// </remarks>
    public Guid? TeacherPersonId { get; private set; }

    /// <summary>
    /// Quién predicó, solo en Culto General. Dato descriptivo, no un cargo: no existe tabla de
    /// "predicadores autorizados". Igual que <see cref="TeacherPersonId"/>, acepta cualquier
    /// <see cref="Person"/> sin restricción previa.
    /// </summary>
    public Guid? PreacherPersonId { get; private set; }

    /// <summary>Quién levantó el reporte (regla 7.4 del Core). Obligatorio.</summary>
    public Guid CreatedByPersonId { get; private set; }

    /// <summary>Quién corrigió por última vez, si alguien lo hizo (regla 7.4 del Core).</summary>
    public Guid? UpdatedByPersonId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ServiceType ServiceType { get; private set; } = null!;
    public Society? Society { get; private set; }
    public Person? Teacher { get; private set; }
    public Person? Preacher { get; private set; }

    /// <summary>
    /// Sesión de Escuela Dominical: lleva Sociedad y maestro, nunca diezmo ni predicador.
    /// Las dos fábricas existen para que la forma correcta sea la fácil de escribir; la
    /// validación contra los flags del <see cref="ServiceType"/> vive en Penuel.Application,
    /// que es quien puede consultarlos.
    /// </summary>
    public static ServiceSession ForSundaySchool(
        Guid serviceTypeId,
        Guid societyId,
        DateOnly sessionDate,
        decimal totalOffering,
        Guid? teacherPersonId,
        Guid createdByPersonId,
        DateTimeOffset now)
    {
        return new ServiceSession
        {
            Id = Guid.NewGuid(),
            ServiceTypeId = serviceTypeId,
            SocietyId = societyId,
            SessionDate = sessionDate,
            TotalOffering = totalOffering,
            TotalTithe = null,
            TeacherPersonId = teacherPersonId,
            PreacherPersonId = null,
            CreatedByPersonId = createdByPersonId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Sesión de Culto General, de Oración o de Jóvenes: sin Sociedad ni maestro; el diezmo
    /// solo lo acepta quien lo recoge.
    /// </summary>
    public static ServiceSession ForGeneralService(
        Guid serviceTypeId,
        DateOnly sessionDate,
        decimal totalOffering,
        decimal? totalTithe,
        Guid? preacherPersonId,
        Guid createdByPersonId,
        DateTimeOffset now)
    {
        return new ServiceSession
        {
            Id = Guid.NewGuid(),
            ServiceTypeId = serviceTypeId,
            SocietyId = null,
            SessionDate = sessionDate,
            TotalOffering = totalOffering,
            TotalTithe = totalTithe,
            TeacherPersonId = null,
            PreacherPersonId = preacherPersonId,
            CreatedByPersonId = createdByPersonId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Corrección de totales (regla 7.1: nunca se borra ni se recaptura, se corrige).
    /// </summary>
    public void CorrectTotals(
        decimal totalOffering,
        decimal? totalTithe,
        Guid? updatedByPersonId,
        DateTimeOffset now)
    {
        TotalOffering = totalOffering;
        TotalTithe = totalTithe;
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }
}
