namespace Penuel.Domain.Entities.Services;

/// <summary>
/// Asistencia de una persona a una sesión concreta.
/// </summary>
/// <remarks>
/// Los tres campos granulares —puntualidad, Biblia, capítulos— solo tienen sentido cuando el
/// <see cref="ServiceType"/> de la sesión tiene <c>RequiresSocietyGrouping = true</c>, o sea
/// hoy solo en Escuela Dominical (regla 7.3). Existen porque son exactamente el detalle que la
/// hoja física ya recoge y que permite calcular el % que lee la Biblia y el promedio de
/// capítulos por semana, métricas que la iglesia ya usa.
/// </remarks>
public sealed class ServiceAttendance
{
    private ServiceAttendance() { }

    public Guid Id { get; private set; }
    public Guid ServiceSessionId { get; private set; }
    public Guid PersonId { get; private set; }

    /// <summary>Aplica siempre, en cualquier tipo de servicio.</summary>
    public bool WasPresent { get; private set; }

    public bool? WasPunctual { get; private set; }
    public bool? BroughtBible { get; private set; }
    public int? ChaptersRead { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Quién corrigió esta asistencia, si alguien lo hizo (regla 7.4 del Core).</summary>
    public Guid? UpdatedByPersonId { get; private set; }

    public Person Person { get; private set; } = null!;
    public ServiceSession ServiceSession { get; private set; } = null!;

    public static ServiceAttendance Record(
        Guid serviceSessionId,
        Guid personId,
        bool wasPresent,
        bool? wasPunctual,
        bool? broughtBible,
        int? chaptersRead,
        DateTimeOffset now)
    {
        return new ServiceAttendance
        {
            Id = Guid.NewGuid(),
            ServiceSessionId = serviceSessionId,
            PersonId = personId,
            WasPresent = wasPresent,
            WasPunctual = wasPunctual,
            BroughtBible = broughtBible,
            ChaptersRead = chaptersRead,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Corrección de una asistencia ya capturada (regla 7.1: se corrige, no se borra).</summary>
    public void Correct(
        bool wasPresent,
        bool? wasPunctual,
        bool? broughtBible,
        int? chaptersRead,
        Guid? updatedByPersonId,
        DateTimeOffset now)
    {
        WasPresent = wasPresent;
        WasPunctual = wasPunctual;
        BroughtBible = broughtBible;
        ChaptersRead = chaptersRead;
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }
}
