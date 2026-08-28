namespace Penuel.Domain.Entities.Services;

/// <summary>
/// Catálogo de tipos de servicio: Escuela Dominical, Culto General, Culto de Oración,
/// Culto de Jóvenes.
/// </summary>
/// <remarks>
/// Existe para no repetir cuatro tablas casi gemelas (Sección 2 de la rama). Los tres flags
/// son los que deciden qué comportamiento aplica en cada caso, de modo que agregar un tipo
/// nuevo no exige tocar código.
/// Es la única entidad de esta rama con <c>ChurchId</c> propio: todas las demás cuelgan de
/// <see cref="ServiceSession"/>, que a su vez cuelga de aquí.
/// </remarks>
public sealed class ServiceType
{
    private ServiceType() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>
    /// <c>true</c> solo en Escuela Dominical. Gobierna dos cosas: que la sesión deba llevar
    /// <c>SocietyId</c> (regla 7.4) y que los campos granulares de asistencia
    /// —puntualidad, Biblia, capítulos— sean aceptables (regla 7.3).
    /// </summary>
    public bool RequiresSocietyGrouping { get; private set; }

    /// <summary>
    /// <c>true</c> solo en Culto General. Sin esto, <c>TotalTithe</c> debe quedar nulo (regla 7.3).
    /// </summary>
    public bool CollectsTithe { get; private set; }

    /// <summary>
    /// Puramente informativo para la futura interfaz ("normalmente no se toma, pero puedes").
    /// NUNCA bloquea que se tome asistencia: es una regla explícita de la iglesia
    /// (Core, Sección 4.4) y ningún caso de uso debe tratarlo como una restricción.
    /// </summary>
    public bool AttendanceCustomary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ServiceType Create(
        Guid churchId,
        string name,
        bool requiresSocietyGrouping,
        bool collectsTithe,
        bool attendanceCustomary,
        DateTimeOffset now)
    {
        return new ServiceType
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            Name = name.Trim(),
            RequiresSocietyGrouping = requiresSocietyGrouping,
            CollectsTithe = collectsTithe,
            AttendanceCustomary = attendanceCustomary,
            CreatedAt = now
        };
    }

    public void Rename(string name) => Name = name.Trim();
}
