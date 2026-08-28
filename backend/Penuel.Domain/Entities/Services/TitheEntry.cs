namespace Penuel.Domain.Entities.Services;

/// <summary>
/// Diezmo identificado por persona en una sesión.
/// </summary>
/// <remarks>
/// Registro PARCIAL y VOLUNTARIO: no todos anotan sus datos en el sobre. La suma de los
/// <see cref="TitheEntry"/> de una sesión NUNCA se valida ni se fuerza a cuadrar con
/// <see cref="ServiceSession.TotalTithe"/> (regla 7.5). Que no coincidan no es un error de
/// captura — son datos independientes por diseño, y el total es el único número garantizado.
/// Es información más sensible que el total: solo Pastor y Tesorero pueden verla (Sección 8.3).
/// </remarks>
public sealed class TitheEntry
{
    private TitheEntry() { }

    public Guid Id { get; private set; }
    public Guid ServiceSessionId { get; private set; }
    public Guid PersonId { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>Quién lo registró — normalmente el Tesorero (regla 7.2).</summary>
    public Guid CreatedByPersonId { get; private set; }

    /// <summary>Quién lo corrigió, si alguien lo hizo (regla 7.4 del Core).</summary>
    public Guid? UpdatedByPersonId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Person Person { get; private set; } = null!;
    public ServiceSession ServiceSession { get; private set; } = null!;

    public static TitheEntry Record(
        Guid serviceSessionId,
        Guid personId,
        decimal amount,
        Guid createdByPersonId,
        DateTimeOffset now)
    {
        return new TitheEntry
        {
            Id = Guid.NewGuid(),
            ServiceSessionId = serviceSessionId,
            PersonId = personId,
            Amount = amount,
            CreatedByPersonId = createdByPersonId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Corregir es un UPDATE controlado, no una fila nueva: esto es captura operativa,
    /// no una asignación organizacional auditada como las del Core (Sección 6.4).
    /// </summary>
    public void Correct(decimal amount, Guid? updatedByPersonId, DateTimeOffset now)
    {
        Amount = amount;
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }
}
