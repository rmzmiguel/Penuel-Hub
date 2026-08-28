using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class PersonPositionConfiguration : IEntityTypeConfiguration<PersonPosition>
{
    public void Configure(EntityTypeBuilder<PersonPosition> builder)
    {
        builder.ToTable("person_positions");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.AssignedAt).IsRequired();

        builder.HasOne(pp => pp.Position)
            .WithMany()
            .HasForeignKey(pp => pp.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pp => pp.Person)
            .WithMany()
            .HasForeignKey(pp => pp.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(pp => pp.AssignedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(pp => pp.RevokedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // ATENCIÓN — deliberadamente NO hay índice único sobre PositionId solo:
        // la Sección 6.13 exige que un mismo cargo admita varios titulares activos
        // a la vez (varios Diáconos).
        //
        // El índice de abajo es sobre el PAR (PositionId, PersonId): impide únicamente
        // que la MISMA persona figure dos veces como titular ACTIVO del MISMO cargo,
        // que sería un duplicado sin sentido. No limita cuántas personas ocupan el cargo,
        // ni cuántos cargos acumula una persona (regla 7.13).
        builder.HasIndex(pp => new { pp.PositionId, pp.PersonId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_person_positions_active");

        builder.HasIndex(pp => new { pp.PersonId, pp.RevokedAt });
    }
}
