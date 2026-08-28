namespace Penuel.Domain.Entities;

/// <summary>
/// Titularidad de un <see cref="Position"/> por parte de una <see cref="Person"/>.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="MinistryLeadership"/> y <see cref="SocietyLeadership"/>, aquí SÍ
/// puede haber múltiples filas activas simultáneas para el mismo PositionId — varios Diáconos
/// a la vez, tal como describe el manual (Sección 6.13, regla 7.11). Por eso NO lleva
/// índice único parcial.
/// Ostentar un cargo no otorga ningún <see cref="Role"/> de sistema (regla 7.10).
/// </remarks>
public sealed class PersonPosition
{
    private PersonPosition() { }

    public Guid Id { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public Position Position { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static PersonPosition Assign(
        Guid positionId,
        Guid personId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new PersonPosition
        {
            Id = Guid.NewGuid(),
            PositionId = positionId,
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
