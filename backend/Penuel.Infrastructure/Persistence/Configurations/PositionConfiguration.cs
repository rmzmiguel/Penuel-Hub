using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.IsExecutiveBody).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(p => p.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ChurchId, p.Name })
            .IsUnique()
            .HasDatabaseName("ux_positions_church_id_name");

        // Soporta el cómputo del Cuerpo Ejecutivo (regla 7.9).
        builder.HasIndex(p => p.IsExecutiveBody);

        // Los cuatro cargos de la Sección 4.3, todos con IsExecutiveBody = true: son
        // exactamente quienes componen el Cuerpo Ejecutivo, que se COMPUTA a partir de
        // este flag y nunca se almacena aparte (regla 7.9).
        builder.HasData(
            new
            {
                Id = CoreSeedData.PositionIds.Pastor,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Pastor",
                Description = "Máxima autoridad. Preside la Asamblea General de miembros y el Cuerpo Ejecutivo.",
                IsExecutiveBody = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.PositionIds.Diacono,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Diácono",
                Description = "Oficio eclesiástico. Admite varios titulares activos a la vez (Sección 6.13).",
                IsExecutiveBody = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.PositionIds.SecretarioGeneral,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Secretario General",
                Description = "Registros y actas de la iglesia. Integra el Cuerpo Ejecutivo.",
                IsExecutiveBody = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                Id = CoreSeedData.PositionIds.TesoreroGeneral,
                ChurchId = CoreSeedData.ChurchId,
                Name = "Tesorero General",
                Description = "Administra la Ofrenda. El registro y control de los Diezmos corresponde " +
                              "directamente al Pastor (regla 7.12).",
                IsExecutiveBody = true,
                CreatedAt = CoreSeedData.SeededAt
            });
    }
}
