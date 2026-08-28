namespace Penuel.Domain.Entities;

/// <summary>
/// Liderazgo de una <see cref="Society"/>. Regla 7.11: a lo sumo una fila activa
/// (<c>RevokedAt IS NULL</c>) por SocietyId, aplicada como índice único parcial en PostgreSQL.
/// El resto de la "directiva" de la sociedad no se modela todavía (Sección 4.6).
/// </summary>
public sealed class SocietyLeadership
{
    private SocietyLeadership() { }

    public Guid Id { get; private set; }
    public Guid SocietyId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public Society Society { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static SocietyLeadership Assign(
        Guid societyId,
        Guid personId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new SocietyLeadership
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
