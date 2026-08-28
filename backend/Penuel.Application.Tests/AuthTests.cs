using Microsoft.EntityFrameworkCore;
using Penuel.Application.Auth.Login;
using Penuel.Application.Auth.Refresh;
using Penuel.Application.Persons.DeactivatePerson;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;

namespace Penuel.Application.Tests;

public sealed class AuthTests
{
    private const string Correo = "pastor@penuel.mx";
    private const string Clave = "contrasena-de-prueba";

    [Fact]
    public async Task Login_devuelve_una_sesion_con_los_roles_vigentes()
    {
        await using var h = await TestHarness.CreateAsync();
        var (personId, cuentaId) = await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();

        var result = await h.Sender.Send(new LoginQuery(Correo, Clave));

        result.ShouldSucceed();
        Assert.Equal(personId, result.Value.PersonId);
        Assert.Equal(cuentaId, result.Value.UserAccountId);
        Assert.Contains(RoleNames.Pastor, result.Value.Roles);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
        Assert.True(result.Value.AccessTokenExpiresAt > h.Clock.UtcNow);
    }

    [Fact]
    public async Task Login_falla_con_la_contrasena_incorrecta()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();

        var result = await h.Sender.Send(new LoginQuery(Correo, "equivocada"));

        result.ShouldFailWith("Auth.InvalidCredentials", ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Login_no_permite_distinguir_un_correo_inexistente_de_una_clave_mala()
    {
        // Devolver errores distintos permitiría enumerar qué correos tienen cuenta.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();

        var claveMala = await h.Sender.Send(new LoginQuery(Correo, "equivocada"));
        var correoInexistente = await h.Sender.Send(new LoginQuery("nadie@penuel.mx", "equivocada"));

        Assert.Equal(claveMala.Error!.Code, correoInexistente.Error!.Code);
        Assert.Equal(claveMala.Error.Message, correoInexistente.Error.Message);
    }

    [Fact]
    public async Task Login_bloquea_la_cuenta_tras_cinco_intentos_fallidos()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();

        for (var i = 0; i < 5; i++)
        {
            await h.Sender.Send(new LoginQuery(Correo, "equivocada"));
        }

        // Incluso con la contraseña CORRECTA, la cuenta está bloqueada.
        var result = await h.Sender.Send(new LoginQuery(Correo, Clave));
        result.ShouldFailWith("Auth.AccountLocked", ErrorType.Unauthorized);

        // Y se desbloquea sola al pasar el tiempo configurado.
        h.Clock.Advance(TimeSpan.FromMinutes(16));
        (await h.Sender.Send(new LoginQuery(Correo, Clave))).ShouldSucceed();
    }

    [Fact]
    public async Task Login_rechaza_a_una_persona_inactiva_aunque_su_cuenta_siga_activa()
    {
        // Regla 7.15.
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsPastorAsync(Correo, Clave);
        await h.Sender.Send(new DeactivatePersonCommand(personId));
        h.CurrentUser.SignOut();

        var result = await h.Sender.Send(new LoginQuery(Correo, Clave));

        result.ShouldFailWith("Auth.PersonInactive", ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_rota_el_token_y_revoca_el_anterior()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();
        var sesion = (await h.Sender.Send(new LoginQuery(Correo, Clave))).Value;

        var renovada = await h.Sender.Send(new RefreshTokenCommand(sesion.RefreshToken));

        renovada.ShouldSucceed();
        Assert.NotEqual(sesion.RefreshToken, renovada.Value.RefreshToken);
        h.Db.ChangeTracker.Clear();
        Assert.Equal(1, await h.Db.RefreshTokens.CountAsync(t => t.RevokedAt == null));
        Assert.Equal(2, await h.Db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RefreshToken_falla_con_un_token_desconocido()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);

        var result = await h.Sender.Send(new RefreshTokenCommand("token-que-nunca-existio"));

        result.ShouldFailWith("Auth.InvalidRefreshToken", ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_reusado_cierra_TODAS_las_sesiones_de_la_cuenta()
    {
        // Sección 8.1: presentar un token ya revocado es señal de robo. No basta con rechazar
        // esa petición — se fuerza un inicio de sesión completo.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync(Correo, Clave);
        h.CurrentUser.SignOut();

        var primera = (await h.Sender.Send(new LoginQuery(Correo, Clave))).Value;
        var segunda = (await h.Sender.Send(new RefreshTokenCommand(primera.RefreshToken))).Value;

        // El atacante reusa el token viejo, que la rotación ya revocó.
        var reuso = await h.Sender.Send(new RefreshTokenCommand(primera.RefreshToken));
        reuso.ShouldFailWith("Auth.RefreshTokenReuseDetected", ErrorType.Unauthorized);

        // El token legítimo del usuario también quedó inutilizado.
        h.Db.ChangeTracker.Clear();
        Assert.Equal(0, await h.Db.RefreshTokens.CountAsync(t => t.RevokedAt == null));
        (await h.Sender.Send(new RefreshTokenCommand(segunda.RefreshToken)))
            .ShouldFailWith("Auth.RefreshTokenReuseDetected", ErrorType.Unauthorized);
    }
}
