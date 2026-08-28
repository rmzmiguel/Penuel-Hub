namespace Penuel.Application.Abstractions;

/// <summary>
/// Reloj del sistema. Existe para que las pruebas del Paso 8 controlen el tiempo sin mocks
/// de infraestructura, y para garantizar que TODA marca de tiempo se genere en UTC.
/// </summary>
/// <remarks>
/// El offset debe ser cero: Npgsql exige que un <see cref="DateTimeOffset"/> destinado a una
/// columna <c>timestamptz</c> esté en UTC, y lanza excepción con cualquier otro offset.
/// </remarks>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
