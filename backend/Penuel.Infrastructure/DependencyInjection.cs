using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Penuel.Application.Abstractions;
using Penuel.Infrastructure.Persistence;
using Penuel.Infrastructure.Security;
using Penuel.Infrastructure.Time;

namespace Penuel.Infrastructure;

/// <summary>Registro de los servicios de infraestructura.</summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "Penuel";
    public const string LockoutSectionName = "Authentication:Lockout";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddPersistence(configuration)
            .AddSecurity(configuration);
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{ConnectionStringName}' en la configuración.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                // snake_case en tablas y columnas: consistente con la convención de
                // Postgres/Supabase y con los índices parciales de este proyecto (Sección 5.1).
                .UseSnakeCaseNamingConvention());

        // La capa de aplicación solo conoce la interfaz; nunca el DbContext concreto.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        // Validación temprana: si la configuración de JWT es inservible, la aplicación
        // no arranca, en lugar de fallar en el primer login.
        var jwtOptions = new JwtOptions();
        jwtSection.Bind(jwtOptions);
        jwtOptions.Validate();

        services.Configure<JwtOptions>(jwtSection);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton(ReadLockoutPolicy(configuration));

        return services;
    }

    private static LockoutPolicy ReadLockoutPolicy(IConfiguration configuration)
    {
        var section = configuration.GetSection(LockoutSectionName);

        var maxAttempts = section.GetValue<int?>("MaxFailedAttempts")
            ?? LockoutPolicy.Default.MaxFailedAttempts;

        var minutes = section.GetValue<int?>("LockoutMinutes")
            ?? (int)LockoutPolicy.Default.LockoutDuration.TotalMinutes;

        return new LockoutPolicy(maxAttempts, TimeSpan.FromMinutes(minutes));
    }
}
