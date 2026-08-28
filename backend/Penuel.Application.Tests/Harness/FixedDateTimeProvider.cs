using Penuel.Application.Abstractions;

namespace Penuel.Application.Tests.Harness;

/// <summary>
/// Reloj controlado por la prueba. Es lo que permite verificar caducidades y bloqueos
/// sin esperar en tiempo real.
/// </summary>
public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTimeOffset start) => UtcNow = start;

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
}
