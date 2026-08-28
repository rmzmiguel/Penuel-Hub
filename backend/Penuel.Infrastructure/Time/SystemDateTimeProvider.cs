using Penuel.Application.Abstractions;

namespace Penuel.Infrastructure.Time;

/// <summary>
/// Reloj real del sistema, siempre en UTC (offset cero), como exige Npgsql para las
/// columnas <c>timestamptz</c>.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
