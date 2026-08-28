namespace Penuel.Domain.Entities;

/// <summary>
/// Cargo eclesiástico: Pastor, Diácono, Secretario General, Tesorero General.
/// Responde a "¿qué OFICIO ostenta esta persona?", no a qué puede hacer en el software
/// (Sección 3.4). La mayoría de los Diáconos, por ejemplo, nunca tendrán <see cref="UserAccount"/>.
/// </summary>
public sealed class Position
{
    private Position() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>
    /// Determina quién compone el Cuerpo Ejecutivo (regla 7.9). El Cuerpo Ejecutivo NUNCA
    /// se almacena como tabla propia: se computa a partir de este flag y de las filas
    /// activas de <see cref="PersonPosition"/>.
    /// </summary>
    public bool IsExecutiveBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Position Create(
        Guid churchId,
        string name,
        string? description,
        bool isExecutiveBody,
        DateTimeOffset now)
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsExecutiveBody = isExecutiveBody,
            CreatedAt = now
        };
    }

    public void Rename(string name) => Name = name.Trim();

    public void UpdateDescription(string? description) =>
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void SetExecutiveBody(bool isExecutiveBody) => IsExecutiveBody = isExecutiveBody;
}
