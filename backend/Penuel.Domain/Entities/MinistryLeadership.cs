namespace Penuel.Domain.Entities;

/// <summary>
/// Liderazgo de un <see cref="Ministry"/>. Regla 7.11: a lo sumo una fila activa
/// (<c>RevokedAt IS NULL</c>) por MinistryId, aplicada como índice único parcial en PostgreSQL.
/// </summary>
/// <remarks>
/// Regla 7.13: la restricción es sobre el RECURSO, nunca sobre la persona. Nada aquí impide
/// que una misma persona lidere varios ministerios, o que además sea Diácono — en una
/// congregación pequeña eso es la norma, no la excepción.
/// Liderar un ministerio no otorga ningún <see cref="Role"/> de sistema (regla 7.10).
/// </remarks>
public sealed class MinistryLeadership
{
    private MinistryLeadership() { }

    public Guid Id { get; private set; }
    public Guid MinistryId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public Ministry Ministry { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static MinistryLeadership Assign(
        Guid ministryId,
        Guid personId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new MinistryLeadership
        {
            Id = Guid.NewGuid(),
            MinistryId = ministryId,
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
