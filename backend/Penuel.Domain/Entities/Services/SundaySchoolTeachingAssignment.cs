namespace Penuel.Domain.Entities.Services;

/// <summary>
/// Quién da la clase de un grupo de Escuela Dominical, semana con semana.
/// </summary>
/// <remarks>
/// NO confundir con <see cref="SocietyLeadership"/> del Core: liderar una Sociedad
/// organizacionalmente y dar su clase son hechos distintos que no se infieren uno del otro
/// (Sección 3.1). El caso real: quien lidera el Ministerio Infantil no imparte esa clase,
/// y a veces da la de Jóvenes.
///
/// A DIFERENCIA de <see cref="SocietyLeadership"/> y <see cref="MinistryLeadership"/>, aquí
/// NO hay índice único parcial de "uno activo a la vez" (regla 7.7). Se permite a propósito que:
///   - una misma persona tenga asignación activa a varias Sociedades (clases combinadas:
///     Damas y Varones se dan juntas desde hace tiempo por falta de maestros);
///   - varias personas tengan asignación activa a la misma Sociedad (dos maestros que se
///     turnan, o un titular más un sustituto habitual).
/// Es la misma filosofía de la regla 7.13 del Core: nada de restricciones de "uno solo"
/// donde la realidad de la iglesia no las sostiene.
/// </remarks>
public sealed class SundaySchoolTeachingAssignment
{
    private SundaySchoolTeachingAssignment() { }

    public Guid Id { get; private set; }

    /// <summary>
    /// Nulo significa <b>maestro sustituto sin grupo fijo</b>, disponible para cualquier
    /// Sociedad — no significa "sin asignar".
    /// </summary>
    public Guid? SocietyId { get; private set; }

    public Guid PersonId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public Society? Society { get; private set; }
    public Person Person { get; private set; } = null!;

    public static SundaySchoolTeachingAssignment Assign(
        Guid? societyId,
        Guid personId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new SundaySchoolTeachingAssignment
        {
            Id = Guid.NewGuid(),
            SocietyId = societyId,
            PersonId = personId,
            AssignedAt = now,
            AssignedByPersonId = assignedByPersonId,
            RevokedAt = null,
            RevokedByPersonId = null
        };
    }

    public bool IsActive() => RevokedAt is null;

    public void Revoke(Guid? revokedByPersonId, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedByPersonId = revokedByPersonId;
    }
}
