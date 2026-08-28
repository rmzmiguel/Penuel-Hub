using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Penuel.Application.Abstractions;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;
using Penuel.Domain.Entities;
using Penuel.Domain.Entities.Services;
using Penuel.Infrastructure.Persistence;
using Penuel.Infrastructure.Persistence.Seed;
using Penuel.Infrastructure.Security;

namespace Penuel.Application.Tests.Harness;

/// <summary>
/// Base de datos SQLite en memoria, aislada por prueba, con el pipeline real de MediatR
/// (autorización + validación) y las implementaciones reales de BCrypt, JWT y refresh tokens.
/// </summary>
/// <remarks>
/// Se usa SQLite y no el proveedor InMemory a propósito: InMemory ignora índices únicos,
/// índices parciales y claves foráneas, que es exactamente lo que este esquema necesita poder
/// comprobar. El esquema y el seed (iglesia, rol Pastor, 6 ministerios, 4 sociedades, 4 cargos)
/// los produce <c>EnsureCreated</c> a partir del MISMO modelo que genera la migración real.
/// </remarks>
public sealed class TestHarness : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    private TestHarness(SqliteConnection connection, ServiceProvider provider, IServiceScope scope)
    {
        _connection = connection;
        _provider = provider;
        _scope = scope;
    }

    public FakeCurrentUser CurrentUser { get; private init; } = null!;
    public FixedDateTimeProvider Clock { get; private init; } = null!;
    public IPasswordHasher PasswordHasher => _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    public ISender Sender => _scope.ServiceProvider.GetRequiredService<ISender>();
    public ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    public static async Task<TestHarness> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var currentUser = new FakeCurrentUser();
        var clock = new FixedDateTimeProvider(Start);

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<ICurrentUser>(currentUser);
        services.AddSingleton<IDateTimeProvider>(clock);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton(LockoutPolicy.Default);
        services.AddSingleton(Options.Create(new JwtOptions
        {
            Issuer = "penuel-api",
            Audience = "penuel-app",
            SecretKey = "clave-de-pruebas-de-al-menos-32-bytes-de-longitud",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 14
        }));

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        return new TestHarness(connection, provider, scope)
        {
            CurrentUser = currentUser,
            Clock = clock
        };
    }

    /// <summary>Crea una persona activa y devuelve su Id.</summary>
    public async Task<Guid> AddPersonAsync(string firstName = "Persona", string lastName = "De Prueba")
    {
        var person = Person.Register(
            CoreSeedData.ChurchId, firstName, lastName, null, null, null, Clock.UtcNow);

        Db.Persons.Add(person);
        await Db.SaveChangesAsync();
        return person.Id;
    }

    /// <summary>Crea una cuenta para una persona y devuelve su Id.</summary>
    public async Task<Guid> AddUserAccountAsync(Guid personId, string email, string password = "contrasena-de-prueba")
    {
        var account = UserAccount.Create(personId, email, PasswordHasher.Hash(password), Clock.UtcNow);
        Db.UserAccounts.Add(account);
        await Db.SaveChangesAsync();
        return account.Id;
    }

    /// <summary>
    /// Crea al Pastor y lo deja como usuario autenticado de la sesión de prueba: es el punto
    /// de partida de casi toda prueba, porque la regla por defecto de la Sección 8.2 exige
    /// ese rol en casi todos los casos de uso.
    /// </summary>
    public async Task<(Guid PersonId, Guid UserAccountId)> SignInAsPastorAsync(
        string email = "pastor@penuel.mx",
        string password = "contrasena-de-prueba")
    {
        var personId = await AddPersonAsync("Fermín", "Ramírez");
        var accountId = await AddUserAccountAsync(personId, email, password);

        Db.UserRoles.Add(UserRole.Assign(accountId, CoreSeedData.RoleIds.Pastor, personId, Clock.UtcNow));
        await Db.SaveChangesAsync();

        CurrentUser.SignInAs(personId, accountId, RoleNames.Pastor);
        return (personId, accountId);
    }

    /// <summary>
    /// Crea a alguien con el rol SundaySchoolRecorder y lo deja autenticado. NO le da ningún
    /// cargo ni asignación de maestro: tener el rol y ser maestro son hechos independientes
    /// (Sección 3.2 de la rama de Servicios).
    /// </summary>
    public async Task<(Guid PersonId, Guid UserAccountId)> SignInAsSundaySchoolRecorderAsync(
        string firstName = "Encargada",
        string lastName = "De Captura")
    {
        var personId = await AddPersonAsync(firstName, lastName);
        var accountId = await AddUserAccountAsync(personId, $"{Guid.NewGuid():N}@penuel.mx");

        Db.UserRoles.Add(UserRole.Assign(
            accountId, CoreSeedData.RoleIds.SundaySchoolRecorder, personId, Clock.UtcNow));
        await Db.SaveChangesAsync();

        CurrentUser.SignInAs(personId, accountId, RoleNames.SundaySchoolRecorder);
        return (personId, accountId);
    }

    /// <summary>
    /// Crea a un Desarrollador y lo deja autenticado. A propósito NO recibe cargo, ni
    /// liderazgo, ni membresía: el rol tiene que bastar solo, porque quien lo tiene es
    /// justamente alguien de fuera de la estructura de la iglesia.
    /// </summary>
    public async Task<(Guid PersonId, Guid UserAccountId)> SignInAsDeveloperAsync()
    {
        var personId = await AddPersonAsync("Miguel", "Ramírez");
        var accountId = await AddUserAccountAsync(personId, $"{Guid.NewGuid():N}@penuel.mx");

        Db.UserRoles.Add(UserRole.Assign(
            accountId, CoreSeedData.RoleIds.Developer, personId, Clock.UtcNow));
        await Db.SaveChangesAsync();

        CurrentUser.SignInAs(personId, accountId, RoleNames.Developer);
        return (personId, accountId);
    }

    /// <summary>
    /// Crea al Tesorero General y lo deja autenticado. Nótese que entra por su CARGO y SIN
    /// ningún rol de sistema: es el único punto donde un Position concede acceso (Sección 8.3).
    /// </summary>
    public async Task<(Guid PersonId, Guid UserAccountId)> SignInAsTreasurerAsync()
    {
        var personId = await AddPersonAsync("Tesorero", "General");
        var accountId = await AddUserAccountAsync(personId, $"{Guid.NewGuid():N}@penuel.mx");

        Db.PersonPositions.Add(PersonPosition.Assign(
            CoreSeedData.PositionIds.TesoreroGeneral, personId, personId, Clock.UtcNow));
        await Db.SaveChangesAsync();

        CurrentUser.SignInAs(personId, accountId);   // sin roles: solo el cargo
        return (personId, accountId);
    }

    /// <summary>Asigna a alguien como maestro. <c>societyId</c> nulo = sustituto flotante.</summary>
    public async Task<Guid> AddTeachingAssignmentAsync(Guid? societyId, Guid personId)
    {
        var assignment = SundaySchoolTeachingAssignment.Assign(
            societyId, personId, null, Clock.UtcNow);

        Db.SundaySchoolTeachingAssignments.Add(assignment);
        await Db.SaveChangesAsync();
        return assignment.Id;
    }

    /// <summary>Vuelve a leer una entidad desde la base, sin el caché del rastreador de cambios.</summary>
    public async Task<TEntity?> ReloadAsync<TEntity>(Guid id) where TEntity : class
    {
        Db.ChangeTracker.Clear();
        return await Db.FindAsync<TEntity>(id);
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>Aserciones sobre <see cref="Result"/> repetidas en todas las pruebas.</summary>
public static class ResultAssertions
{
    public static void ShouldSucceed(this Result result) =>
        Xunit.Assert.True(result.IsSuccess, $"Se esperaba éxito pero falló con: {result.Error}");

    public static void ShouldFailWith(this Result result, string expectedCode, ErrorType expectedType)
    {
        Xunit.Assert.True(result.IsFailure, "Se esperaba un fallo pero la operación tuvo éxito.");
        Xunit.Assert.Equal(expectedCode, result.Error!.Code);
        Xunit.Assert.Equal(expectedType, result.Error.Type);
    }
}
