using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;

namespace Penuel.Infrastructure.Persistence.Configurations;

public sealed class ServiceSessionConfiguration : IEntityTypeConfiguration<ServiceSession>
{
    /// <summary>Dinero: hasta 9,999,999,999.99 con dos decimales exactos, sin redondeos al azar.</summary>
    private const int MoneyPrecision = 12;
    private const int MoneyScale = 2;

    public void Configure(EntityTypeBuilder<ServiceSession> builder)
    {
        builder.ToTable("service_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SessionDate).IsRequired();

        builder.Property(s => s.TotalOffering)
            .HasPrecision(MoneyPrecision, MoneyScale)
            .IsRequired();

        builder.Property(s => s.TotalTithe)
            .HasPrecision(MoneyPrecision, MoneyScale);

        builder.Property(s => s.CreatedByPersonId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasOne(s => s.ServiceType)
            .WithMany()
            .HasForeignKey(s => s.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Society)
            .WithMany()
            .HasForeignKey(s => s.SocietyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cuatro claves foráneas distintas hacia Person: quién dio la clase, quién predicó,
        // quién levantó el reporte y quién lo corrigió. Son hechos separados.
        builder.HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Preacher)
            .WithMany()
            .HasForeignKey(s => s.PreacherPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(s => s.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(s => s.UpdatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dos índices únicos PARCIALES, y hacen falta los dos (Sección 6.2).
        //
        // El primero evita dos sesiones de la misma Sociedad el mismo día.
        builder.HasIndex(s => new { s.ServiceTypeId, s.SessionDate, s.SocietyId })
            .IsUnique()
            .HasFilter("society_id IS NOT NULL")
            .HasDatabaseName("ux_service_sessions_by_society");

        // El segundo evita dos Cultos Generales (o de Oración, o de Jóvenes) el mismo día.
        // Sin él, Postgres permitiría duplicados: en un índice único trata cada NULL como
        // distinto de cualquier otro NULL, así que el índice de arriba no los alcanza.
        builder.HasIndex(s => new { s.ServiceTypeId, s.SessionDate })
            .IsUnique()
            .HasFilter("society_id IS NULL")
            .HasDatabaseName("ux_service_sessions_without_society");

        // Consulta caliente: el historial por rango de fechas (GetServiceSessionHistoryQuery).
        builder.HasIndex(s => s.SessionDate);
    }
}
