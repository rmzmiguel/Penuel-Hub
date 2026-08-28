using Microsoft.EntityFrameworkCore;
using Penuel.Application.Auth.Login;
using Penuel.Application.Roles.AssignRole;
using Penuel.Application.Roles.RevokeRole;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;

namespace Penuel.Application.Tests;

public sealed class RolesTests
{
    [Fact]
    public async Task AssignRole_otorga_el_rol_y_registra_quien_lo_otorgo()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var cuenta = await h.AddUserAccountAsync(await h.AddPersonAsync(), "nuevo@penuel.mx");

        var result = await h.Sender.Send(new AssignRoleCommand(cuenta, RoleNames.Pastor));

        result.ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var userRole = await h.Db.UserRoles.SingleAsync(ur => ur.Id == result.Value);
        Assert.Null(userRole.RevokedAt);
        Assert.Equal(pastorId, userRole.AssignedByPersonId);
    }

    [Fact]
    public async Task AssignRole_falla_si_la_cuenta_ya_tiene_ese_rol_activo()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var cuenta = await h.AddUserAccountAsync(await h.AddPersonAsync(), "nuevo@penuel.mx");
        await h.Sender.Send(new AssignRoleCommand(cuenta, RoleNames.Pastor));

        var result = await h.Sender.Send(new AssignRoleCommand(cuenta, RoleNames.Pastor));

        result.ShouldFailWith("Role.AlreadyAssigned", ErrorType.Conflict);
    }

    [Fact]
    public async Task AssignRole_falla_si_el_rol_no_existe()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var cuenta = await h.AddUserAccountAsync(await h.AddPersonAsync(), "nuevo@penuel.mx");

        var result = await h.Sender.Send(new AssignRoleCommand(cuenta, "RolInexistente"));

        result.ShouldFailWith("Role.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task AssignRole_permite_reasignar_el_mismo_rol_despues_de_revocarlo()
    {
        // Esta es LA prueba de regresión de la corrección 6.7: con la PK compuesta original
        // esto era imposible, porque la segunda fila chocaba con la primera.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var cuenta = await h.AddUserAccountAsync(await h.AddPersonAsync(), "nuevo@penuel.mx");

        (await h.Sender.Send(new AssignRoleCommand(cuenta, RoleNames.Pastor))).ShouldSucceed();
        (await h.Sender.Send(new RevokeRoleCommand(cuenta, RoleNames.Pastor))).ShouldSucceed();
        (await h.Sender.Send(new AssignRoleCommand(cuenta, RoleNames.Pastor))).ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var filas = await h.Db.UserRoles.Where(ur => ur.UserAccountId == cuenta).ToListAsync();
        Assert.Equal(2, filas.Count);                                   // el historial se conserva
        Assert.Single(filas.Where(ur => ur.RevokedAt == null));          // solo una activa
    }

    [Fact]
    public async Task RevokeRole_retira_el_rol_y_cierra_todas_las_sesiones_vivas()
    {
        // Sección 8.1: es la mitad del candado que impide renovar y obtener un token nuevo.
        await using var h = await TestHarness.CreateAsync();
        var (_, cuentaPastor) = await h.SignInAsPastorAsync(password: "contrasena-de-prueba");

        var sesion = await h.Sender.Send(new LoginQuery("pastor@penuel.mx", "contrasena-de-prueba"));
        sesion.ShouldSucceed();
        Assert.Equal(1, await h.Db.RefreshTokens.CountAsync(t => t.RevokedAt == null));

        var result = await h.Sender.Send(new RevokeRoleCommand(cuentaPastor, RoleNames.Pastor));

        result.ShouldSucceed();
        h.Db.ChangeTracker.Clear();
        Assert.Equal(0, await h.Db.RefreshTokens.CountAsync(t => t.RevokedAt == null));
        Assert.Equal(0, await h.Db.UserRoles.CountAsync(ur => ur.RevokedAt == null));
    }

    [Fact]
    public async Task RevokeRole_falla_si_la_cuenta_no_tiene_ese_rol()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var cuenta = await h.AddUserAccountAsync(await h.AddPersonAsync(), "sinrol@penuel.mx");

        var result = await h.Sender.Send(new RevokeRoleCommand(cuenta, RoleNames.Pastor));

        result.ShouldFailWith("Role.NotAssigned", ErrorType.NotFound);
    }
}
