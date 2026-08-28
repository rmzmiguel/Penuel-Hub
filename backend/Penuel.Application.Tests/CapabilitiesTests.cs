using Microsoft.EntityFrameworkCore;
using Penuel.Application.Capabilities.GetExecutiveBody;
using Penuel.Application.Capabilities.GetMyCapabilities;
using Penuel.Application.Memberships.CreateMembership;
using Penuel.Application.Ministries.AssignMinistryLeader;
using Penuel.Application.Positions.AssignPosition;
using Penuel.Application.Positions.CreatePosition;
using Penuel.Application.Positions.RevokePosition;
using Penuel.Application.Roles.RevokeRole;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests;

public sealed class CapabilitiesTests
{
    [Fact]
    public async Task El_cuerpo_ejecutivo_no_tiene_tabla_propia_en_el_esquema()
    {
        // Regla 7.9: el Cuerpo Ejecutivo se COMPUTA y jamás se almacena. Si alguien agregara
        // una tabla para guardarlo, se desincronizaría de la fuente real de verdad —
        // esta prueba lo detectaría.
        await using var h = await TestHarness.CreateAsync();

        var tablas = h.Db.Model.GetEntityTypes()
            .Select(e => e.GetTableName()!)
            .ToList();

        // Las 13 tablas del Core siguen ahí. Se comprueban por NOMBRE y no por conteo:
        // un total exacto se rompería con cada rama nueva sin que eso signifique nada,
        // y el conteo nunca fue lo que esta prueba protege.
        string[] nucleo =
        [
            "churches", "persons", "memberships", "user_accounts", "refresh_tokens",
            "roles", "user_roles", "societies", "society_leaderships",
            "ministries", "ministry_leaderships", "positions", "person_positions"
        ];
        Assert.All(nucleo, t => Assert.Contains(t, tablas));

        // Lo que de verdad se vigila: que nadie haya creado una tabla para ALMACENAR
        // el Cuerpo Ejecutivo, ni en el Core ni en ninguna rama posterior.
        Assert.DoesNotContain(tablas, t => t.Contains("executive", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tablas, t => t.Contains("ejecutivo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetExecutiveBody_computa_a_partir_de_los_cargos_activos()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Pastor, pastorId));

        var tesorero = await h.AddPersonAsync("Tesorero", "Hermano");
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.TesoreroGeneral, tesorero));

        var result = await h.Sender.Send(new GetExecutiveBodyQuery());

        result.ShouldSucceed();
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, m => m.PersonId == pastorId);
        Assert.Contains(result.Value, m => m.PersonId == tesorero);
    }

    [Fact]
    public async Task GetExecutiveBody_agrupa_los_varios_cargos_de_una_misma_persona()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Pastor, pastorId));
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, pastorId));

        var result = await h.Sender.Send(new GetExecutiveBodyQuery());

        result.ShouldSucceed();
        var miembro = Assert.Single(result.Value);          // una sola persona...
        Assert.Equal(2, miembro.Positions.Count);            // ...con sus dos cargos
    }

    [Fact]
    public async Task GetExecutiveBody_excluye_los_cargos_revocados()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, pastorId));
        await h.Sender.Send(new RevokePositionCommand(CoreSeedData.PositionIds.Diacono, pastorId));

        var result = await h.Sender.Send(new GetExecutiveBodyQuery());

        result.ShouldSucceed();
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetExecutiveBody_ignora_los_cargos_ajenos_al_cuerpo_ejecutivo()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var ujier = (await h.Sender.Send(new CreatePositionCommand("Ujier", null, false))).Value;
        await h.Sender.Send(new AssignPositionCommand(ujier, pastorId));

        var result = await h.Sender.Send(new GetExecutiveBodyQuery());

        result.ShouldSucceed();
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetMyCapabilities_devuelve_los_tres_ejes_por_separado()
    {
        // Sección 8.4: es lo que el frontend usa para armar su navegación.
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        await h.Sender.Send(new CreateMembershipCommand(pastorId, null));
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Pastor, pastorId));
        await h.Sender.Send(new AssignMinistryLeaderCommand(CoreSeedData.MinistryIds.Adoracion, pastorId));

        var result = await h.Sender.Send(new GetMyCapabilitiesQuery());

        result.ShouldSucceed();
        var yo = result.Value;
        Assert.Equal(pastorId, yo.PersonId);
        Assert.Contains(RoleNames.Pastor, yo.Roles);                    // eje 1: permiso de sistema
        Assert.Contains(yo.Positions, p => p.Name == "Pastor");         // eje 2: cargo eclesiástico
        Assert.Contains(yo.LedMinistries, m => m.Name == "Adoración");  // eje 3: liderazgo
        Assert.Empty(yo.LedSocieties);
        Assert.True(yo.IsOfficialMember);
        Assert.True(yo.IsExecutiveBodyMember);
    }

    [Fact]
    public async Task GetMyCapabilities_refleja_una_revocacion_de_rol_de_inmediato()
    {
        // Los roles se leen de la BASE, no de los claims del token.
        await using var h = await TestHarness.CreateAsync();
        var (_, cuentaId) = await h.SignInAsPastorAsync();

        Assert.Contains(RoleNames.Pastor, (await h.Sender.Send(new GetMyCapabilitiesQuery())).Value.Roles);

        await h.Sender.Send(new RevokeRoleCommand(cuentaId, RoleNames.Pastor));

        // El usuario sigue teniendo el rol en su token de sesión...
        Assert.Contains(RoleNames.Pastor, h.CurrentUser.Roles);
        // ...pero la respuesta ya no se lo reconoce.
        var despues = await h.Sender.Send(new GetMyCapabilitiesQuery());
        despues.ShouldSucceed();
        Assert.Empty(despues.Value.Roles);
    }

    [Fact]
    public async Task GetMyCapabilities_falla_sin_sesion()
    {
        await using var h = await TestHarness.CreateAsync();

        var result = await h.Sender.Send(new GetMyCapabilitiesQuery());

        result.ShouldFailWith("Auth.NotAuthenticated", ErrorType.Unauthorized);
    }
}
