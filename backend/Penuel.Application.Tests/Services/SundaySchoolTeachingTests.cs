using Microsoft.EntityFrameworkCore;
using Penuel.Application.Services.SundaySchool.AssignSundaySchoolTeacher;
using Penuel.Application.Services.SundaySchool.RevokeSundaySchoolTeacher;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests.Services;

public sealed class SundaySchoolTeachingTests
{
    [Fact]
    public async Task Una_misma_persona_puede_dar_varias_Sociedades_a_la_vez()
    {
        // Regla 7.7 y el caso real: Damas y Varones se imparten juntas por falta de maestros.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var maestro = await h.AddPersonAsync("Maestro", "Combinado");

        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro)))
            .ShouldSucceed();
        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Varones, maestro)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SundaySchoolTeachingAssignments
            .CountAsync(a => a.PersonId == maestro && a.RevokedAt == null));
    }

    [Fact]
    public async Task Una_misma_Sociedad_puede_tener_varios_maestros_activos()
    {
        // Regla 7.7: dos maestros que se turnan, o un titular más un sustituto habitual.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var uno = await h.AddPersonAsync("Maestro", "Uno");
        var dos = await h.AddPersonAsync("Maestro", "Dos");

        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Jovenes, uno)))
            .ShouldSucceed();
        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Jovenes, dos)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SundaySchoolTeachingAssignments
            .CountAsync(a => a.SocietyId == CoreSeedData.SocietyIds.Jovenes && a.RevokedAt == null));
    }

    [Fact]
    public async Task Se_puede_registrar_un_sustituto_flotante_sin_grupo_fijo()
    {
        // SocietyId nulo NO significa "sin asignar": significa disponible para cualquier grupo.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var sustituto = await h.AddPersonAsync("Sustituto", "Flotante");

        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(null, sustituto))).ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        var asignacion = await h.Db.SundaySchoolTeachingAssignments.SingleAsync();
        Assert.Null(asignacion.SocietyId);
    }

    [Fact]
    public async Task Solo_se_rechaza_el_duplicado_EXACTO()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var maestro = await h.AddPersonAsync();

        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro)))
            .ShouldSucceed();

        var duplicado = await h.Sender.Send(
            new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro));

        duplicado.ShouldFailWith("SundaySchoolTeachingAssignment.AlreadyAssigned", ErrorType.Conflict);
    }

    [Fact]
    public async Task Revocar_conserva_la_fila_y_permite_volver_a_asignar()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var maestro = await h.AddPersonAsync();
        var id = (await h.Sender.Send(
            new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro))).Value;

        (await h.Sender.Send(new RevokeSundaySchoolTeacherCommand(id))).ShouldSucceed();
        (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro)))
            .ShouldSucceed();

        h.Db.ChangeTracker.Clear();
        Assert.Equal(2, await h.Db.SundaySchoolTeachingAssignments.CountAsync());
        Assert.Equal(1, await h.Db.SundaySchoolTeachingAssignments.CountAsync(a => a.RevokedAt == null));
    }

    [Fact]
    public async Task Revocar_dos_veces_falla()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var id = (await h.Sender.Send(new AssignSundaySchoolTeacherCommand(
            CoreSeedData.SocietyIds.Damas, await h.AddPersonAsync()))).Value;
        await h.Sender.Send(new RevokeSundaySchoolTeacherCommand(id));

        (await h.Sender.Send(new RevokeSundaySchoolTeacherCommand(id)))
            .ShouldFailWith("SundaySchoolTeachingAssignment.AlreadyRevoked", ErrorType.Conflict);
    }

    [Fact]
    public async Task Ser_maestro_NO_otorga_ningun_rol_de_sistema()
    {
        // Regla 7.8: son ejes independientes.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var maestro = await h.AddPersonAsync();

        await h.Sender.Send(new AssignSundaySchoolTeacherCommand(CoreSeedData.SocietyIds.Damas, maestro));

        h.Db.ChangeTracker.Clear();
        var cuenta = await h.Db.UserAccounts.FirstOrDefaultAsync(u => u.PersonId == maestro);
        Assert.Null(cuenta);   // ni siquiera tiene cuenta
    }
}
