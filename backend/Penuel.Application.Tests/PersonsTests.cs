using Penuel.Application.Persons.DeactivatePerson;
using Penuel.Application.Persons.ReactivatePerson;
using Penuel.Application.Persons.RegisterPerson;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Tests;

public sealed class PersonsTests
{
    [Fact]
    public async Task RegisterPerson_crea_la_persona_y_audita_a_quien_la_capturo()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand(
            "Ana", "Ramírez", new DateOnly(1980, 5, 12), "8341234567"));

        result.ShouldSucceed();

        var person = await h.ReloadAsync<Person>(result.Value);
        Assert.NotNull(person);
        Assert.Equal("Ana", person.FirstName);
        Assert.Equal(PersonStatus.Active, person.Status);
        // Regla 7.4: la auditoría guarda el PersonId del ejecutor.
        Assert.Equal(pastorId, person.CreatedByPersonId);
    }

    [Fact]
    public async Task RegisterPerson_registrar_no_convierte_en_miembro_ni_da_acceso()
    {
        // Sección 3: los tres ejes son independientes.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null));
        result.ShouldSucceed();

        Assert.False(h.Db.Memberships.Any(m => m.PersonId == result.Value));
        Assert.False(h.Db.UserAccounts.Any(u => u.PersonId == result.Value));
    }

    [Fact]
    public async Task RegisterPerson_rechaza_nombre_vacio()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand("", "", null, null));

        result.ShouldFailWith("Validation.Failed", ErrorType.Validation);
    }

    [Fact]
    public async Task DeactivatePerson_marca_inactiva_sin_borrar_la_fila()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        (await h.Sender.Send(new DeactivatePersonCommand(personId))).ShouldSucceed();

        // Regla 7.3: nunca hay borrado físico.
        var person = await h.ReloadAsync<Person>(personId);
        Assert.NotNull(person);
        Assert.Equal(PersonStatus.Inactive, person.Status);
    }

    [Fact]
    public async Task DeactivatePerson_falla_si_ya_estaba_inactiva()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();
        await h.Sender.Send(new DeactivatePersonCommand(personId));

        var result = await h.Sender.Send(new DeactivatePersonCommand(personId));

        result.ShouldFailWith("Person.AlreadyInactive", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeactivatePerson_falla_si_la_persona_no_existe()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new DeactivatePersonCommand(Guid.NewGuid()));

        result.ShouldFailWith("Person.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task ReactivatePerson_devuelve_a_la_persona_a_estado_activo()
    {
        // Caso real de la Sección 3.1: alguien que se fue y años después regresa.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();
        await h.Sender.Send(new DeactivatePersonCommand(personId));

        (await h.Sender.Send(new ReactivatePersonCommand(personId))).ShouldSucceed();

        var person = await h.ReloadAsync<Person>(personId);
        Assert.Equal(PersonStatus.Active, person!.Status);
    }

    [Fact]
    public async Task ReactivatePerson_falla_si_ya_estaba_activa()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        var result = await h.Sender.Send(new ReactivatePersonCommand(personId));

        result.ShouldFailWith("Person.AlreadyActive", ErrorType.Conflict);
    }
}
