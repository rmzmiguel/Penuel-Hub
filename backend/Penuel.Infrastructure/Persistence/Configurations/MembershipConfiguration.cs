using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        // Regla 7.2: una Person tiene como máximo un Membership (1 a 0..1).
        builder.HasOne(m => m.Person)
            .WithOne()
            .HasForeignKey<Membership>(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.PersonId).IsUnique();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(m => m.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(m => m.RegisteredByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.Status);
    }
}
