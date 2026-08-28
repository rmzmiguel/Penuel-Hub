using Microsoft.EntityFrameworkCore;
using Penuel.Application.Persons.GetPersons;
using Penuel.Application.Services.ServiceTypes.GetServiceTypes;
using Penuel.Application.Societies.AddSocietyMember;
using Penuel.Application.Societies.GetSocietyMembers;
using Penuel.Application.Societies.RemoveSocietyMember;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests;

public sealed class SocietyMembershipTests
{
    [Fact]
    public async Task Agregar_integrantes_precarga_el_grupo_para_el_reporte_dominical()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var ana = await h.AddPersonAsync("Ana", "Gómez");
        var luis = await h.AddPersonAsync("Luis", "Martínez");

        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Jovenes, ana)))
            .ShouldSucceed();
        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Jovenes, luis)))
            .ShouldSucceed();

        var grupo = await h.Sender.Send(new GetSocietyMembersQuery(CoreSeedData.SocietyIds.Jovenes));

        grupo.ShouldSucceed();
        Assert.Equal("Jóvenes", grupo.Value.SocietyName);
        Assert.Equal(2, grupo.Value.Members.Count);
        Assert.Equal("Gómez", grupo.Value.Members.First().LastName);   // ordenado por apellido
    }

    [Fact]
    public async Task Pertenecer_a_una_Sociedad_no_hace_a_nadie_miembro_oficial_ni_le_da_acceso()
    {
        // Sección 3.2 del Core: es el caso normal de quien está siendo alcanzado.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var visitante = await h.AddPersonAsync("Visitante", "Nuevo");

        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Jovenes, visitante)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.False(await h.Db.Memberships.AnyAsync(m => m.PersonId == visitante));
        Assert.False(await h.Db.UserAccounts.AnyAsync(u => u.PersonId == visitante));
    }

    [Fact]
    public async Task Una_persona_puede_pertenecer_a_varias_Sociedades()
    {
        // Regla 7.13 del Core: la restricción es sobre el duplicado exacto, no sobre la persona.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();

        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Jovenes, persona)))
            .ShouldSucceed();
        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Damas, persona)))
            .ShouldSucceed();
    }

    [Fact]
    public async Task El_duplicado_exacto_si_se_rechaza()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();
        await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Damas, persona));

        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Damas, persona)))
            .ShouldFailWith("Society.MemberAlreadyAdded", ErrorType.Conflict);
    }

    [Fact]
    public async Task Dar_de_baja_conserva_el_historial_y_permite_volver_a_agregar()
    {
        // Alguien que pasa de Jóvenes a Damas, o que regresa después (regla 7.3).
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();
        var id = (await h.Sender.Send(
            new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Damas, persona))).Value;

        (await h.Sender.Send(new RemoveSocietyMemberCommand(id))).ShouldSucceed();

        var grupo = await h.Sender.Send(new GetSocietyMembersQuery(CoreSeedData.SocietyIds.Damas));
        Assert.Empty(grupo.Value.Members);

        (await h.Sender.Send(new AddSocietyMemberCommand(CoreSeedData.SocietyIds.Damas, persona)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SocietyMemberships.CountAsync());   // historial intacto
        Assert.Equal(1, await h.Db.SocietyMemberships.CountAsync(m => m.RevokedAt == null));
    }

    [Fact]
    public async Task El_directorio_devuelve_solo_nombres_y_filtra_por_busqueda()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        await h.AddPersonAsync("Ana", "Gómez");
        await h.AddPersonAsync("Luis", "Martínez");

        var todos = await h.Sender.Send(new GetPersonsQuery(null));
        todos.ShouldSucceed();
        Assert.Contains(todos.Value, p => p.FirstName == "Ana");

        var filtrado = await h.Sender.Send(new GetPersonsQuery("martí"));
        filtrado.ShouldSucceed();
        var uno = Assert.Single(filtrado.Value);
        Assert.Equal("Martínez", uno.LastName);
    }

    [Fact]
    public async Task El_directorio_lo_puede_leer_quien_captura_reportes_no_solo_el_Pastor()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        await h.AddPersonAsync("Ana", "Gómez");

        await h.SignInAsSundaySchoolRecorderAsync();
        (await h.Sender.Send(new GetPersonsQuery(null))).ShouldSucceed();

        await h.SignInAsTreasurerAsync();
        (await h.Sender.Send(new GetPersonsQuery(null))).ShouldSucceed();
    }

    [Fact]
    public async Task Los_tipos_de_servicio_llegan_con_sus_tres_banderas()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var result = await h.Sender.Send(new GetServiceTypesQuery());

        result.ShouldSucceed();
        Assert.Equal(4, result.Value.Count);
        var ed = result.Value.Single(t => t.Name == "Escuela Dominical");
        Assert.True(ed.RequiresSocietyGrouping);
        Assert.False(ed.CollectsTithe);
        Assert.True(result.Value.Single(t => t.Name == "Culto General").CollectsTithe);
    }
}
