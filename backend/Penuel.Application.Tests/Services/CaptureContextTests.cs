using Penuel.Application.Services.SundaySchool.GetMySundaySchoolCaptureContext;
using Penuel.Application.Tests.Harness;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

/// <summary>
/// Los tres escenarios de la Sección 8.2, que son lo que decide qué le pregunta el frontend
/// a quien va a capturar.
/// </summary>
public sealed class CaptureContextTests
{
    [Fact]
    public async Task Escenario_1_un_solo_grupo_fijo_no_hay_nada_que_preguntar()
    {
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Jovenes, personId);

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());

        result.ShouldSucceed();
        Assert.Equal(SundaySchoolCaptureMode.SingleFixedGroup, result.Value.Mode);
        var mio = Assert.Single(result.Value.MySocieties);
        Assert.Equal("Jóvenes", mio.SocietyName);
        Assert.False(result.Value.IsFloatingSubstitute);
    }

    [Fact]
    public async Task Escenario_2_varios_grupos_fijos_hay_que_preguntar_cual()
    {
        // El caso de las clases combinadas: Damas y Varones a cargo de la misma persona.
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Damas, personId);
        await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Varones, personId);

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());

        result.ShouldSucceed();
        Assert.Equal(SundaySchoolCaptureMode.MultipleFixedGroups, result.Value.Mode);
        Assert.Equal(2, result.Value.MySocieties.Count);
    }

    [Fact]
    public async Task Escenario_3a_sin_ninguna_asignacion_se_ofrecen_las_cuatro_Sociedades()
    {
        // Alguien de confianza que solo digitaliza reportes ajenos.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());

        result.ShouldSucceed();
        Assert.Equal(SundaySchoolCaptureMode.NoFixedGroup, result.Value.Mode);
        Assert.Empty(result.Value.MySocieties);
        Assert.Equal(4, result.Value.AllSocieties.Count);
        Assert.False(result.Value.IsFloatingSubstitute);
    }

    [Fact]
    public async Task Escenario_3b_sustituto_flotante_tambien_cae_en_sin_grupo_fijo()
    {
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        await h.AddTeachingAssignmentAsync(null, personId);   // sin Sociedad

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());

        result.ShouldSucceed();
        Assert.Equal(SundaySchoolCaptureMode.NoFixedGroup, result.Value.Mode);
        Assert.Empty(result.Value.MySocieties);
        Assert.True(result.Value.IsFloatingSubstitute);
    }

    [Fact]
    public async Task Los_candidatos_de_cada_grupo_incluyen_a_sus_titulares_y_a_los_flotantes()
    {
        // Sección 8.2: al elegir quién dio la clase se ofrecen los maestros del grupo Y los
        // sustitutos sin grupo fijo, que pueden haber cubierto ese domingo.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsSundaySchoolRecorderAsync();

        var titular = await h.AddPersonAsync("Titular", "De Jóvenes");
        var flotante = await h.AddPersonAsync("Sustituto", "Flotante");
        var ajeno = await h.AddPersonAsync("Titular", "De Damas");

        await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Jovenes, titular);
        await h.AddTeachingAssignmentAsync(null, flotante);
        await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Damas, ajeno);

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());
        result.ShouldSucceed();

        var jovenes = result.Value.AllSocieties.Single(s => s.SocietyName == "Jóvenes");
        var ids = jovenes.TeacherCandidates.Select(c => c.PersonId).ToList();

        Assert.Contains(titular, ids);
        Assert.Contains(flotante, ids);
        Assert.DoesNotContain(ajeno, ids);       // el titular de Damas no es candidato aquí

        Assert.True(jovenes.TeacherCandidates.Single(c => c.PersonId == titular).HasFixedGroup);
        Assert.False(jovenes.TeacherCandidates.Single(c => c.PersonId == flotante).HasFixedGroup);
    }

    [Fact]
    public async Task Las_asignaciones_revocadas_no_cuentan()
    {
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsSundaySchoolRecorderAsync();
        var id = await h.AddTeachingAssignmentAsync(CoreSeedData.SocietyIds.Damas, personId);

        var asignacion = await h.Db.SundaySchoolTeachingAssignments.FindAsync(id);
        asignacion!.Revoke(personId, h.Clock.UtcNow);
        await h.Db.SaveChangesAsync();

        var result = await h.Sender.Send(new GetMySundaySchoolCaptureContextQuery());

        result.ShouldSucceed();
        Assert.Equal(SundaySchoolCaptureMode.NoFixedGroup, result.Value.Mode);
    }
}
