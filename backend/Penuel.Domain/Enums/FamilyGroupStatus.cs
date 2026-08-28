namespace Penuel.Domain.Enums;

/// <summary>
/// Estado de un Grupo Familiar. Un grupo que deja de reunirse se marca <c>Inactive</c>;
/// nunca se borra (regla 7.6 de la rama, que es la 7.3 del Core aplicada aquí).
/// </summary>
public enum FamilyGroupStatus
{
    Active = 0,
    Inactive = 1
}
