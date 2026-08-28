namespace Penuel.Domain.Entities.FamilyGroups;

/// <summary>
/// El reporte de una reunión: la fecha real en que ocurrió y la ofrenda total.
/// </summary>
/// <remarks>
/// Deliberadamente mínimo. Aquí NO hay puntualidad, ni Biblia, ni capítulos leídos: eso es
/// propio de Escuela Dominical y meterlo aquí convertiría un formulario de dos campos en uno
/// de cinco, para gente que hoy lo lleva en una hoja de papel.
///
/// <see cref="MeetingDate"/> es la fecha REAL, sin ninguna atadura al día habitual del grupo
/// (regla 7.7). El sistema no debe enterarse de que un reporte "debería" haber sido jueves.
/// </remarks>
public sealed class FamilyGroupMeeting
{
    private FamilyGroupMeeting() { }

    public Guid Id { get; private set; }
    public Guid FamilyGroupId { get; private set; }
    public DateOnly MeetingDate { get; private set; }
    public decimal TotalOffering { get; private set; }

    public Guid CreatedByPersonId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByPersonId { get; private set; }

    public FamilyGroup FamilyGroup { get; private set; } = null!;

    private readonly List<FamilyGroupAttendance> _attendances = [];
    public IReadOnlyCollection<FamilyGroupAttendance> Attendances => _attendances;

    public static FamilyGroupMeeting Create(
        Guid familyGroupId,
        DateOnly meetingDate,
        decimal totalOffering,
        Guid createdByPersonId,
        DateTimeOffset now)
    {
        return new FamilyGroupMeeting
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyGroupId,
            MeetingDate = meetingDate,
            TotalOffering = totalOffering,
            CreatedByPersonId = createdByPersonId,
            CreatedAt = now,
            UpdatedAt = now,
            UpdatedByPersonId = null
        };
    }

    public void AddAttendance(Guid personId, bool wasPresent, DateTimeOffset now) =>
        _attendances.Add(FamilyGroupAttendance.Record(Id, personId, wasPresent, now));

    /// <summary>
    /// Corrige la ofrenda de un reporte ya levantado. UPDATE controlado, nunca borrar y
    /// recapturar — mismo criterio que la regla 7.1 de la rama de Servicios.
    /// </summary>
    public void CorrectOffering(decimal totalOffering, Guid updatedByPersonId, DateTimeOffset now)
    {
        TotalOffering = totalOffering;
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }

    public void Stamp(Guid updatedByPersonId, DateTimeOffset now)
    {
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }
}
