namespace Penuel.Domain.Enums;

/// <summary>
/// Estado de la membresía oficial en la Comunidad Cristiana Penuel.
/// La sola existencia de la fila determina que alguien es miembro (Sección 6.3);
/// este enum determina en qué condición lo es.
/// </summary>
public enum MembershipStatus
{
    Active = 0,
    Inactive = 1,
    FormerMember = 2
}
