using Microsoft.EntityFrameworkCore;
using Penuel.Application.Positions.AssignPosition;
using Penuel.Application.Positions.CreatePosition;
using Penuel.Application.Positions.RevokePosition;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests;

public sealed class PositionsTests
{
    [Fact]
    public async Task El_seed_dejo_los_cuatro_cargos_todos_del_cuerpo_ejecutivo()
    {
        await using var h = await TestHarness.CreateAsync();

        var cargos = await h.Db.Positions.ToListAsync();

        Assert.Equal(4, cargos.Count);
        Assert.All(cargos, c => Assert.True(c.IsExecutiveBody));
    }

    [Fact]
    public async Task CreatePosition_crea_el_cargo()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(
            new CreatePositionCommand("Ujier", "Apoya en el orden del culto.", false));

        result.ShouldSucceed();
        h.Db.ChangeTracker.Clear();
        var creado = await h.Db.Positions.SingleAsync(p => p.Id == result.Value);
        Assert.False(creado.IsExecutiveBody);
    }

    [Fact]
    public async Task CreatePosition_falla_si_el_nombre_ya_existe()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var result = await h.Sender.Send(new CreatePositionCommand("Diácono", null, true));

        result.ShouldFailWith("Position.NameAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task AssignPosition_admite_varios_titulares_activos_del_mismo_cargo()
    {
        // Sección 6.13: "hay variedad en cuanto al número" de diáconos. A diferencia de los
        // liderazgos, aquí NO se bloquea por estar el cargo ocupado.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var uno = await h.AddPersonAsync("Diácono", "Uno");
        var dos = await h.AddPersonAsync("Diácono", "Dos");
        var tres = await h.AddPersonAsync("Diácono", "Tres");

        foreach (var persona in new[] { uno, dos, tres })
        {
            (await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, persona)))
                .ShouldSucceed();
        }

        h.Db.ChangeTracker.Clear();
        Assert.Equal(3, await h.Db.PersonPositions.CountAsync(
            pp => pp.PositionId == CoreSeedData.PositionIds.Diacono && pp.RevokedAt == null));
    }

    [Fact]
    public async Task AssignPosition_falla_si_la_misma_persona_ya_ostenta_ese_cargo()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, persona));

        var result = await h.Sender.Send(
            new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, persona));

        result.ShouldFailWith("Position.AlreadyHeldByPerson", ErrorType.Conflict);
    }

    [Fact]
    public async Task Una_misma_persona_puede_acumular_varios_cargos()
    {
        // Regla 7.13, caso real: alguien que es Diácono y a la vez Tesorero General.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();

        (await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, persona)))
            .ShouldSucceed();
        (await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.TesoreroGeneral, persona)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.PersonPositions.CountAsync(
            pp => pp.PersonId == persona && pp.RevokedAt == null));
    }

    [Fact]
    public async Task RevokePosition_retira_solo_a_esa_persona_y_deja_a_los_demas()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var uno = await h.AddPersonAsync("Diácono", "Uno");
        var dos = await h.AddPersonAsync("Diácono", "Dos");
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, uno));
        await h.Sender.Send(new AssignPositionCommand(CoreSeedData.PositionIds.Diacono, dos));

        (await h.Sender.Send(new RevokePositionCommand(CoreSeedData.PositionIds.Diacono, uno)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(1, await h.Db.PersonPositions.CountAsync(
            pp => pp.PositionId == CoreSeedData.PositionIds.Diacono && pp.RevokedAt == null));
        Assert.True(await h.Db.PersonPositions.AnyAsync(
            pp => pp.PersonId == dos && pp.RevokedAt == null));
    }

    [Fact]
    public async Task RevokePosition_falla_si_la_persona_no_ostenta_el_cargo()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var persona = await h.AddPersonAsync();

        var result = await h.Sender.Send(
            new RevokePositionCommand(CoreSeedData.PositionIds.Diacono, persona));

        result.ShouldFailWith("Position.NotHeldByPerson", ErrorType.NotFound);
    }
}
