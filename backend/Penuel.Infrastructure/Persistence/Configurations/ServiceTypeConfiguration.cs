using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("service_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.RequiresSocietyGrouping).IsRequired();
        builder.Property(t => t.CollectsTithe).IsRequired();
        builder.Property(t => t.AttendanceCustomary).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(t => t.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ChurchId, t.Name })
            .IsUnique()
            .HasDatabaseName("ux_service_types_church_id_name");

        // Los cuatro tipos del ritmo semanal real de la iglesia (Core, Sección 4.4).
        // Los tres flags son lo único que distingue a unos de otros: no hay código que
        // pregunte por el nombre.
        builder.HasData(
            new
            {
                Id = ServicesSeedData.ServiceTypeIds.EscuelaDominical,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Escuela Dominical",
                // Único que se agrupa por Sociedad, y por tanto el único donde aplican
                // puntualidad, Biblia y capítulos leídos (regla 7.3).
                RequiresSocietyGrouping = true,
                CollectsTithe = false,
                AttendanceCustomary = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = ServicesSeedData.ServiceTypeIds.CultoGeneral,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Culto General",
                RequiresSocietyGrouping = false,
                // Único donde se recoge diezmo (Core, Sección 4.4).
                CollectsTithe = true,
                // "No, pero la opción debe estar siempre disponible": este flag es informativo
                // para la UI y NUNCA bloquea que se tome asistencia (Sección 6.1).
                AttendanceCustomary = false,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = ServicesSeedData.ServiceTypeIds.CultoDeOracion,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Culto de Oración",
                RequiresSocietyGrouping = false,
                CollectsTithe = false,
                AttendanceCustomary = false,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = ServicesSeedData.ServiceTypeIds.CultoDeJovenes,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Culto de Jóvenes",
                RequiresSocietyGrouping = false,
                CollectsTithe = false,
                AttendanceCustomary = false,
                CreatedAt = CoreSeedData.SeededAt
            });
    }
}
