using Microsoft.EntityFrameworkCore;
using Penuel.Application.Services.Sessions.GetServiceSessionHistory;
using Penuel.Application.Services.Sessions.SubmitGeneralServiceReport;
using Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;
using Penuel.Application.Services.Tithes.GetSessionTitheEntries;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

public sealed class ServicesAuthorizationTests
{
    private static readonly Guid EscuelaDominical = ServicesSeedData.ServiceTypeIds.EscuelaDominical;
    private static readonly Guid CultoGeneral = ServicesSeedData.ServiceTypeIds.CultoGeneral;
    private static readonly DateOnly Domingo = new(2026, 3, 1);

    [Fact]
    public async Task El_Tesorero_entra_por_su_CARGO_sin_tener_ningun_rol_de_sistema()
    {
        // Sección 8.3: es el único punto donde un Position concede acceso, y el cargo NO
        // viaja en el token — se resuelve contra la base.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        Assert.Empty(h.CurrentUser.Roles);   // literalmente ningún rol

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 1000m, 3000m, null));

        result.ShouldSucceed();
    }

    [Fact]
    public async Task Revocar_el_cargo_de_Tesorero_le_corta_el_acceso_de_inmediato()
    {
        // Consecuencia directa de resolver el cargo contra la base y no contra el token.
        await using var h = await TestHarness.CreateAsync();
        var (tesoreroId, _) = await h.SignInAsTreasurerAsync();

        (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, new DateOnly(2026, 2, 25), 100m, null, null))).ShouldSucceed();

        var cargo = await h.Db.PersonPositions.SingleAsync(pp => pp.PersonId == tesoreroId);
        cargo.Revoke(tesoreroId, h.Clock.UtcNow);
        await h.Db.SaveChangesAsync();

        var despues = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 200m, null, null));

        despues.ShouldFailWith("Auth.InsufficientPermissions", ErrorType.Forbidden);
    }

    [Fact]
    public async Task El_encargado_de_Escuela_Dominical_no_puede_reportar_un_Culto_General()
    {
        // Ahí es donde vive el dinero que administra la Tesorería.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 1000m, 3000m, null));

        result.ShouldFailWith("Auth.InsufficientPermissions", ErrorType.Forbidden);
    }

    [Fact]
    public async Task El_Tesorero_no_puede_levantar_un_reporte_de_Escuela_Dominical()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, []));

        result.ShouldFailWith("Auth.InsufficientPermissions", ErrorType.Forbidden);
    }

    [Fact]
    public async Task El_Pastor_puede_con_ambos_mundos()
    {
        // Core, Sección 1: control absoluto. No debe tener que otorgarse roles extra.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        (await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, []))).ShouldSucceed();

        (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 1000m, 3000m, null))).ShouldSucceed();
    }

    [Fact]
    public async Task El_historial_le_esconde_los_cultos_generales_a_quien_solo_captura_Escuela_Dominical()
    {
        // Sección 8.4, el patrón "puerta amplia + filtro en el handler".
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, []));
        await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 5000m, 20000m, null));

        // El Pastor ve las dos.
        var comoPastor = await h.Sender.Send(new GetServiceSessionHistoryQuery(null, null, null, null));
        comoPastor.ShouldSucceed();
        Assert.Equal(2, comoPastor.Value.Count);

        // Quien solo captura Escuela Dominical ve una sola.
        await h.SignInAsSundaySchoolRecorderAsync();
        var comoEncargado = await h.Sender.Send(new GetServiceSessionHistoryQuery(null, null, null, null));
        comoEncargado.ShouldSucceed();
        var unica = Assert.Single(comoEncargado.Value);
        Assert.Equal("Escuela Dominical", unica.ServiceTypeName);
        Assert.Equal(100m, unica.TotalOffering);
    }

    [Fact]
    public async Task El_desglose_de_diezmo_le_esta_vedado_a_quien_solo_captura_Escuela_Dominical()
    {
        // Sección 8.3: es información más sensible que el total.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var sessionId = (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 1000m, 3000m, null))).Value;

        await h.SignInAsSundaySchoolRecorderAsync();
        var result = await h.Sender.Send(new GetSessionTitheEntriesQuery(sessionId));

        result.ShouldFailWith("Auth.InsufficientPermissions", ErrorType.Forbidden);
    }

    [Fact]
    public async Task Sin_sesion_todo_devuelve_401_y_no_403()
    {
        await using var h = await TestHarness.CreateAsync();

        (await h.Sender.Send(new GetServiceSessionHistoryQuery(null, null, null, null)))
            .ShouldFailWith("Auth.NotAuthenticated", ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Quien_no_tiene_ni_rol_ni_cargo_recibe_403_con_un_mensaje_util()
    {
        await using var h = await TestHarness.CreateAsync();
        var personId = await h.AddPersonAsync();
        var accountId = await h.AddUserAccountAsync(personId, "cualquiera@penuel.mx");
        h.CurrentUser.SignInAs(personId, accountId);

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 100m, null, null));

        result.ShouldFailWith("Auth.InsufficientPermissions", ErrorType.Forbidden);
        // El mensaje dice QUÉ haría falta: quien lo lee suele ser quien tiene que pedirlo.
        Assert.Contains("Pastor", result.Error!.Message);
        Assert.Contains("Tesorero General", result.Error.Message);
    }
}
