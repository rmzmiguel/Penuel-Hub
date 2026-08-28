using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class FamilyGroupAttendanceConfiguration : IEntityTypeConfiguration<FamilyGroupAttendance>
{
    public void Configure(EntityTypeBuilder<FamilyGroupAttendance> builder)
    {
        builder.ToTable("family_group_attendances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.WasPresent).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasOne(a => a.Person)
            .WithMany()
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una marca por persona por reunión. Sin esto, corregir un reporte reenviando la
        // lista podría duplicar filas y la asistencia dejaría de cuadrar.
        builder.HasIndex(a => new { a.FamilyGroupMeetingId, a.PersonId })
            .IsUnique()
            .HasDatabaseName("ux_family_group_attendances_person");
    }
}
