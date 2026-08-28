using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Constants;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(r => r.IsSystemRole).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasOne<Church>()
            .WithMany()
            .HasForeignKey(r => r.ChurchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.ChurchId, r.Name })
            .IsUnique()
            .HasDatabaseName("ux_roles_church_id_name");

        // Único rol de sistema de esta fase. IsSystemRole impide que la futura UI lo edite
        // o lo borre: sin él, nadie podría otorgar ningún rol (regla 7.5).
        builder.HasData(
            new
            {
                Id = CoreSeedData.RoleIds.Pastor,
                ChurchId = CoreSeedData.ChurchId,
                Name = RoleNames.Pastor,
                Description = "Control total del sistema: gestiona personas, membresías, roles, " +
                              "ministerios, sociedades y cargos.",
                IsSystemRole = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                // Agregado por la rama de Servicios. Sin ESTA FILA, la constante en RoleNames
                // no serviría de nada: AssignRoleCommand busca el rol por nombre en la tabla,
                // y nadie podría recibirlo nunca.
                Id = CoreSeedData.RoleIds.SundaySchoolRecorder,
                ChurchId = CoreSeedData.ChurchId,
                Name = RoleNames.SundaySchoolRecorder,
                Description = "Puede levantar y corregir los reportes de Escuela Dominical de " +
                              "cualquier grupo. No implica ser maestro de ninguno.",
                IsSystemRole = true,
                CreatedAt = CoreSeedData.SeededAt
            },
            new
            {
                // Llave de servicio para quien construye y mantiene el sistema. No es un puesto
                // dentro de la congregación: quien lo tiene no es miembro, ni tiene cargo, ni
                // lidera nada. Salta la autorización en lugar de acumular permisos — el porqué
                // está en RoleNames.Developer.
                Id = CoreSeedData.RoleIds.Developer,
                ChurchId = CoreSeedData.ChurchId,
                Name = RoleNames.Developer,
                Description = "Acceso irrestricto para el mantenimiento técnico del sistema. " +
                              "No es un cargo de la iglesia y no implica membresía ni liderazgo.",
                IsSystemRole = true,
                CreatedAt = CoreSeedData.SeededAt
            });
    }
}
