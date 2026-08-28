namespace Penuel.Domain.Entities;

/// <summary>
/// Permiso de sistema (RBAC). Responde exclusivamente a "¿qué puede HACER esta cuenta dentro
/// del software?" — no es un cargo eclesiástico ni un liderazgo (Sección 3.4, regla 7.10).
/// </summary>
public sealed class Role
{
    private Role() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    /// <summary>Roles sembrados por el sistema (ej. Pastor): no editables ni borrables desde la UI.</summary>
    public bool IsSystemRole { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Role Create(
        Guid churchId,
        string name,
        string description,
        bool isSystemRole,
        DateTimeOffset now)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            Name = name.Trim(),
            Description = description.Trim(),
            IsSystemRole = isSystemRole,
            CreatedAt = now
        };
    }

    public void UpdateDescription(string description) => Description = description.Trim();
}
