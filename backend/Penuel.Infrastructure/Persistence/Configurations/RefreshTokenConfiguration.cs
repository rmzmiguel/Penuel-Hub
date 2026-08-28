using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        // Solo se almacena el hash del token, nunca el token en claro (Sección 6.5).
        builder.Property(t => t.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne(t => t.UserAccount)
            .WithMany()
            .HasForeignKey(t => t.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token_hash");

        // Soporta la revocación masiva de sesiones de una cuenta (Sección 8.1).
        builder.HasIndex(t => new { t.UserAccountId, t.RevokedAt });
    }
}
