using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Penuel.Application.Abstractions;
using Penuel.Domain.Enums;

namespace Penuel.Infrastructure.Security;

public static class JwtBearerSetup
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
        jwtOptions.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Sin esto, ASP.NET reescribe 'sub' y 'role' a URIs largas de WS-Federation
                // y los claims dejarían de llamarse como los emite JwtProvider.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidateLifetime = true,
                    // Sin tolerancia: un token expirado está expirado. El valor por omisión
                    // de 5 minutos alargaría gratis la ventana de un token comprometido.
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = JwtProvider.RoleClaim
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateAgainstDatabaseAsync
                };
            });

        return services;
    }

    /// <summary>
    /// Revalidación por petición contra la base de datos (Sección 8.1).
    /// </summary>
    /// <remarks>
    /// Un JWT es criptográficamente válido hasta su expiración natural aunque se le revoque el
    /// rol un segundo después de emitirlo. Sin esta comprobación, la Definition of Done
    /// ("revocar un rol invalida el acceso sin esperar a que expire su token") sería falsa.
    /// Se verifican tres cosas, y cualquiera que falle invalida la autenticación:
    ///   1. la cuenta sigue activa;
    ///   2. la Person asociada sigue con Status = Active (regla 7.15 — el mismo candado del login);
    ///   3. todo rol que el token reclama sigue teniendo una fila UserRole ACTIVA.
    /// Es una consulta extra por petición; al volumen de esta iglesia el costo es irrelevante.
    /// </remarks>
    private static async Task ValidateAgainstDatabaseAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (principal is null)
        {
            context.Fail("El token no produjo una identidad válida.");
            return;
        }

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            context.Fail("El token no contiene un identificador de cuenta válido.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

        var snapshot = await db.UserAccounts
            .Where(u => u.Id == userAccountId)
            .Select(u => new { u.IsActive, PersonStatus = u.Person.Status })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (snapshot is null || !snapshot.IsActive)
        {
            context.Fail("La cuenta ya no está activa.");
            return;
        }

        if (snapshot.PersonStatus != PersonStatus.Active)
        {
            context.Fail("La persona asociada a la cuenta ya no está activa.");
            return;
        }

        var claimedRoles = principal.FindAll(JwtProvider.RoleClaim)
            .Select(c => c.Value)
            .ToList();

        if (claimedRoles.Count == 0)
        {
            return;
        }

        var activeRoles = await db.UserRoles
            .Where(ur => ur.UserAccountId == userAccountId && ur.RevokedAt == null)
            .Select(ur => ur.Role.Name)
            .ToListAsync(context.HttpContext.RequestAborted);

        var revoked = claimedRoles
            .Where(claimed => !activeRoles.Contains(claimed, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (revoked.Count > 0)
        {
            context.Fail($"El token reclama roles que ya fueron revocados: {string.Join(", ", revoked)}.");
        }
    }
}
