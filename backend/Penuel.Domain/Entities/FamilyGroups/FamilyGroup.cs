using Penuel.Domain.Enums;

namespace Penuel.Domain.Entities.FamilyGroups;

/// <summary>
/// Una casa que abre sus puertas los jueves. Es la unidad que da origen a todo el proyecto:
/// sustituye las hojas de papel con las que hoy se lleva asistencia y ofrenda.
/// </summary>
/// <remarks>
/// A diferencia de una <see cref="Society"/> —donde quien captura rota entre varias personas
/// de confianza—, un Grupo Familiar tiene identidad estable: una casa concreta, un Anfitrión
/// y un Encargado.
///
/// <b>Anfitrión y Encargado son roles distintos con permisos IDÉNTICOS</b> (Sección 8.2 de la
/// rama). Ofrecer la casa y dirigir la reunión son cosas diferentes que a menudo recaen en la
/// misma persona; la distinción es informativa para el Pastor, no una jerarquía de acceso.
/// </remarks>
public sealed class FamilyGroup
{
    private FamilyGroup() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }

    /// <summary>Quien pone la casa. Nunca nulo.</summary>
    public Guid HostPersonId { get; private set; }

    /// <summary>
    /// Quien dirige la reunión. Nunca nulo: si nadie más lidera, apunta a la misma persona
    /// que <see cref="HostPersonId"/> (regla 7.1).
    /// </summary>
    /// <remarks>
    /// Que nunca sea nulo NO es un detalle de conveniencia. Es lo que evita que cada pantalla
    /// y cada comprobación de permisos tenga que preguntar antes "¿y si no hay Encargado
    /// distinto?" — una pregunta que se habría repetido en once casos de uso.
    /// </remarks>
    public Guid LeaderPersonId { get; private set; }

    /// <summary>Dirección DEL GRUPO, no de la persona: si el Anfitrión cambia, la casa puede seguir.</summary>
    public string Address { get; private set; } = null!;

    /// <summary>
    /// Día habitual de reunión. Puramente INFORMATIVO (regla 7.7): jamás valida ni bloquea la
    /// fecha real de una reunión. Quien mueve su jueves porque de verdad no pudo, no debe
    /// encontrarse con que el sistema se lo discute.
    /// </summary>
    public DayOfWeek DefaultMeetingDayOfWeek { get; private set; }

    public FamilyGroupStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedByPersonId { get; private set; }
    public Guid? UpdatedByPersonId { get; private set; }

    public Person Host { get; private set; } = null!;
    public Person Leader { get; private set; } = null!;

    public static FamilyGroup Create(
        Guid churchId,
        Guid hostPersonId,
        Guid? leaderPersonId,
        string address,
        DayOfWeek defaultMeetingDayOfWeek,
        Guid? createdByPersonId,
        DateTimeOffset now)
    {
        return new FamilyGroup
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            HostPersonId = hostPersonId,
            // Regla 7.1: sin Encargado distinto, el Anfitrión lo es.
            LeaderPersonId = leaderPersonId ?? hostPersonId,
            Address = address.Trim(),
            DefaultMeetingDayOfWeek = defaultMeetingDayOfWeek,
            Status = FamilyGroupStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByPersonId = createdByPersonId,
            UpdatedByPersonId = createdByPersonId
        };
    }

    /// <summary>Quién es Anfitrión y quién Encargado. Acto organizacional: solo el Pastor (8.1).</summary>
    public void Reassign(
        Guid hostPersonId,
        Guid? leaderPersonId,
        Guid? updatedByPersonId,
        DateTimeOffset now)
    {
        HostPersonId = hostPersonId;
        LeaderPersonId = leaderPersonId ?? hostPersonId;
        Stamp(updatedByPersonId, now);
    }

    public void UpdateDetails(
        string address,
        DayOfWeek defaultMeetingDayOfWeek,
        Guid? updatedByPersonId,
        DateTimeOffset now)
    {
        Address = address.Trim();
        DefaultMeetingDayOfWeek = defaultMeetingDayOfWeek;
        Stamp(updatedByPersonId, now);
    }

    /// <summary>Deja de reunirse. La fila permanece (regla 7.6).</summary>
    public void Deactivate(Guid? updatedByPersonId, DateTimeOffset now)
    {
        Status = FamilyGroupStatus.Inactive;
        Stamp(updatedByPersonId, now);
    }

    public void Reactivate(Guid? updatedByPersonId, DateTimeOffset now)
    {
        Status = FamilyGroupStatus.Active;
        Stamp(updatedByPersonId, now);
    }

    /// <summary>
    /// ¿Esta persona es el Anfitrión o el Encargado? Es la comprobación que sustituye por
    /// completo a roles y cargos en esta rama (Sección 2.2).
    /// </summary>
    public bool IsOwnedBy(Guid personId) => HostPersonId == personId || LeaderPersonId == personId;

    private void Stamp(Guid? updatedByPersonId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedByPersonId = updatedByPersonId;
    }
}
