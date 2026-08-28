using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class FamilyGroupConfiguration : IEntityTypeConfiguration<FamilyGroup>
{
    public void Configure(EntityTypeBuilder<FamilyGroup> builder)
    {
        builder.ToTable("family_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Address).IsRequired().HasMaxLength(300);
        builder.Property(g => g.Status).IsRequired().HasConversion<int>();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();

        // System.DayOfWeek se guarda como entero. Es informativo (regla 7.7): ninguna
        // consulta lo usa para validar la fecha real de una reunión.
        builder.Property(g => g.DefaultMeetingDayOfWeek).IsRequired().HasConversion<int>();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(g => g.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Anfitrión y Encargado son DOS relaciones distintas a Person, y las dos obligatorias
        // (regla 7.1). Con frecuencia apuntan a la misma fila, cosa que la base admite sin
        // problema: son dos claves foráneas independientes.
        builder.HasOne(g => g.Host)
            .WithMany()
            .HasForeignKey(g => g.HostPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Leader)
            .WithMany()
            .HasForeignKey(g => g.LeaderPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(g => g.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(g => g.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================================================
        //  SIN índice único sobre HostPersonId ni LeaderPersonId, y es deliberado.
        //  Nada impide que una misma persona sea Anfitriona de dos casas, ni que sea
        //  Encargada de un grupo y Anfitriona de otro. La restricción de "uno solo"
        //  de esta rama es sobre GroupMember, que es otra cosa (ver 6.2).
        // ==========================================================================

        // Alimentan GetMyFamilyGroupsQuery, que es la consulta más caliente de la rama:
        // se ejecuta al abrir la aplicación para decidir qué pantalla mostrar.
        builder.HasIndex(g => new { g.HostPersonId, g.Status });
        builder.HasIndex(g => new { g.LeaderPersonId, g.Status });
    }
}
