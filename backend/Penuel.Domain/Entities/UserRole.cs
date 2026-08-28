namespace Penuel.Domain.Entities;

/// <summary>
/// Otorgamiento de un <see cref="Role"/> a una <see cref="UserAccount"/>, con auditoría completa.
/// Un rol está activo mientras <c>RevokedAt</c> sea null.
/// </summary>
/// <remarks>
/// La PK sustituta <c>Id</c> corrige la Sección 6.7 del documento original: con una PK compuesta
/// (UserAccountId, RoleId) sería imposible volver a otorgar un rol después de revocarlo, porque
/// la segunda fila chocaría con la primera. La unicidad real se aplica como índice único PARCIAL
/// sobre (UserAccountId, RoleId) WHERE revoked_at IS NULL, el mismo patrón de la regla 7.11.
/// La auditoría se guarda como PersonId, no como UserAccountId (regla 7.4).
/// </remarks>
public sealed class UserRole
{
    private UserRole() { }

    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByPersonId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByPersonId { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    public static UserRole Assign(
        Guid userAccountId,
        Guid roleId,
        Guid? assignedByPersonId,
        DateTimeOffset now)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            RoleId = roleId,
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
