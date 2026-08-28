namespace Penuel.Domain.Entities;

/// <summary>
/// La iglesia. En esta fase existe exactamente una fila (Comunidad Cristiana Penuel),
/// creada vía seed de migración y nunca vía endpoint (Sección 6.1).
/// Su Id se propaga como <c>ChurchId</c> a las tablas organizacionales: es lo único
/// que se conserva de la puerta abierta a multi-tenant (Sección 5.4).
/// </summary>
public sealed class Church
{
    private Church() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string TimeZone { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public string? Address { get; private set; }
    public int? FoundedYear { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Church Create(
        string name,
        string timeZone,
        string currency,
        string? address,
        int? foundedYear,
        DateTimeOffset now)
    {
        return new Church
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            TimeZone = timeZone.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            FoundedYear = foundedYear,
            CreatedAt = now
        };
    }
}
