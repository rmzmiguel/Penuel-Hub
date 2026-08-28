using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class SocietyLeadershipConfiguration : IEntityTypeConfiguration<SocietyLeadership>
{
    public void Configure(EntityTypeBuilder<SocietyLeadership> builder)
    {
        builder.ToTable("society_leaderships");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.AssignedAt).IsRequired();

        builder.HasOne(l => l.Society)
            .WithMany()
            .HasForeignKey(l => l.SocietyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Person)
            .WithMany()
            .HasForeignKey(l => l.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(l => l.AssignedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(l => l.RevokedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Regla 7.11: a lo sumo UN liderazgo activo por sociedad.
        builder.HasIndex(l => l.SocietyId)
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_society_leaderships_active");

        builder.HasIndex(l => new { l.PersonId, l.RevokedAt });
    }
}
