using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class ServiceAttendanceConfiguration : IEntityTypeConfiguration<ServiceAttendance>
{
    public void Configure(EntityTypeBuilder<ServiceAttendance> builder)
    {
        builder.ToTable("service_attendances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.WasPresent).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasOne(a => a.ServiceSession)
            .WithMany()
            .HasForeignKey(a => a.ServiceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Person)
            .WithMany()
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una fila de asistencia por persona por sesión.
        builder.HasIndex(a => new { a.ServiceSessionId, a.PersonId })
            .IsUnique()
            .HasDatabaseName("ux_service_attendances_session_person");

        // Soporta el reporte de hábitos de lectura, que recorre asistencias por persona.
        builder.HasIndex(a => a.PersonId);
    }
}
