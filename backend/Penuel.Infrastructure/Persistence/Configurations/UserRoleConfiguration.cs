using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.AssignedAt).IsRequired();

        builder.HasOne(ur => ur.UserAccount)
            .WithMany()
            .HasForeignKey(ur => ur.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auditoría como PersonId, no como UserAccountId (regla 7.4 y corrección 6.7.1).
        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(ur => ur.AssignedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(ur => ur.RevokedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Regla 7.11 / corrección 6.7.2: impide dos asignaciones ACTIVAS del mismo rol
        // a la misma cuenta, sin impedir reasignarlo después de haberlo revocado.
        builder.HasIndex(ur => new { ur.UserAccountId, ur.RoleId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_user_roles_active");

        // Consulta caliente: los roles activos de una cuenta, en cada petición (Sección 8.1).
        builder.HasIndex(ur => new { ur.UserAccountId, ur.RevokedAt });
    }
}
