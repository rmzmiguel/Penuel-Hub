namespace Penuel.Domain.Entities.FamilyGroups;

/// <summary>
/// Que una persona asista a un Grupo Familiar concreto.
/// </summary>
/// <remarks>
/// <b>La regla más importante de la rama vive aquí</b>, y es la INVERSA de un patrón que el
/// Core ya usaba. En el Core, "a lo sumo uno activo" se aplica POR RECURSO: un Ministerio
/// tiene un solo líder, pero una persona puede liderar varios. Aquí es al revés: <b>a lo sumo
/// un grupo activo POR PERSONA</b>, sin importar cuál.
///
/// Técnicamente es el mismo mecanismo —un índice único parcial—, pero sobre la columna de la
/// PERSONA en vez de la del recurso. A primera vista parecen la misma regla y no lo son; si
/// alguien copia aquí el índice del Core sin mirar, obtendrá exactamente la restricción
/// contraria a la que hace falta.
///
/// Salir de un grupo NO borra la fila: se cierra con <see cref="LeftAt"/> (regla 7.6). Eso es
/// lo que permite que mover a alguien de un grupo a otro —quitar y volver a agregar— no choque
/// contra el índice, porque la fila cerrada ya no cuenta.
/// </remarks>
public sealed class GroupMember
{
    private GroupMember() { }

    public Guid Id { get; private set; }
    public Guid FamilyGroupId { get; private set; }
    public Guid PersonId { get; private set; }

    public DateOnly JoinedAt { get; private set; }

    /// <summary>Nulo mientras sigue asistiendo. Al salir se cierra; nunca se borra la fila.</summary>
    public DateOnly? LeftAt { get; private set; }

    /// <summary>Quién lo agregó (regla 7.9, que es la 7.4 del Core).</summary>
    public Guid CreatedByPersonId { get; private set; }

    public FamilyGroup FamilyGroup { get; private set; } = null!;
    public Person Person { get; private set; } = null!;

    public static GroupMember Add(
        Guid familyGroupId,
        Guid personId,
        DateOnly joinedAt,
        Guid createdByPersonId)
    {
        return new GroupMember
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyGroupId,
            PersonId = personId,
            JoinedAt = joinedAt,
            LeftAt = null,
            CreatedByPersonId = createdByPersonId
        };
    }

    public bool IsActive() => LeftAt is null;

    /// <summary>Cierra la pertenencia. Idempotente: cerrar dos veces no mueve la primera fecha.</summary>
    public void Leave(DateOnly leftAt)
    {
        if (LeftAt is not null)
        {
            return;
        }

        LeftAt = leftAt;
    }
}
