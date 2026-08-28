using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class SocietyMembershipConfiguration : IEntityTypeConfiguration<SocietyMembership>
{
    public void Configure(EntityTypeBuilder<SocietyMembership> builder)
    {
        builder.ToTable("society_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AssignedAt).IsRequired();

        builder.HasOne(m => m.Society)
            .WithMany()
            .HasForeignKey(m => m.SocietyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Person)
            .WithMany()
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.AssignedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.RevokedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Impide el duplicado exacto y nada más: una persona puede pertenecer a varias
        // Sociedades, y una Sociedad tiene tantas personas como haga falta (regla 7.13).
        builder.HasIndex(m => new { m.SocietyId, m.PersonId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_society_memberships_active");

        // Consulta caliente: los integrantes de un grupo al abrir el reporte dominical.
        builder.HasIndex(m => new { m.SocietyId, m.RevokedAt });
    }
}
