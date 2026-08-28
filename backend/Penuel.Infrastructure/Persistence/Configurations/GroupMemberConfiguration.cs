using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("group_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasOne(m => m.FamilyGroup)
            .WithMany()
            .HasForeignKey(m => m.FamilyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Person)
            .WithMany()
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================================================
        //  EL ÍNDICE MÁS IMPORTANTE DE LA RAMA — y es la INVERSA del patrón del Core.
        //
        //  Va sobre (person_id) A SECAS, sin family_group_id. Eso significa: una persona
        //  no puede estar activa en DOS grupos a la vez EN TODO EL SISTEMA (regla 7.2).
        //
        //  Compárese con ux_society_leaderships_active del Core, que va sobre society_id:
        //  allí el RECURSO limita cuántos titulares tiene, y una persona puede liderar
        //  varias sociedades. Aquí es al revés: la PERSONA limita en cuántos grupos está.
        //  Mismo mecanismo, columna contraria — si alguien añade family_group_id a este
        //  índice "para que quede como los demás", la regla deja de existir en silencio y
        //  una persona podrá aparecer en todos los grupos a la vez.
        //
        //  El filtro por left_at es lo que permite MOVER a alguien de un grupo a otro
        //  (regla 7.3): la fila cerrada deja de contar, así que la nueva no choca.
        // ==========================================================================
        builder.HasIndex(m => m.PersonId)
            .IsUnique()
            .HasFilter("left_at IS NULL")
            .HasDatabaseName("ux_group_members_active_person");

        // Consulta: los integrantes vivos de un grupo, que es lo que precarga el reporte.
        builder.HasIndex(m => new { m.FamilyGroupId, m.LeftAt });
    }
}
