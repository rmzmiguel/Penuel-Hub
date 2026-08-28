using Microsoft.EntityFrameworkCore;
using Penuel.Application.Services.Sessions.SubmitGeneralServiceReport;
using Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;
using Penuel.Application.Services.Tithes.CorrectTitheEntry;
using Penuel.Application.Services.Tithes.GetSessionTitheEntries;
using Penuel.Application.Services.Tithes.RecordTitheEntry;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

public sealed class TitheEntryTests
{
    private static readonly Guid CultoGeneral = ServicesSeedData.ServiceTypeIds.CultoGeneral;
    private static readonly DateOnly Domingo = new(2026, 3, 1);

    private static async Task<Guid> SesionConDiezmoAsync(TestHarness h, decimal totalTithe)
        => (await h.Sender.Send(new SubmitGeneralServiceReportCommand(
            CultoGeneral, Domingo, 1000m, totalTithe, null))).Value;

    [Fact]
    public async Task Un_diezmo_identificado_que_NO_cuadra_con_el_total_se_acepta_sin_reclamo()
    {
        // Definition of Done y regla 7.5: son datos independientes por diseño. No todos anotan
        // sus datos en el sobre, y que no coincidan NO es un error de captura.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        var sessionId = await SesionConDiezmoAsync(h, totalTithe: 5000m);
        var persona = await h.AddPersonAsync("Hermana", "Que Anotó");

        // Solo 800 de los 5000 quedan identificados.
        (await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 800m))).ShouldSucceed();

        var detalle = await h.Sender.Send(new GetSessionTitheEntriesQuery(sessionId));
        detalle.ShouldSucceed();

        Assert.Equal(5000m, detalle.Value.TotalTithe);
        Assert.Equal(800m, detalle.Value.IdentifiedTotal);
        Assert.Equal(4200m, detalle.Value.UnidentifiedAmount);   // informativo, no un error
        Assert.Single(detalle.Value.Entries);
    }

    [Fact]
    public async Task La_suma_identificada_puede_incluso_SUPERAR_el_total_sin_ser_rechazada()
    {
        // Cada quien da conforme a su criterio y el total pudo contarse mal; el sistema
        // no opina (regla 7.5).
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        var sessionId = await SesionConDiezmoAsync(h, totalTithe: 100m);
        var persona = await h.AddPersonAsync();

        (await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 900m))).ShouldSucceed();

        var detalle = (await h.Sender.Send(new GetSessionTitheEntriesQuery(sessionId))).Value;
        Assert.Equal(-800m, detalle.UnidentifiedAmount);
    }

    [Fact]
    public async Task No_se_registra_diezmo_en_un_servicio_que_no_lo_recoge()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        var persona = await h.AddPersonAsync();

        // Se crea la sesión de Escuela Dominical directamente, con un actor válido.
        var (actorId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        var escuela = ServiceSession.ForSundaySchool(
            ServicesSeedData.ServiceTypeIds.EscuelaDominical, CoreSeedData.SocietyIds.Damas,
            Domingo, 100m, null, actorId, h.Clock.UtcNow);
        h.Db.ServiceSessions.Add(escuela);
        await h.Db.SaveChangesAsync();

        await h.SignInAsTreasurerAsync();
        var result = await h.Sender.Send(new RecordTitheEntryCommand(escuela.Id, persona, 100m));

        result.ShouldFailWith("ServiceType.DoesNotCollectTithe", ErrorType.Conflict);
    }

    [Fact]
    public async Task La_misma_persona_no_puede_tener_dos_diezmos_en_la_misma_sesion()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        var sessionId = await SesionConDiezmoAsync(h, 1000m);
        var persona = await h.AddPersonAsync();

        (await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 300m))).ShouldSucceed();
        var segundo = await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 200m));

        segundo.ShouldFailWith("TitheEntry.AlreadyRecorded", ErrorType.Conflict);
    }

    [Fact]
    public async Task Corregir_un_diezmo_es_un_UPDATE_que_registra_quien_lo_corrigio()
    {
        await using var h = await TestHarness.CreateAsync();
        var (tesoreroId, _) = await h.SignInAsTreasurerAsync();
        var sessionId = await SesionConDiezmoAsync(h, 1000m);
        var persona = await h.AddPersonAsync();
        var entryId = (await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 300m))).Value;

        (await h.Sender.Send(new CorrectTitheEntryCommand(entryId, 350m))).ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        // Una sola fila: se corrige, no se agrega otra (Sección 6.4).
        var entradas = await h.Db.TitheEntries.Where(t => t.ServiceSessionId == sessionId).ToListAsync();
        var entrada = Assert.Single(entradas);
        Assert.Equal(350m, entrada.Amount);
        Assert.Equal(tesoreroId, entrada.UpdatedByPersonId);
    }

    [Fact]
    public async Task Un_diezmo_de_cero_o_negativo_se_rechaza()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        var sessionId = await SesionConDiezmoAsync(h, 1000m);
        var persona = await h.AddPersonAsync();

        (await h.Sender.Send(new RecordTitheEntryCommand(sessionId, persona, 0m)))
            .ShouldFailWith("Validation.Failed", ErrorType.Validation);
    }
}
