using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class TitheEntryConfiguration : IEntityTypeConfiguration<TitheEntry>
{
    private const int MoneyPrecision = 12;
    private const int MoneyScale = 2;

    public void Configure(EntityTypeBuilder<TitheEntry> builder)
    {
        builder.ToTable("tithe_entries");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(MoneyPrecision, MoneyScale)
            .IsRequired();

        builder.Property(t => t.CreatedByPersonId).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // NOTA: que la sesión sea de un ServiceType con CollectsTithe = true se valida en
        // Penuel.Application, NO con un CHECK de base de datos (Sección 6.4). Un CHECK tendría
        // que atravesar dos tablas y quedaría fuera del alcance de una restricción de columna.
        builder.HasOne(t => t.ServiceSession)
            .WithMany()
            .HasForeignKey(t => t.ServiceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Person)
            .WithMany()
            .HasForeignKey(t => t.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // A lo sumo un registro de diezmo por persona por sesión: corregirlo es un UPDATE
        // controlado, no una fila nueva (Sección 6.4).
        builder.HasIndex(t => new { t.ServiceSessionId, t.PersonId })
            .IsUnique()
            .HasDatabaseName("ux_tithe_entries_session_person");
    }
}
