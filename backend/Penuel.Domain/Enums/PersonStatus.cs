namespace Penuel.Domain.Enums;

/// <summary>
/// Estado de una <c>Person</c>. Regla 7.3: nunca hay borrado físico,
/// todo cambio es una transición lógica.
/// </summary>
public enum PersonStatus
{
    Active = 0,
    Inactive = 1,
    Deceased = 2
}
