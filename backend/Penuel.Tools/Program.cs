// =============================================================================================
//  PENUEL — RESTABLECER LA CONTRASEÑA DE UNA CUENTA
// =============================================================================================
//
//  Existe porque el sistema NO tiene recuperación de contraseña por correo, y no la tiene a
//  propósito: no manda correos a nadie. Cuando alguien olvida la suya, el camino normal es que
//  el Pastor le cree una cuenta nueva desde la aplicación. Esta herramienta cubre el caso que
//  ese camino no alcanza — que sea la cuenta DEL PROPIO Pastor la que se perdió, o que haga
//  falta devolver el acceso sin tocar la persona ni sus permisos.
//
//  Usa el MISMO hasher que la API (BCrypt, work factor 12). Generar el hash por fuera sería
//  la forma más fácil de que un día dejen de coincidir.
//
//  La contraseña la GENERA el programa y se muestra UNA vez. No se pide por teclado: una que
//  uno inventa sobre la marcha acaba siendo débil o repetida, y aquí no hay ninguna razón para
//  que lo sea.
//
//  Uso:  dotnet run --project Penuel.Tools -- correo@ejemplo.mx [otro@ejemplo.mx ...]
//
// =============================================================================================

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Penuel.Infrastructure.Persistence;
using Penuel.Infrastructure.Security;

if (args.Length == 0)
{
    Console.WriteLine("Uso: dotnet run --project Penuel.Tools -- correo@ejemplo.mx [otro@ejemplo.mx ...]");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("Penuel")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Penuel");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Falta la cadena de conexión. Configúrala con user-secrets o ConnectionStrings__Penuel.");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

await using var db = new ApplicationDbContext(options);
var hasher = new BCryptPasswordHasher();
var ahora = DateTimeOffset.UtcNow;

Console.WriteLine();
Console.WriteLine("===========================================================");
Console.WriteLine("  Contraseñas nuevas — anótalas ahora, no se vuelven a ver");
Console.WriteLine("===========================================================");
Console.WriteLine();

var fallos = 0;

foreach (var correo in args)
{
    var normalizado = correo.Trim().ToLowerInvariant();

    var cuenta = await db.UserAccounts
        .Include(u => u.Person)
        .FirstOrDefaultAsync(u => u.Email == normalizado);

    if (cuenta is null)
    {
        Console.WriteLine($"  ✗ {correo} — no existe ninguna cuenta con ese correo");
        fallos++;
        continue;
    }

    var clave = GenerarClave();
    cuenta.ChangePassword(hasher.Hash(clave), ahora);

    // Se cierran las sesiones vivas. Si la contraseña se restablece es porque alguien perdió
    // el control de la cuenta o el acceso a ella; dejar abiertas las sesiones anteriores
    // vaciaría de sentido el cambio.
    var vivas = await db.RefreshTokens
        .Where(t => t.UserAccountId == cuenta.Id && t.RevokedAt == null)
        .ToListAsync();

    foreach (var token in vivas)
    {
        token.Revoke(ahora);
    }

    Console.WriteLine($"  {cuenta.Person.FirstName} {cuenta.Person.LastName}");
    Console.WriteLine($"    correo:      {cuenta.Email}");
    Console.WriteLine($"    contraseña:  {clave}");
    Console.WriteLine($"    sesiones cerradas: {vivas.Count}");
    Console.WriteLine();
}

await db.SaveChangesAsync();
Console.WriteLine("Listo.");
return fallos == 0 ? 0 : 1;

/// <summary>
/// Contraseña legible en voz alta: sin caracteres que se confundan al dictarla —ni l ni 1,
/// ni O ni 0— porque estas se dictan por teléfono o se anotan en un papel. La entropía la
/// pone la longitud, no los símbolos raros.
/// </summary>
static string GenerarClave()
{
    const string alfabeto = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var bloques = new List<string>();

    for (var b = 0; b < 3; b++)
    {
        var bloque = new char[5];
        for (var i = 0; i < bloque.Length; i++)
        {
            bloque[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
        }
        bloques.Add(new string(bloque));
    }

    return string.Join('-', bloques);
}
