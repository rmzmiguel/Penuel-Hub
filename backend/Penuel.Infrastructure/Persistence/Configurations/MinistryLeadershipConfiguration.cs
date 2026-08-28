using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class MinistryLeadershipConfiguration : IEntityTypeConfiguration<MinistryLeadership>
{
    public void Configure(EntityTypeBuilder<MinistryLeadership> builder)
    {
        builder.ToTable("ministry_leaderships");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.AssignedAt).IsRequired();

        builder.HasOne(l => l.Ministry)
            .WithMany()
            .HasForeignKey(l => l.MinistryId)
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

        // Regla 7.11: a lo sumo UN liderazgo activo por ministerio. La restricción es sobre
        // el RECURSO, no sobre la persona — nada impide que alguien lidere varios ministerios
        // a la vez, que en esta congregación es la norma (regla 7.13).
        builder.HasIndex(l => l.MinistryId)
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_ministry_leaderships_active");

        builder.HasIndex(l => new { l.PersonId, l.RevokedAt });
    }
}
