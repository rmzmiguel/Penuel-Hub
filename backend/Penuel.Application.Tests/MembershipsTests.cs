using Penuel.Application.Memberships.CreateMembership;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Tests;

public sealed class MembershipsTests
{
    [Fact]
    public async Task CreateMembership_convierte_a_la_persona_en_miembro_oficial()
    {
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        var result = await h.Sender.Send(
            new CreateMembershipCommand(personId, new DateOnly(2020, 6, 1)));

        result.ShouldSucceed();

        var membership = await h.ReloadAsync<Membership>(result.Value);
        Assert.NotNull(membership);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(new DateOnly(2020, 6, 1), membership.JoinedAt);
        Assert.Equal(pastorId, membership.RegisteredByPersonId);
    }

    [Fact]
    public async Task CreateMembership_falla_si_la_persona_ya_es_miembro()
    {
        // Regla 7.2: una Person tiene como máximo un Membership.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();
        await h.Sender.Send(new CreateMembershipCommand(personId, null));

        var result = await h.Sender.Send(new CreateMembershipCommand(personId, null));

        result.ShouldFailWith("Membership.AlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateMembership_rechaza_una_fecha_de_ingreso_futura()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync();

        var futura = DateOnly.FromDateTime(h.Clock.UtcNow.AddDays(30).UtcDateTime);
        var result = await h.Sender.Send(new CreateMembershipCommand(personId, futura));

        result.ShouldFailWith("Validation.Failed", ErrorType.Validation);
    }
}
