using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class SocietyConfiguration : IEntityTypeConfiguration<Society>
{
    public void Configure(EntityTypeBuilder<Society> builder)
    {
        builder.ToTable("societies");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(s => s.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ChurchId, s.Name })
            .IsUnique()
            .HasDatabaseName("ux_societies_church_id_name");

        builder.HasData(
            new
            {
                Id = CoreSeedData.SocietyIds.Damas,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Damas",
                Description = "Bajo el cuidado general del Ministerio de Comunión.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.SocietyIds.Varones,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Varones",
                Description = "Bajo el cuidado general del Ministerio de Comunión.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.SocietyIds.Jovenes,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Jóvenes",
                Description = "Bajo el cuidado general del Ministerio de Comunión.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                // Complementa al Ministerio Infantil, no lo duplica: existe para que la
                // Escuela Dominical agrupe la asistencia de los niños igual que la de
                // Damas, Varones y Jóvenes (Sección 4.6).
                Id = CoreSeedData.SocietyIds.Infantil,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Infantil",
                Description = "Agrupa a los niños de la congregación. Complementa al Ministerio Infantil " +
                              "sin duplicarlo: son dos registros distintos. El ministerio es el " +
                              "departamento funcional con su encargado; esta sociedad existe para " +
                              "agrupar la asistencia igual que Damas, Varones y Jóvenes.",
                CreatedAt = CoreSeedData.SeededAt
            });
    }
}
