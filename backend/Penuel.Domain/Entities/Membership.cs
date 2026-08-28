using Penuel.Domain.Enums;

namespace Penuel.Domain.Entities;

/// <summary>
/// Ser miembro oficial de la Comunidad Cristiana Penuel. La sola existencia de esta fila
/// es lo único que determina que alguien es miembro (Sección 6.3). Asistir a un Grupo Familiar
/// NO convierte a nadie en miembro: esa conversión es una decisión posterior y separada,
/// que solo el Pastor (o quien él autorice) registra formalmente (Sección 3.2).
/// </summary>
public sealed class Membership
{
    private Membership() { }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid ChurchId { get; private set; }
    public MembershipStatus Status { get; private set; }

    /// <summary>Fecha en que se volvió miembro oficial, si se conoce.</summary>
    public DateOnly? JoinedAt { get; private set; }

    public Guid? RegisteredByPersonId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Person Person { get; private set; } = null!;

    public static Membership Create(
        Guid personId,
        Guid churchId,
        DateOnly? joinedAt,
        Guid? registeredByPersonId,
        DateTimeOffset now)
    {
        return new Membership
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ChurchId = churchId,
            Status = MembershipStatus.Active,
            JoinedAt = joinedAt,
            RegisteredByPersonId = registeredByPersonId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Activate(DateTimeOffset now)
    {
        Status = MembershipStatus.Active;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        Status = MembershipStatus.Inactive;
        UpdatedAt = now;
    }

    public void MarkAsFormerMember(DateTimeOffset now)
    {
        Status = MembershipStatus.FormerMember;
        UpdatedAt = now;
    }
}
