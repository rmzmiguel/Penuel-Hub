using Microsoft.EntityFrameworkCore;
using Penuel.Application.Societies.AssignSocietyLeader;
using Penuel.Application.Societies.CreateSociety;
using Penuel.Application.Societies.RevokeSocietyLeader;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests;

public sealed class SocietiesTests
{
    [Fact]
    public async Task El_seed_dejo_los_seis_ministerios_de_la_iglesia()
    {
        await using var h = await TestHarness.CreateAsync();

        var nombres = await h.Db.Societies.Select(m => m.Name).OrderBy(n => n).ToListAsync();

        Assert.Equal(4, nombres.Count);
        Assert.Contains("Infantil", nombres);
    }

    [Fact]
    public async Task CreateSociety_crea_el_ministerio()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new CreateSocietyCommand("Matrimonios", "Sociedad de parejas."));

        result.ShouldSucceed();
        Assert.Equal(5, await h.Db.Societies.CountAsync());
    }

    [Fact]
    public async Task CreateSociety_falla_si_el_nombre_ya_existe_en_la_iglesia()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new CreateSocietyCommand("damas", null));

        result.ShouldFailWith("Society.NameAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task AssignSocietyLeader_asigna_al_lider()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var ana = await h.AddPersonAsync("Ana", "Ramírez");

        var result = await h.Sender.Send(
            new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, ana));

        result.ShouldSucceed();
        h.Db.ChangeTracker.Clear();
        var liderazgo = await h.Db.SocietyLeaderships.SingleAsync();
        Assert.Equal(ana, liderazgo.PersonId);
        Assert.Null(liderazgo.RevokedAt);
        Assert.Equal(pastorId, liderazgo.AssignedByPersonId);
    }

    [Fact]
    public async Task AssignSocietyLeader_falla_si_ya_hay_un_lider_activo()
    {
        // Reglas 7.11 y 7.14: nunca se reemplaza en silencio.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var ana = await h.AddPersonAsync("Ana", "Ramírez");
        var otro = await h.AddPersonAsync("Otro", "Hermano");
        await h.Sender.Send(new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, ana));

        var result = await h.Sender.Send(
            new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, otro));

        result.ShouldFailWith("Society.AlreadyHasActiveLeader", ErrorType.Conflict);
    }

    [Fact]
    public async Task AssignSocietyLeader_permite_reasignar_tras_revocar_al_anterior()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var ana = await h.AddPersonAsync("Ana", "Ramírez");
        var otro = await h.AddPersonAsync("Otro", "Hermano");
        await h.Sender.Send(new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, ana));

        (await h.Sender.Send(new RevokeSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil)))
            .ShouldSucceed();
        (await h.Sender.Send(new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, otro)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SocietyLeaderships.CountAsync());              // historial intacto
        Assert.Equal(1, await h.Db.SocietyLeaderships.CountAsync(l => l.RevokedAt == null));
    }

    [Fact]
    public async Task Una_misma_persona_puede_liderar_varios_ministerios_a_la_vez()
    {
        // Regla 7.13: la restricción es sobre el recurso, nunca sobre la persona.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var ana = await h.AddPersonAsync("Ana", "Ramírez");

        (await h.Sender.Send(new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Infantil, ana)))
            .ShouldSucceed();
        (await h.Sender.Send(new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Jovenes, ana)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SocietyLeaderships.CountAsync(l => l.PersonId == ana && l.RevokedAt == null));
    }

    [Fact]
    public async Task RevokeSocietyLeader_falla_si_el_ministerio_no_tiene_lider()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(
            new RevokeSocietyLeaderCommand(CoreSeedData.SocietyIds.Varones));

        result.ShouldFailWith("Society.NoActiveLeader", ErrorType.NotFound);
    }

    [Fact]
    public async Task AssignSocietyLeader_falla_si_la_persona_esta_inactiva()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();
        await h.Sender.Send(new Penuel.Application.Persons.DeactivatePerson.DeactivatePersonCommand(personId));

        var result = await h.Sender.Send(
            new AssignSocietyLeaderCommand(CoreSeedData.SocietyIds.Varones, personId));

        result.ShouldFailWith("Person.NotActive", ErrorType.Conflict);
    }
}
