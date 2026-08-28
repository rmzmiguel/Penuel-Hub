using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class ChurchConfiguration : IEntityTypeConfiguration<Church>
{
    public void Configure(EntityTypeBuilder<Church> builder)
    {
        builder.ToTable("churches");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TimeZone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(c => c.Address)
            .HasMaxLength(300);

        builder.Property(c => c.FoundedYear);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Sección 4.2. Para esta fase existe exactamente una fila, creada aquí y nunca
        // vía endpoint (Sección 6.1).
        builder.HasData(new
        {
            Id = CoreSeedData.ChurchId,
            Name = "Comunidad Cristiana Penuel",
            TimeZone = "America/Mexico_City",
            Currency = "MXN",
            Address = "Manzana 8, Lote 2, Calle Enrique Higuera M, S/N, C.P. 87270, " +
                      "Colonia Ejido Loma Alta, Ciudad Victoria, Tamaulipas",
            FoundedYear = 1997,
            CreatedAt = CoreSeedData.SeededAt
        });
    }
}
