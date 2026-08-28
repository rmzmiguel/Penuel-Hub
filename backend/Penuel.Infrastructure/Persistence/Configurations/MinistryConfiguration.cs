using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class MinistryConfiguration : IEntityTypeConfiguration<Ministry>
{
    public void Configure(EntityTypeBuilder<Ministry> builder)
    {
        builder.ToTable("ministries");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(m => m.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ChurchId, m.Name })
            .IsUnique()
            .HasDatabaseName("ux_ministries_church_id_name");

        // Los 6 ministerios funcionales de la Sección 4.3, con las descripciones del
        // Manual de Organización.
        builder.HasData(
            new
            {
                Id = CoreSeedData.MinistryIds.Evangelismo,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Evangelismo",
                Description = "Campañas, evangelismo casa por casa y el proyecto UNO+UNO.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.MinistryIds.Comunion,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Comunión",
                Description = "Organiza los cultos, recibe a los visitantes y tiene bajo su " +
                              "cuidado a las Sociedades de Damas, Varones y Jóvenes.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.MinistryIds.Discipulado,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Discipulado",
                Description = "Escuela Bíblica Local y cursos de Catecúmenos, Prebautismal y Prematrimonial.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.MinistryIds.Adoracion,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Adoración",
                Description = "Grupos de alabanza, vida devocional y la agenda de veladas y vigilias.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.MinistryIds.Servicio,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Servicio",
                Description = "Ujieres, eventos especiales, ACUPYHNAD y mantenimiento de los bienes.",
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                // El manual de 2022 lo describía como un área de Discipulado; hoy es un
                // ministerio propio con encargado propio, y así se modela (Sección 4.3).
                Id = CoreSeedData.MinistryIds.Infantil,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Ministerio Infantil",
                Description = "Atiende a los niños de la congregación. Ministerio propio e " +
                              "independiente, con encargado propio igual que los otros cinco.",
                CreatedAt = CoreSeedData.SeededAt
            });
    }
}
