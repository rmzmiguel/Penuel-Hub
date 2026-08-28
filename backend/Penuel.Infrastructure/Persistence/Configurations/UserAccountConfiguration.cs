using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(u => u.Id);

        // 320 = longitud máxima real de una dirección de correo según RFC 5321.
        builder.Property(u => u.Email)
            .HasMaxLength(320)
            .IsRequired();

        // Un hash BCrypt ocupa 60 caracteres; el margen deja espacio a un cambio
        // de algoritmo futuro sin necesidad de migrar la columna.
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.FailedLoginAttempts).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // Regla 7.1: una Person tiene como máximo un UserAccount (1 a 0..1).
        builder.HasOne(u => u.Person)
            .WithOne()
            .HasForeignKey<UserAccount>(u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.PersonId).IsUnique();

        // El email se normaliza a minúsculas en el Dominio, así que el índice
        // único basta para garantizar unicidad insensible a mayúsculas.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_user_accounts_email");
    }
}
