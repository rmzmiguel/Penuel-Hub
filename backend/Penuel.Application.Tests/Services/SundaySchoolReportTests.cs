using Microsoft.EntityFrameworkCore;
using Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

public sealed class SundaySchoolReportTests
{
    private static readonly Guid EscuelaDominical = ServicesSeedData.ServiceTypeIds.EscuelaDominical;
    private static readonly Guid CultoGeneral = ServicesSeedData.ServiceTypeIds.CultoGeneral;
    private static readonly DateOnly Domingo = new(2026, 3, 1);

    [Fact]
    public async Task El_seed_dejo_los_cuatro_tipos_con_sus_banderas_correctas()
    {
        await using var h = await TestHarness.CreateAsync();

        var tipos = await h.Db.ServiceTypes.ToDictionaryAsync(t => t.Name);

        Assert.Equal(4, tipos.Count);

        // Solo Escuela Dominical agrupa por Sociedad, y por tanto es la única donde aplican
        // puntualidad, Biblia y capítulos (regla 7.3).
        Assert.True(tipos["Escuela Dominical"].RequiresSocietyGrouping);
        Assert.False(tipos["Culto General"].RequiresSocietyGrouping);

        // Solo Culto General recoge diezmo (Core, Sección 4.4).
        Assert.True(tipos["Culto General"].CollectsTithe);
        Assert.False(tipos["Escuela Dominical"].CollectsTithe);
        Assert.False(tipos["Culto de Oración"].CollectsTithe);
        Assert.False(tipos["Culto de Jóvenes"].CollectsTithe);
    }

    [Fact]
    public async Task SubmitReport_crea_la_sesion_y_todas_sus_asistencias_en_una_transaccion()
    {
        await using var h = await TestHarness.CreateAsync();
        var (recorderId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        var uno = await h.AddPersonAsync("Asistente", "Uno");
        var dos = await h.AddPersonAsync("Asistente", "Dos");

        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Jovenes, Domingo, 350.50m, recorderId,
            [
                new SundaySchoolAttendanceInput(uno, true, true, true, 5),
                new SundaySchoolAttendanceInput(dos, true, false, false, 0)
            ]));

        result.ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var session = await h.Db.ServiceSessions.SingleAsync(s => s.Id == result.Value);
        Assert.Equal(350.50m, session.TotalOffering);
        Assert.Null(session.TotalTithe);              // Escuela Dominical no recoge diezmo
        Assert.Equal(recorderId, session.TeacherPersonId);
        Assert.Equal(recorderId, session.CreatedByPersonId);

        var asistencias = await h.Db.ServiceAttendances
            .Where(a => a.ServiceSessionId == result.Value).ToListAsync();
        Assert.Equal(2, asistencias.Count);
        Assert.Equal(5, asistencias.Single(a => a.PersonId == uno).ChaptersRead);
    }

    [Fact]
    public async Task Las_cuatro_Sociedades_se_reportan_el_mismo_domingo_y_Damas_y_Varones_comparten_maestro()
    {
        // Definition of Done de la rama. Damas y Varones se imparten JUNTAS desde hace tiempo
        // por falta de maestros: el modelo no representa eso: simplemente el mismo maestro
        // aparece en las dos sesiones de ese domingo (Sección 4).
        await using var h = await TestHarness.CreateAsync();
        var (recorderId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        var maestroCombinado = await h.AddPersonAsync("Maestro", "De Damas y Varones");
        var maestroJovenes = await h.AddPersonAsync("Maestro", "De Jóvenes");
        var maestroInfantil = await h.AddPersonAsync("Ana", "Del Infantil");

        var grupos = new (Guid Society, Guid Teacher)[]
        {
            (CoreSeedData.SocietyIds.Damas, maestroCombinado),
            (CoreSeedData.SocietyIds.Varones, maestroCombinado),   // el MISMO maestro
            (CoreSeedData.SocietyIds.Jovenes, maestroJovenes),
            (CoreSeedData.SocietyIds.Infantil, maestroInfantil)
        };

        foreach (var (society, teacher) in grupos)
        {
            var r = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
                EscuelaDominical, society, Domingo, 100m, teacher, []));
            r.ShouldSucceed();
        }

        h.Db.ChangeTracker.Clear();
        var sesiones = await h.Db.ServiceSessions
            .Where(s => s.SessionDate == Domingo).ToListAsync();

        Assert.Equal(4, sesiones.Count);
        Assert.Equal(2, sesiones.Count(s => s.TeacherPersonId == maestroCombinado));
    }

    [Fact]
    public async Task No_se_permiten_dos_reportes_del_mismo_grupo_el_mismo_domingo()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var primero = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, []));
        primero.ShouldSucceed();

        var segundo = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 120m, null, []));

        segundo.ShouldFailWith("ServiceSession.AlreadyExistsForSociety", ErrorType.Conflict);
    }

    [Fact]
    public async Task El_indice_unico_parcial_bloquea_el_duplicado_aunque_se_esquive_el_handler()
    {
        // La Definition of Done pide provocar el índice, no solo comprobar que existe.
        await using var h = await TestHarness.CreateAsync();
        var (actorId, _) = await h.SignInAsSundaySchoolRecorderAsync();

        h.Db.ServiceSessions.Add(ServiceSession.ForSundaySchool(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, actorId, h.Clock.UtcNow));
        await h.Db.SaveChangesAsync();

        h.Db.ServiceSessions.Add(ServiceSession.ForSundaySchool(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 999m, null, actorId, h.Clock.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => h.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task El_maestro_puede_ser_cualquier_persona_sin_asignacion_previa()
    {
        // Sección 6.2: cubrir a alguien sin asignación formal es normal, no una excepción.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();
        var alguienSinAsignacion = await h.AddPersonAsync("Alguien", "Que Cubrió");

        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Jovenes, Domingo, 50m, alguienSinAsignacion, []));

        result.ShouldSucceed();
        Assert.Empty(h.Db.SundaySchoolTeachingAssignments);
    }

    [Fact]
    public async Task SubmitReport_rechaza_un_tipo_de_servicio_que_no_se_agrupa_por_Sociedad()
    {
        // Regla 7.4.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            CultoGeneral, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null, []));

        result.ShouldFailWith("ServiceType.DoesNotRequireSocietyGrouping", ErrorType.Conflict);
    }

    [Fact]
    public async Task SubmitReport_rechaza_a_la_misma_persona_dos_veces_en_el_reporte()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();
        var persona = await h.AddPersonAsync();

        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, Domingo, 100m, null,
            [
                new SundaySchoolAttendanceInput(persona, true, true, true, 3),
                new SundaySchoolAttendanceInput(persona, false, null, null, null)
            ]));

        result.ShouldFailWith("ServiceAttendance.DuplicatePersonInReport", ErrorType.Validation);
    }

    [Fact]
    public async Task SubmitReport_rechaza_una_fecha_futura()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var futura = DateOnly.FromDateTime(h.Clock.UtcNow.AddDays(7).UtcDateTime);
        var result = await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, futura, 100m, null, []));

        result.ShouldFailWith("Validation.Failed", ErrorType.Validation);
    }
}
