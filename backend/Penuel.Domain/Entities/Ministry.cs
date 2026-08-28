namespace Penuel.Domain.Entities;

/// <summary>
/// Ministerio: departamento funcional de la congregación (Evangelismo, Comunión, Discipulado,
/// Adoración, Servicio, Ministerio Infantil). Cada uno tiene un líder propuesto por el Pastor
/// y el Cuerpo Ejecutivo (Sección 4.3).
/// </summary>
public sealed class Ministry
{
    private Ministry() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Ministry Create(
        Guid churchId,
        string name,
        string? description,
        DateTimeOffset now)
    {
        return new Ministry
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
