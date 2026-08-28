// =============================================================================================
//  PENUEL — SIEMBRA DEL PRIMER PASTOR (USO ÚNICO DE ARRANQUE)
// =============================================================================================
//
//  Este programa existe para resolver el problema del huevo y la gallina de la regla 7.6:
//  la regla 7.5 exige el rol Pastor para otorgar roles, así que el PRIMER Pastor no puede
//  crearse por el flujo normal de la API. Nadie puede.
//
//  Se ejecuta UNA SOLA VEZ, al poner en marcha el sistema. Después de eso, toda alta de
//  personas, cuentas y roles pasa por la API como cualquier otra operación. El programa se
//  niega a ejecutarse dos veces.
//
//  La contraseña se pide en la terminal, no se muestra en pantalla, y no se guarda en ningún
//  lado: solo se usa para generar el hash BCrypt. Ni siquiera este código llega a verla escrita.
//
//  Uso:   dotnet run --project Penuel.Bootstrap
//         (toma la cadena de conexión del mismo almacén de user-secrets que Penuel.WebApi)
//
// =============================================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Penuel.Bootstrap;
using Penuel.Domain.Entities;
using Penuel.Infrastructure.Persistence;
using Penuel.Infrastructure.Persistence.Seed;
using Penuel.Infrastructure.Security;

const string ConnectionStringName = "Penuel";

// Datos reales del pastor titular (Sección 4.2 del documento maestro).
const string FirstName = "Fermín";
const string LastName = "Ramírez Vázquez";
var joinedAt = new DateOnly(1997, 1, 1);   // Al frente de la iglesia desde 1997.

Console.WriteLine();
Console.WriteLine("===========================================================");
Console.WriteLine("  PENUEL — Siembra del primer Pastor");
Console.WriteLine("  USO ÚNICO DE ARRANQUE (regla 7.6)");
Console.WriteLine("===========================================================");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString(ConnectionStringName)
    ?? Environment.GetEnvironmentVariable("PENUEL_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        $"No se encontró la cadena de conexión '{ConnectionStringName}'.\n" +
        "Configúrala con:\n" +
        "  dotnet user-secrets set \"ConnectionStrings:Penuel\" \"...\" --project Penuel.WebApi");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

await using var db = new ApplicationDbContext(options);

// ---------------------------------------------------------------------------------------------
// Comprobaciones previas. Si algo falta o si ya se sembró antes, se aborta sin tocar nada.
// ---------------------------------------------------------------------------------------------

if (!await db.Database.CanConnectAsync())
{
    Console.Error.WriteLine("No se pudo conectar a la base de datos. Revisa la cadena de conexión.");
    return 1;
}

var pendientes = await db.Database.GetPendingMigrationsAsync();
if (pendientes.Any())
{
    Console.Error.WriteLine(
        "Hay migraciones sin aplicar: " + string.Join(", ", pendientes) + "\n" +
        "Ejecuta primero: dotnet ef database update --project Penuel.Infrastructure --startup-project Penuel.WebApi");
    return 1;
}

var church = await db.Churches.FirstOrDefaultAsync(c => c.Id == CoreSeedData.ChurchId);
var pastorRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == CoreSeedData.RoleIds.Pastor);
var pastorPosition = await db.Positions.FirstOrDefaultAsync(p => p.Id == CoreSeedData.PositionIds.Pastor);

if (church is null || pastorRole is null || pastorPosition is null)
{
    Console.Error.WriteLine(
        "Falta el seed de la migración inicial (iglesia, rol Pastor o cargo Pastor).\n" +
        "La base no está en el estado que este programa espera.");
    return 1;
}

// La condición de "ya se sembró": existe alguien con el rol Pastor ACTIVO.
var yaHayPastor = await db.UserRoles
    .AnyAsync(ur => ur.RoleId == CoreSeedData.RoleIds.Pastor && ur.RevokedAt == null);

if (yaHayPastor)
{
    Console.Error.WriteLine(
        "ABORTADO: ya existe una cuenta con el rol Pastor activo.\n" +
        "Este programa es de USO ÚNICO. A partir de aquí, las altas se hacen por la API\n" +
        "(POST /api/persons, POST /api/persons/{id}/user-account, POST /api/roles/assign).");
    return 1;
}

// ---------------------------------------------------------------------------------------------
// Datos que sí se piden. El resto ya está decidido y no se pregunta.
// ---------------------------------------------------------------------------------------------

Console.WriteLine($"Se va a crear el registro de: {FirstName} {LastName}");
Console.WriteLine($"Iglesia:            {church.Name}");
Console.WriteLine($"Miembro oficial desde: {joinedAt:yyyy-MM-dd}");
Console.WriteLine("Teléfono y fecha de nacimiento: se dejan en blanco.");
Console.WriteLine();

string email;
string password;

try
{
    ConsoleInput.RequireInteractiveTerminal();
    email = ConsoleInput.ReadEmail("Correo para iniciar sesión: ");
    password = ConsoleInput.ReadPasswordTwice();
}
catch (ConsoleInput.NoInteractiveInputException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine("No se escribió nada en la base de datos.");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Se crearán, en una sola transacción:");
Console.WriteLine($"  1. Person            {FirstName} {LastName}");
Console.WriteLine($"  2. UserAccount       {UserAccount.NormalizeEmail(email)}");
Console.WriteLine($"  3. Membership        activa, desde {joinedAt:yyyy-MM-dd}");
Console.WriteLine("  4. UserRole          Pastor        (permiso de sistema — RBAC)");
Console.WriteLine("  5. PersonPosition    Pastor        (cargo — computa el Cuerpo Ejecutivo)");
Console.WriteLine();

if (!ConsoleInput.Confirm("Escribe SI para confirmar: "))
{
    Console.WriteLine("Cancelado. No se escribió nada.");
    return 1;
}

// ---------------------------------------------------------------------------------------------
// Siembra. Todo o nada.
// ---------------------------------------------------------------------------------------------

var now = DateTimeOffset.UtcNow;
var hasher = new BCryptPasswordHasher();

await using var transaction = await db.Database.BeginTransactionAsync();

try
{
    // 1. Person. Nace sin auditoría de creador porque literalmente no había nadie antes que él.
    var person = Person.Register(
        church.Id, FirstName, LastName,
        dateOfBirth: null, phoneNumber: null,
        createdByPersonId: null, now);

    db.Persons.Add(person);
    await db.SaveChangesAsync();

    // Ahora que ya tiene Id, la auditoría se apunta a sí misma: el Pastor es el origen de la
    // cadena de auditoría de todo el sistema. Se hace por UPDATE porque el Id lo genera la
    // fábrica del dominio y no puede conocerse antes de construir la entidad.
    await db.Persons
        .Where(p => p.Id == person.Id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(p => p.CreatedByPersonId, person.Id)
            .SetProperty(p => p.UpdatedByPersonId, person.Id));

    // 2. UserAccount.
    var account = UserAccount.Create(person.Id, email, hasher.Hash(password), now);
    db.UserAccounts.Add(account);

    // 3. Membership — miembro oficial desde 1997.
    var membership = Membership.Create(person.Id, church.Id, joinedAt, person.Id, now);
    db.Memberships.Add(membership);

    // 4. UserRole = Pastor — el permiso de sistema (Sección 3.4, eje RBAC).
    db.UserRoles.Add(UserRole.Assign(account.Id, pastorRole.Id, person.Id, now));

    // 5. PersonPosition = Pastor — el cargo eclesiástico, para que el cómputo del
    //    Cuerpo Ejecutivo (regla 7.9) funcione desde el primer día. Es un eje DISTINTO
    //    del rol de arriba: por eso se registran los dos.
    db.PersonPositions.Add(PersonPosition.Assign(pastorPosition.Id, person.Id, person.Id, now));

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    Console.WriteLine();
    Console.WriteLine("LISTO. Se crearon las cinco filas.");
    Console.WriteLine($"  PersonId       {person.Id}");
    Console.WriteLine($"  UserAccountId  {account.Id}");
    Console.WriteLine();
    Console.WriteLine("Ya puedes iniciar sesión en POST /api/auth/login con ese correo.");
    Console.WriteLine("Este programa no debe volver a ejecutarse.");
    return 0;
}
catch (Exception exception)
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine();
    Console.Error.WriteLine("FALLÓ. Se revirtió todo; la base quedó como estaba.");
    Console.Error.WriteLine(exception.Message);
    return 1;
}
