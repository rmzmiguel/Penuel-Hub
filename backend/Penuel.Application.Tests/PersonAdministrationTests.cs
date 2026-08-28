using Microsoft.EntityFrameworkCore;
using Penuel.Application.Capabilities.GetMyCapabilities;
using Penuel.Application.Memberships.CreateMembership;
using Penuel.Application.Memberships.SetMembershipStatus;
using Penuel.Application.FamilyGroups.AddExistingPersonToGroup;
using Penuel.Application.FamilyGroups.CreateFamilyGroup;
using Penuel.Application.FamilyGroups.SubmitFamilyGroupReport;
using Penuel.Application.Persons.GetPersonAdministration;
using Penuel.Application.Persons.UpdatePerson;
using Penuel.Application.Roles.AssignRole;
using Penuel.Application.Tests.Harness;
using Penuel.Application.UserAccounts.SetUserAccountAccess;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;

namespace Penuel.Application.Tests;

/// <summary>
/// Las operaciones que faltaban para que el panel de permisos pudiera DESHACER lo que hace, y
/// la lectura que le dice qué está encendido.
/// </summary>
public sealed class PersonAdministrationTests
{
    [Fact]
    public async Task Quitar_el_acceso_desactiva_la_cuenta_y_mata_las_sesiones_vivas()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var cuentaId = await h.AddUserAccountAsync(personId, "miguel@penuel.mx");
        h.Db.RefreshTokens.Add(RefreshToken.Issue(cuentaId, "token-vivo", h.Clock.UtcNow.AddDays(7), h.Clock.UtcNow));
        await h.Db.SaveChangesAsync();

        (await h.Sender.Send(new SetUserAccountAccessCommand(personId, false))).ShouldSucceed();

        var cuenta = await h.ReloadAsync<UserAccount>(cuentaId);
        Assert.False(cuenta!.IsActive);

        // La otra mitad del candado: sin esto seguiría renovando su token hasta que expirara.
        var vivos = await h.Db.RefreshTokens
            .Where(t => t.UserAccountId == cuentaId && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, vivos);
    }

    [Fact]
    public async Task Devolver_el_acceso_reactiva_la_misma_cuenta_sin_crear_otra()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var cuentaId = await h.AddUserAccountAsync(personId, "miguel@penuel.mx");

        (await h.Sender.Send(new SetUserAccountAccessCommand(personId, false))).ShouldSucceed();
        (await h.Sender.Send(new SetUserAccountAccessCommand(personId, true))).ShouldSucceed();

        Assert.True((await h.ReloadAsync<UserAccount>(cuentaId))!.IsActive);
        Assert.Equal(1, await h.Db.UserAccounts.CountAsync(u => u.PersonId == personId));
    }

    [Fact]
    public async Task Nadie_puede_desactivar_su_propia_cuenta()
    {
        // Es el único movimiento del panel sin vuelta atrás para quien lo hace.
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();

        (await h.Sender.Send(new SetUserAccountAccessCommand(pastorId, false)))
            .ShouldFailWith("UserAccount.CannotDeactivateOwn", ErrorType.Conflict);
    }

    [Fact]
    public async Task Dar_de_baja_la_membresia_conserva_la_fila_y_su_fecha_de_ingreso()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var ingreso = new DateOnly(2020, 5, 10);
        (await h.Sender.Send(new CreateMembershipCommand(personId, ingreso))).ShouldSucceed();

        (await h.Sender.Send(new SetMembershipStatusCommand(personId, false))).ShouldSucceed();

        var membresia = await h.Db.Memberships.SingleAsync(m => m.PersonId == personId);
        Assert.Equal(MembershipStatus.FormerMember, membresia.Status);
        // Un libro de miembros no puede perder cuándo entró alguien por haberle dado de baja.
        Assert.Equal(ingreso, membresia.JoinedAt);
    }

    [Fact]
    public async Task Un_exmiembro_deja_de_contar_como_miembro_oficial()
    {
        // Antes esto se preguntaba solo por la EXISTENCIA de la fila, así que dar de baja no
        // se reflejaba en ningún sitio.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var cuentaId = await h.AddUserAccountAsync(personId, "miguel@penuel.mx");
        (await h.Sender.Send(new CreateMembershipCommand(personId, null))).ShouldSucceed();
        (await h.Sender.Send(new SetMembershipStatusCommand(personId, false))).ShouldSucceed();

        h.CurrentUser.SignInAs(personId, cuentaId);
        var capacidades = await h.Sender.Send(new GetMyCapabilitiesQuery());

        capacidades.ShouldSucceed();
        Assert.False(capacidades.Value.IsOfficialMember);
    }

    [Fact]
    public async Task Restituir_la_membresia_reutiliza_la_fila_en_vez_de_crear_otra()
    {
        // La regla 7.2 dice que hay como mucho UNA membresía por persona, así que restituir
        // no puede ser "crear otra": tiene que ser una transición sobre la que ya está.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        (await h.Sender.Send(new CreateMembershipCommand(personId, null))).ShouldSucceed();
        (await h.Sender.Send(new SetMembershipStatusCommand(personId, false))).ShouldSucceed();
        (await h.Sender.Send(new SetMembershipStatusCommand(personId, true))).ShouldSucceed();

        Assert.Equal(1, await h.Db.Memberships.CountAsync(m => m.PersonId == personId));
        Assert.Equal(MembershipStatus.Active,
            (await h.Db.Memberships.SingleAsync(m => m.PersonId == personId)).Status);
    }

    [Fact]
    public async Task El_panel_recibe_el_catalogo_completo_de_roles_marcando_los_otorgados()
    {
        // Es lo que permite que el frontend dibuje los interruptores sin escribir un solo
        // nombre de rol en su código.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var cuentaId = await h.AddUserAccountAsync(personId, "miguel@penuel.mx");
        (await h.Sender.Send(new AssignRoleCommand(cuentaId, RoleNames.Developer))).ShouldSucceed();

        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(personId));

        estado.ShouldSucceed();
        Assert.Equal(RoleNames.All.Count, estado.Value.Roles.Count);
        Assert.True(estado.Value.Roles.Single(r => r.Name == RoleNames.Developer).Granted);
        Assert.False(estado.Value.Roles.Single(r => r.Name == RoleNames.Pastor).Granted);
        Assert.True(estado.Value.HasAccount);
        Assert.True(estado.Value.AccountIsActive);
        Assert.Equal("miguel@penuel.mx", estado.Value.Email);
        Assert.False(estado.Value.IsOfficialMember);
        // Los cargos llegan igual: catálogo completo con la marca de cuáles ostenta.
        Assert.NotEmpty(estado.Value.Positions);
        Assert.All(estado.Value.Positions, p => Assert.False(p.Held));
    }

    [Fact]
    public async Task Los_grupos_llegan_completos_con_su_lider_actual()
    {
        // Sin el catálogo completo, el panel podría quitar un liderazgo pero nunca ponerlo.
        // Y sin el nombre del líder actual, asignar uno a un grupo que ya lo tiene fallaría
        // sin explicación visible.
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync("Miguel", "Ramírez");

        var sociedad = h.Db.Societies.OrderBy(sc => sc.Name).First();
        h.Db.SocietyLeaderships.Add(
            SocietyLeadership.Assign(sociedad.Id, pastorId, pastorId, h.Clock.UtcNow));
        await h.Db.SaveChangesAsync();

        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(personId));

        estado.ShouldSucceed();
        Assert.NotEmpty(estado.Value.Ministries);
        Assert.NotEmpty(estado.Value.Societies);

        var conLider = estado.Value.Societies.Single(sc => sc.SocietyId == sociedad.Id);
        Assert.False(conLider.LedByThisPerson);       // lo lidera el Pastor, no esta persona
        Assert.Equal("Fermín Ramírez", conLider.CurrentLeaderName);
        Assert.False(conLider.IsMember);
        Assert.Null(conLider.SocietyMembershipId);
    }

    [Fact]
    public async Task Se_puede_corregir_la_ficha_sin_tocar_lo_que_la_persona_ES()
    {
        // Un apellido mal tecleado se quedaba mal para siempre: el Dominio sabía corregirlo
        // desde el Core y no había comando que lo invocara.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Rosa", "Ibara");   // con la errata
        (await h.Sender.Send(new CreateMembershipCommand(personId, null))).ShouldSucceed();

        (await h.Sender.Send(new UpdatePersonCommand(
            personId, "Rosa María", "Ibarra Ponce", new DateOnly(1968, 4, 12), "834 111 2233")))
            .ShouldSucceed();

        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(personId));
        estado.ShouldSucceed();
        Assert.Equal("Rosa María", estado.Value.FirstName);
        Assert.Equal("Ibarra Ponce", estado.Value.LastName);
        Assert.Equal(new DateOnly(1968, 4, 12), estado.Value.DateOfBirth);
        // Y lo que la persona ES en la iglesia sigue intacto.
        Assert.True(estado.Value.IsOfficialMember);
    }

    [Fact]
    public async Task El_detalle_dice_a_que_casa_asiste_y_quien_la_lleva()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var anfitrion = await h.AddPersonAsync("Rosa", "Ibarra");
        var creado = await h.Sender.Send(
            new CreateFamilyGroupCommand(anfitrion, null, "Calle Hidalgo 120", null));
        creado.ShouldSucceed();

        var visitante = await h.AddPersonAsync("Elena", "Ruiz");
        (await h.Sender.Send(new AddExistingPersonToGroupCommand(creado.Value, visitante)))
            .ShouldSucceed();

        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(visitante));

        estado.ShouldSucceed();
        var casa = estado.Value.FamilyGroup;
        Assert.NotNull(casa);
        Assert.Equal("Calle Hidalgo 120", casa!.Address);
        Assert.False(casa.IsHost);
        Assert.False(casa.IsLeader);
        Assert.Equal("Rosa Ibarra", casa.HostName);
        // Sin Encargado distinto, el Anfitrión lo es (regla 7.1): los dos nombres coinciden.
        Assert.Equal("Rosa Ibarra", casa.LeaderName);
    }

    [Fact]
    public async Task La_racha_mezcla_las_dos_fuentes_y_va_de_lo_viejo_a_lo_nuevo()
    {
        // "¿Qué tan constante es?" no se responde mirando media vida: quien no falta a su
        // grupo del jueves pero nunca va al culto no es inconstante, es otra cosa.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var anfitrion = await h.AddPersonAsync("Rosa", "Ibarra");
        var creado = await h.Sender.Send(
            new CreateFamilyGroupCommand(anfitrion, null, "Calle Hidalgo 120", null));
        creado.ShouldSucceed();

        var persona = await h.AddPersonAsync("Elena", "Ruiz");
        (await h.Sender.Send(new AddExistingPersonToGroupCommand(creado.Value, persona)))
            .ShouldSucceed();

        h.CurrentUser.SignInAs(anfitrion, Guid.NewGuid());
        foreach (var (dia, vino) in new[]
                 { (new DateOnly(2026, 2, 5), true), (new DateOnly(2026, 2, 12), false) })
        {
            (await h.Sender.Send(new SubmitFamilyGroupReportCommand(
                creado.Value, dia, 100m, [new FamilyGroupAttendanceInput(persona, vino)])))
                .ShouldSucceed();
        }

        await h.SignInAsPastorAsync("pastor2@penuel.mx");
        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(persona));

        estado.ShouldSucceed();
        var racha = estado.Value.RecentAttendance.ToList();
        Assert.Equal(2, racha.Count);
        // Ascendente: la fila de puntos se lee del pasado hacia hoy.
        Assert.True(racha[0].Date < racha[1].Date);
        Assert.True(racha[0].WasPresent);
        Assert.False(racha[1].WasPresent);
        Assert.Equal("Grupo Familiar", racha[0].Source);
    }

    [Fact]
    public async Task Sin_cuenta_el_panel_no_inventa_roles()
    {
        // Un rol se otorga a unas credenciales, no a una persona (regla 7.4): sin cuenta,
        // ninguno puede estar otorgado.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var personId = await h.AddPersonAsync("Sin", "Cuenta");

        var estado = await h.Sender.Send(new GetPersonAdministrationQuery(personId));

        estado.ShouldSucceed();
        Assert.False(estado.Value.HasAccount);
        Assert.Null(estado.Value.UserAccountId);
        Assert.All(estado.Value.Roles, r => Assert.False(r.Granted));
    }
}
