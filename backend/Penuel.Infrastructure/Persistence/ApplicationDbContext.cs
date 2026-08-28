using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Entities.Services;

namespace Penuel.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia del Core. Toda la configuración se hace con Fluent API
/// (<see cref="IEntityTypeConfiguration{TEntity}"/>): cero Data Annotations en el Dominio (Sección 5.1).
/// Los nombres de tablas y columnas se generan en snake_case mediante
/// <c>UseSnakeCaseNamingConvention()</c>, configurado al registrar el contexto.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Church> Churches => Set<Church>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Society> Societies => Set<Society>();
    public DbSet<SocietyLeadership> SocietyLeaderships => Set<SocietyLeadership>();
    public DbSet<SocietyMembership> SocietyMemberships => Set<SocietyMembership>();
    public DbSet<Ministry> Ministries => Set<Ministry>();
    public DbSet<MinistryLeadership> MinistryLeaderships => Set<MinistryLeadership>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PersonPosition> PersonPositions => Set<PersonPosition>();

    // --- Rama de Servicios y Cultos ---
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServiceSession> ServiceSessions => Set<ServiceSession>();
    public DbSet<ServiceAttendance> ServiceAttendances => Set<ServiceAttendance>();
    public DbSet<TitheEntry> TitheEntries => Set<TitheEntry>();
    public DbSet<SundaySchoolTeachingAssignment> SundaySchoolTeachingAssignments =>
        Set<SundaySchoolTeachingAssignment>();

    // --- Rama de Grupos Familiares ---
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<FamilyGroupMeeting> FamilyGroupMeetings => Set<FamilyGroupMeeting>();
    public DbSet<FamilyGroupAttendance> FamilyGroupAttendances => Set<FamilyGroupAttendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
