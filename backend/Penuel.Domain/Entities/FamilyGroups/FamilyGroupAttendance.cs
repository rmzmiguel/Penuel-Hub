namespace Penuel.Domain.Entities.FamilyGroups;

/// <summary>
/// Si una persona estuvo o no en una reunión. Un solo dato, a propósito.
/// </summary>
/// <remarks>
/// Compárese con <c>ServiceAttendance</c> de la rama de Servicios, que además guarda
/// puntualidad, Biblia y capítulos leídos. Aquí nada de eso existe: el formulario de un Grupo
/// Familiar es una lista de nombres con una casilla cada uno, y ya.
/// </remarks>
public sealed class FamilyGroupAttendance
{
    private FamilyGroupAttendance() { }

    public Guid Id { get; private set; }
    public Guid FamilyGroupMeetingId { get; private set; }
    public Guid PersonId { get; private set; }
    public bool WasPresent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public FamilyGroupMeeting FamilyGroupMeeting { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static FamilyGroupAttendance Record(
        Guid familyGroupMeetingId,
        Guid personId,
        bool wasPresent,
        DateTimeOffset now)
    {
        return new FamilyGroupAttendance
        {
            Id = Guid.NewGuid(),
            FamilyGroupMeetingId = familyGroupMeetingId,
            PersonId = personId,
            WasPresent = wasPresent,
            CreatedAt = now
        };
    }

    /// <summary>Corrección de una marca ya guardada. La fila se actualiza, nunca se reemplaza.</summary>
    public void SetPresence(bool wasPresent) => WasPresent = wasPresent;
}
