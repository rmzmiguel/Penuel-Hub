namespace Penuel.Domain.Entities;

/// <summary>
/// Sociedad: agrupación de la congregación por perfil (Damas, Varones, Jóvenes, Infantil),
/// cada una con su propio líder. Nótese que la Sociedad "Infantil" y el Ministerio Infantil
/// son dos registros distintos y complementarios, no el mismo (Sección 4.6): el Ministerio
/// es el departamento funcional con su encargado; la Sociedad existe para que la Escuela
/// Dominical agrupe la asistencia de los niños igual que la de Damas/Varones/Jóvenes.
/// </summary>
public sealed class Society
{
    private Society() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Society Create(
        Guid churchId,
        string name,
        string? description,
        DateTimeOffset now)
    {
        return new Society
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = now
        };
    }

    public void Rename(string name) => Name = name.Trim();

    public void UpdateDescription(string? description) =>
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
