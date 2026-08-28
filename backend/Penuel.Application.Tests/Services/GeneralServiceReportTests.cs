using Microsoft.EntityFrameworkCore;
using Penuel.Application.Services.Sessions.CorrectServiceSessionTotals;
using Penuel.Application.Services.Sessions.SubmitGeneralServiceReport;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

public sealed class GeneralServiceReportTests
{
    private static readonly Guid CultoGeneral = ServicesSeedData.ServiceTypeIds.CultoGeneral;
    private static readonly Guid CultoOracion = ServicesSeedData.ServiceTypeIds.CultoDeOracion;
    private static readonly Guid EscuelaDominical = ServicesSeedData.ServiceTypeIds.EscuelaDominical;
    // El reloj del harness marca el domingo 2026-03-01, así que el miércoles del reporte
    // tiene que ser el ANTERIOR: un reporte con fecha futura se rechaza, y con razón.
    private static readonly DateOnly Miercoles = new(2026, 2, 25);

    [Fact]
    public async Task SubmitGeneralReport_registra_ofrenda_diezmo_y_predicador()
    {
        await using var h = await TestHarness.CreateAsync();
        var (tesoreroId, _) = await h.SignInAsTreasurerAsync();
        var predicador = await h.AddPersonAsync("Fermín", "Ramírez");

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Miercoles, 1250.00m, 4300.00m, predicador));

        result.ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var session = await h.Db.ServiceSessions.SingleAsync(s => s.Id == result.Value);
        Assert.Equal(1250.00m, session.TotalOffering);
        Assert.Equal(4300.00m, session.TotalTithe);
        Assert.Equal(predicador, session.PreacherPersonId);
        Assert.Null(session.SocietyId);          // regla 7.4
        Assert.Null(session.TeacherPersonId);
        Assert.Equal(tesoreroId, session.CreatedByPersonId);
    }

    [Fact]
    public async Task No_se_permiten_dos_Cultos_Generales_el_mismo_dia()
    {
        // Definition of Done. Es el caso que el índice sobre (tipo, fecha, sociedad) NO
        // alcanza, porque Postgres trata cada NULL como distinto de otro NULL.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Miercoles, 1000m, null, null))).ShouldSucceed();

        var segundo = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Miercoles, 2000m, null, null));

        segundo.ShouldFailWith("ServiceSession.AlreadyExistsForDate", ErrorType.Conflict);
    }

    [Fact]
    public async Task El_indice_parcial_de_SocietyId_nulo_bloquea_el_duplicado_en_la_base()
    {
        // Provocado directamente contra la base, esquivando el handler: es lo que la
        // Definition of Done pide comprobar.
        await using var h = await TestHarness.CreateAsync();
        var (actorId, _) = await h.SignInAsTreasurerAsync();

        h.Db.ServiceSessions.Add(ServiceSession.ForGeneralService(
            CultoGeneral, Miercoles, 1000m, null, null, actorId, h.Clock.UtcNow));
        await h.Db.SaveChangesAsync();

        h.Db.ServiceSessions.Add(ServiceSession.ForGeneralService(
            CultoGeneral, Miercoles, 2000m, null, null, actorId, h.Clock.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => h.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Dos_servicios_DISTINTOS_el_mismo_dia_si_se_permiten()
    {
        // El miércoles hay Culto de Oración y Culto General seguidos (Core, Sección 4.4).
        // El índice es por (tipo, fecha): tipos distintos no chocan.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoOracion, Miercoles, 200m, null, null))).ShouldSucceed();

        (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Miercoles, 1000m, 3000m, null))).ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.ServiceSessions.CountAsync(s => s.SessionDate == Miercoles));
    }

    [Fact]
    public async Task No_se_acepta_diezmo_en_un_servicio_que_no_lo_recoge()
    {
        // Regla 7.3.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoOracion, Miercoles, 200m, 500m, null));

        result.ShouldFailWith("ServiceType.DoesNotCollectTithe", ErrorType.Conflict);
    }

    [Fact]
    public async Task SubmitGeneralReport_rechaza_un_tipo_agrupado_por_Sociedad()
    {
        // Regla 7.4, en el sentido contrario.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();

        var result = await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            EscuelaDominical, Miercoles, 200m, null, null));

        result.ShouldFailWith("ServiceType.RequiresSocietyGrouping", ErrorType.Conflict);
    }

    [Fact]
    public async Task CorrectTotals_corrige_y_registra_quien_corrigio()
    {
        await using var h = await TestHarness.CreateAsync();
        var (tesoreroId, _) = await h.SignInAsTreasurerAsync();

        var sessionId = (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Miercoles, 1000m, 3000m, null))).Value;

        (await h.Sender.Send(new CorrectServiceSessionTotalsCommand(sessionId, 1150m, 3200m)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var session = await h.Db.ServiceSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(1150m, session.TotalOffering);
        Assert.Equal(3200m, session.TotalTithe);
        Assert.Equal(tesoreroId, session.UpdatedByPersonId);   // regla 7.4 del Core
    }
}
