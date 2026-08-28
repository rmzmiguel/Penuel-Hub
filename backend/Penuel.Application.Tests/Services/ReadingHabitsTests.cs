using Penuel.Application.Services.SundaySchool.GetReadingHabitsReport;
using Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;
using Penuel.Application.Tests.Harness;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

/// <summary>
/// El resultado concreto de capturar el detalle granular de la hoja física: las métricas que
/// la iglesia ya calcula a mano (Core, Sección 4.6).
/// </summary>
public sealed class ReadingHabitsTests
{
    private static readonly Guid EscuelaDominical = ServicesSeedData.ServiceTypeIds.EscuelaDominical;

    [Fact]
    public async Task El_reporte_calcula_porcentaje_de_Biblia_y_promedio_de_capitulos()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var a = await h.AddPersonAsync("Persona", "A");
        var b = await h.AddPersonAsync("Persona", "B");
        var c = await h.AddPersonAsync("Persona", "C");
        var d = await h.AddPersonAsync("Persona", "D");

        // Domingo 1: 4 presentes, 3 con Biblia, 12 capítulos en total.
        await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Jovenes, new DateOnly(2026, 2, 22), 100m, null,
            [
                new SundaySchoolAttendanceInput(a, true, true, true, 5),
                new SundaySchoolAttendanceInput(b, true, true, true, 4),
                new SundaySchoolAttendanceInput(c, true, false, true, 3),
                new SundaySchoolAttendanceInput(d, true, true, false, 0)
            ]));

        // Domingo 2: 2 presentes, 1 con Biblia, 6 capítulos.
        await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Jovenes, new DateOnly(2026, 3, 1), 100m, null,
            [
                new SundaySchoolAttendanceInput(a, true, true, true, 6),
                new SundaySchoolAttendanceInput(b, true, true, false, 0)
            ]));

        var result = await h.Sender.Send(new GetReadingHabitsReportQuery(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31), CoreSeedData.SocietyIds.Jovenes));

        result.ShouldSucceed();
        var r = result.Value;

        Assert.Equal(2, r.SessionCount);
        Assert.Equal(6, r.TotalPresent);          // 4 + 2
        Assert.Equal(4, r.TotalBroughtBible);     // 3 + 1
        Assert.Equal(66.7m, r.BiblePercentage);   // 4/6
        Assert.Equal(3.00m, r.AverageChaptersPerPerson);  // 18 capítulos / 6 presentes

        // La serie va de la más antigua a la más reciente: así se lee la tendencia.
        var semanas = r.Sessions.ToList();
        Assert.Equal(new DateOnly(2026, 2, 22), semanas[0].SessionDate);
        Assert.Equal(75.0m, semanas[0].BiblePercentage);   // 3/4
        Assert.Equal(50.0m, semanas[1].BiblePercentage);   // 1/2 -> la tendencia va a la baja
        Assert.Equal(3.00m, semanas[0].AverageChaptersPerPerson);  // 12/4
    }

    [Fact]
    public async Task Solo_cuenta_a_los_presentes_no_a_los_ausentes()
    {
        // Incluir ausentes hundiría los promedios y haría el número inservible.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();
        var presente = await h.AddPersonAsync("Vino", "Sí");
        var ausente = await h.AddPersonAsync("No", "Vino");

        await h.Sender.Send(new SubmitSundaySchoolReportCommand(
            EscuelaDominical, CoreSeedData.SocietyIds.Damas, new DateOnly(2026, 3, 1), 100m, null,
            [
                new SundaySchoolAttendanceInput(presente, true, true, true, 4),
                new SundaySchoolAttendanceInput(ausente, false, null, null, null)
            ]));

        var r = (await h.Sender.Send(new GetReadingHabitsReportQuery(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31), null))).Value;

        Assert.Equal(1, r.TotalPresent);
        Assert.Equal(100.0m, r.BiblePercentage);
        Assert.Equal(4.00m, r.AverageChaptersPerPerson);
    }

    [Fact]
    public async Task Un_periodo_sin_sesiones_devuelve_ceros_y_no_revienta()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var r = (await h.Sender.Send(new GetReadingHabitsReportQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null))).Value;

        Assert.Equal(0, r.SessionCount);
        Assert.Equal(0m, r.BiblePercentage);          // sin división entre cero
        Assert.Equal(0m, r.AverageChaptersPerPerson);
        Assert.Empty(r.Sessions);
    }

    [Fact]
    public async Task El_reporte_ignora_los_cultos_generales()
    {
        // Solo tiene sentido donde se captura el detalle granular (regla 7.3).
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsTreasurerAsync();
        await h.Sender.Send(new Penuel.Application.Services.Sessions.SubmitGeneralServiceReport
            .SubmitGeneralServiceReportCommand(
                ServicesSeedData.ServiceTypeIds.CultoGeneral, new DateOnly(2026, 3, 1), 500m, 2000m, null));

        await h.SignInAsSundaySchoolRecorderAsync();
        var r = (await h.Sender.Send(new GetReadingHabitsReportQuery(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31), null))).Value;

        Assert.Equal(0, r.SessionCount);
    }
}
