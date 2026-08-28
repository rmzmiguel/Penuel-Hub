using Penuel.Application.Tests.Harness;
using Penuel.Application.UserAccounts.CreateUserAccount;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;

namespace Penuel.Application.Tests;

public sealed class UserAccountsTests
{
    [Fact]
    public async Task CreateUserAccount_guarda_un_hash_bcrypt_y_nunca_la_contrasena()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        var result = await h.Sender.Send(
            new CreateUserAccountCommand(personId, "Ana.Ramirez@Penuel.MX", "contrasena-seguro"));

        result.ShouldSucceed();

        var account = await h.ReloadAsync<UserAccount>(result.Value);
        Assert.NotNull(account);
        Assert.NotEqual("contrasena-seguro", account.PasswordHash);
        Assert.True(h.PasswordHasher.Verify("contrasena-seguro", account.PasswordHash));
        // El email se normaliza para que el índice único sea insensible a mayúsculas.
        Assert.Equal("ana.ramirez@penuel.mx", account.Email);
        Assert.True(account.IsActive);
    }

    [Fact]
    public async Task CreateUserAccount_falla_si_la_persona_ya_tiene_cuenta()
    {
        // Regla 7.1: una Person tiene como máximo un UserAccount.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();
        await h.AddUserAccountAsync(personId, "primera@penuel.mx");

        var result = await h.Sender.Send(
            new CreateUserAccountCommand(personId, "segunda@penuel.mx", "contrasena-seguro"));

        result.ShouldFailWith("UserAccount.AlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateUserAccount_falla_si_el_correo_ya_esta_tomado()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        await h.AddUserAccountAsync(await h.AddPersonAsync(), "repetido@penuel.mx");
        var otraPersona = await h.AddPersonAsync("Otra", "Persona");

        var result = await h.Sender.Send(
            new CreateUserAccountCommand(otraPersona, "REPETIDO@penuel.mx", "contrasena-seguro"));

        result.ShouldFailWith("UserAccount.EmailAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateUserAccount_rechaza_contrasenas_de_mas_de_72_bytes()
    {
        // BCrypt trunca en silencio más allá de 72 bytes: aceptarlas daría una falsa
        // sensación de seguridad (Sección 8.1).
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        var result = await h.Sender.Send(
            new CreateUserAccountCommand(personId, "larga@penuel.mx", new string('a', 73)));

        result.ShouldFailWith("Validation.Failed", ErrorType.Validation);
    }

    [Fact]
    public async Task CreateUserAccount_falla_si_la_persona_no_existe()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(
            new CreateUserAccountCommand(Guid.NewGuid(), "nadie@penuel.mx", "contrasena-seguro"));

        result.ShouldFailWith("Person.NotFound", ErrorType.NotFound);
    }
}
