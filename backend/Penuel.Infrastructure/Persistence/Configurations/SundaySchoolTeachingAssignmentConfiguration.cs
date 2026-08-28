using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class SundaySchoolTeachingAssignmentConfiguration
    : IEntityTypeConfiguration<SundaySchoolTeachingAssignment>
{
    public void Configure(EntityTypeBuilder<SundaySchoolTeachingAssignment> builder)
    {
        builder.ToTable("sunday_school_teaching_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignedAt).IsRequired();

        // SocietyId NULO significa "maestro sustituto, sin grupo fijo, disponible para
        // cualquier Sociedad" — no significa "sin asignar" (Sección 6.5).
        builder.HasOne(a => a.Society)
            .WithMany()
            .HasForeignKey(a => a.SocietyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Person)
            .WithMany()
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(a => a.AssignedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(a => a.RevokedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================================================
        //  AUSENCIA DELIBERADA DE ÍNDICE ÚNICO PARCIAL — no es un olvido (regla 7.7).
        //
        //  A diferencia de society_leaderships y ministry_leaderships del Core, aquí NO
        //  se restringe "uno activo a la vez", ni por Sociedad ni por persona, porque la
        //  realidad de la iglesia no lo sostiene:
        //    - Damas y Varones se imparten juntas desde hace tiempo por falta de maestros,
        //      así que una misma persona tiene asignación activa a las dos Sociedades.
        //    - Un grupo puede tener dos maestros que se turnan, o un titular más un
        //      sustituto habitual, ambos activos.
        //
        //  Si alguien agrega aquí un índice único creyendo que faltaba, romperá los dos
        //  casos de uso reales de arriba.
        // ==========================================================================

        // Índices de consulta, no de unicidad: alimentan GetMySundaySchoolCaptureContextQuery.
        builder.HasIndex(a => new { a.PersonId, a.RevokedAt });
        builder.HasIndex(a => new { a.SocietyId, a.RevokedAt });
    }
}
