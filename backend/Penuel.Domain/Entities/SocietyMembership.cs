namespace Penuel.Domain.Entities;

/// <summary>
/// Pertenencia de una persona a una <see cref="Society"/>: quién es "de Damas", "de Jóvenes", etc.
/// </summary>
/// <remarks>
/// NO confundir con <see cref="SocietyLeadership"/> (quién está al frente de la Sociedad) ni con
/// la asignación de maestro de Escuela Dominical (quién da la clase). Son tres hechos distintos
/// sobre la misma Sociedad y ninguno se infiere de otro.
///
/// Tampoco es <see cref="Membership"/>: esa dice si alguien es miembro oficial de la iglesia.
/// Se puede pertenecer a un grupo de Escuela Dominical sin ser miembro oficial — de hecho es
/// el caso normal de quien está siendo alcanzado (Core, Sección 3.2).
///
/// Siguiendo la regla 7.13 del Core, no se limita a cuántas Sociedades puede pertenecer una
/// persona: lo único que se impide es el duplicado exacto, la misma persona en la misma
/// Sociedad dos veces de forma activa. Alguien que pasa de Jóvenes a Damas simplemente
/// tiene la primera revocada y la segunda activa, con su historial intacto (regla 7.3).
/// </remarks>
public sealed class SocietyMembership
{
    private SocietyMembership() { }

    public Guid Id { get; private set; }
    public Guid SocietyId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public Society Society { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static SocietyMembership Add(
        Guid societyId,
        Guid personId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new SocietyMembership
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

    public void Remove(Guid? revokedByPersonId, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedByPersonId = revokedByPersonId;
    }
}
