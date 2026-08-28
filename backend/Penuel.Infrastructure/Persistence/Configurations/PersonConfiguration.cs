using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        // El enum se guarda como texto para que la tabla sea legible al administrarla
        // directamente desde Supabase (Sección 4.7), no como un entero opaco.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(p => p.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auto-referencias de auditoría (regla 7.4). Sin navegación: solo integridad referencial.
        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.LastName, p.FirstName });
    }
}
