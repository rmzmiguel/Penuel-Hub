using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class FamilyGroupMeetingConfiguration : IEntityTypeConfiguration<FamilyGroupMeeting>
{
    public void Configure(EntityTypeBuilder<FamilyGroupMeeting> builder)
    {
        builder.ToTable("family_group_meetings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MeetingDate).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        // Mismo tipo que el dinero de la rama de Servicios: decimal exacto, nunca punto
        // flotante. 12,2 sobra para una ofrenda de casa y no obliga a pensarlo dos veces.
        builder.Property(m => m.TotalOffering).IsRequired().HasPrecision(12, 2);

        builder.HasOne(m => m.FamilyGroup)
            .WithMany()
            .HasForeignKey(m => m.FamilyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Attendances)
            .WithOne(a => a.FamilyGroupMeeting)
            .HasForeignKey(a => a.FamilyGroupMeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Attendances).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Un reporte por grupo por fecha (Sección 6.3). Índice único TOTAL, no parcial:
        // aquí no hay columna de baja que excluir, porque un reporte no se cierra — se corrige.
        builder.HasIndex(m => new { m.FamilyGroupId, m.MeetingDate })
            .IsUnique()
            .HasDatabaseName("ux_family_group_meetings_date");
    }
}
