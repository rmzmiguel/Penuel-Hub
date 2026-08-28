using Microsoft.EntityFrameworkCore;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.FamilyGroups;
using Penuel.Domain.Entities.Services;

namespace Penuel.Application.Abstractions;

/// <summary>
/// Acceso a datos visto desde la capa de aplicación. Penuel.Application referencia el paquete
/// base de EF Core (necesario para <c>DbSet&lt;T&gt;</c> y los operadores LINQ asíncronos), pero
/// NO referencia a Npgsql: sigue sin saber que la base de datos es PostgreSQL.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Church> Churches { get; }
    DbSet<Person> Persons { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Society> Societies { get; }
    DbSet<SocietyLeadership> SocietyLeaderships { get; }
    DbSet<SocietyMembership> SocietyMemberships { get; }
    DbSet<Ministry> Ministries { get; }
    DbSet<MinistryLeadership> MinistryLeaderships { get; }
    DbSet<Position> Positions { get; }
    DbSet<PersonPosition> PersonPositions { get; }

    // --- Rama de Servicios y Cultos ---
    DbSet<ServiceType> ServiceTypes { get; }
    DbSet<ServiceSession> ServiceSessions { get; }
    DbSet<ServiceAttendance> ServiceAttendances { get; }
    DbSet<TitheEntry> TitheEntries { get; }
    DbSet<SundaySchoolTeachingAssignment> SundaySchoolTeachingAssignments { get; }

    // --- Rama de Grupos Familiares ---
    DbSet<FamilyGroup> FamilyGroups { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<FamilyGroupMeeting> FamilyGroupMeetings { get; }
    DbSet<FamilyGroupAttendance> FamilyGroupAttendances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
