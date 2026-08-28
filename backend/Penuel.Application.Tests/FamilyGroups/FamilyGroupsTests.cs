using Microsoft.EntityFrameworkCore;
using Penuel.Application.FamilyGroups.AddExistingPersonToGroup;
using Penuel.Application.FamilyGroups.CreateFamilyGroup;
using Penuel.Application.FamilyGroups.GetMyFamilyGroups;
using Penuel.Application.FamilyGroups.RegisterAndAddGroupMember;
using Penuel.Application.FamilyGroups.RemoveGroupMember;
using Penuel.Application.FamilyGroups.SearchAvailablePersons;
using Penuel.Application.FamilyGroups.SubmitFamilyGroupReport;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.FamilyGroups;

namespace Penuel.Application.Tests.FamilyGroups;

/// <summary>
/// La rama de Grupos Familiares: el tercer patrón de autorización y la regla global de
/// "un solo grupo por persona", que es la inversa de la del Core.
/// </summary>
public sealed class FamilyGroupsTests
{
    [Fact]
    public async Task El_Pastor_crea_un_grupo_y_sin_Encargado_distinto_lo_es_el_Anfitrion()
    {
        // Regla 7.1: LeaderPersonId nunca queda nulo.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var anfitrion = await h.AddPersonAsync("Rosa", "Ibarra");

        var creado = await h.Sender.Send(
            new CreateFamilyGroupCommand(anfitrion, null, "Calle Hidalgo 120", null));

        creado.ShouldSucceed();
        var grupo = await h.ReloadAsync<FamilyGroup>(creado.Value);
        Assert.Equal(anfitrion, grupo!.HostPersonId);
        Assert.Equal(anfitrion, grupo.LeaderPersonId);
        Assert.Equal(DayOfWeek.Thursday, grupo.DefaultMeetingDayOfWeek);
    }

    [Fact]
    public async Task Una_persona_sin_ningun_rol_ni_cargo_opera_su_propio_grupo()
    {
        // El corazón de la rama (Sección 2.2): autorización resuelta contra el RECURSO.
        // Esta persona no tiene Role, ni Position, ni liderazgo de nada.
        await using var h = await TestHarness.CreateAsync();
        var (grupo, anfitrionId) = await CrearGrupoAsync(h);

        h.CurrentUser.SignInAs(anfitrionId, Guid.NewGuid());   // sesión sin un solo rol

        var alta = await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(grupo, "Elena", "Ruiz", null));

        alta.ShouldSucceed();
        Assert.Empty(h.CurrentUser.Roles);
    }

    [Fact]
    public async Task Quien_no_lleva_el_grupo_no_puede_tocarlo_ni_sabe_que_existe()
    {
        // Sección 2.1: el mismo error tanto si el grupo no existe como si no es suyo.
        // Distinguirlos le diría a un Anfitrión que hay otras casas ahí fuera.
        await using var h = await TestHarness.CreateAsync();
        var (grupo, _) = await CrearGrupoAsync(h);

        var ajeno = await h.AddPersonAsync("Ajeno", "Cualquiera");
        h.CurrentUser.SignInAs(ajeno, Guid.NewGuid());

        var suyo = await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(grupo, "Elena", "Ruiz", null));
        var inventado = await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(Guid.NewGuid(), "Elena", "Ruiz", null));

        suyo.ShouldFailWith("FamilyGroup.NotYours", ErrorType.Forbidden);
        // Idéntico: ni el código ni el mensaje delatan que uno existía y el otro no.
        Assert.Equal(suyo.Error!.Code, inventado.Error!.Code);
        Assert.Equal(suyo.Error.Message, inventado.Error.Message);
    }

    [Fact]
    public async Task El_Encargado_tiene_exactamente_los_mismos_permisos_que_el_Anfitrion()
    {
        // Sección 3.1: la distinción entre ambos es informativa, no una jerarquía de acceso.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();
        var anfitrion = await h.AddPersonAsync("Rosa", "Ibarra");
        var encargado = await h.AddPersonAsync("Norma", "Castillo");

        var creado = await h.Sender.Send(
            new CreateFamilyGroupCommand(anfitrion, encargado, "Calle Juárez 45", null));
        creado.ShouldSucceed();

        h.CurrentUser.SignInAs(encargado, Guid.NewGuid());

        (await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(creado.Value, "Elena", "Ruiz", null)))
            .ShouldSucceed();
    }

    [Fact]
    public async Task Registrar_a_alguien_desde_el_grupo_NUNCA_lo_hace_miembro_oficial()
    {
        // Regla 7.4: no es una validación que se pueda olvidar — el comando no tiene dónde.
        await using var h = await TestHarness.CreateAsync();
        var (grupo, anfitrionId) = await CrearGrupoAsync(h);
        h.CurrentUser.SignInAs(anfitrionId, Guid.NewGuid());

        var creada = await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(grupo, "Elena", "Ruiz", null));
        creada.ShouldSucceed();

        Assert.False(await h.Db.Memberships.AnyAsync(m => m.PersonId == creada.Value));
        Assert.False(await h.Db.UserAccounts.AnyAsync(u => u.PersonId == creada.Value));

        // Y la prueba estructural que sobrevive a un refactor: si alguien añadiera mañana un
        // parámetro de membresía al comando, esto se cae.
        var parametros = typeof(RegisterAndAddGroupMemberCommand)
            .GetProperties().Select(p => p.Name.ToLowerInvariant());
        Assert.DoesNotContain(parametros, n => n.Contains("member") || n.Contains("membership"));
    }

    [Fact]
    public async Task La_misma_persona_no_puede_estar_en_dos_grupos_a_la_vez()
    {
        // Regla 7.2, provocada de verdad contra la base y no solo revisada.
        await using var h = await TestHarness.CreateAsync();
        var (pastorId, _) = await h.SignInAsPastorAsync();

        var casaA = await CrearGrupoConAnfitrionAsync(h, "Ana", "Gómez", "Casa A");
        var casaB = await CrearGrupoConAnfitrionAsync(h, "Luis", "Martínez", "Casa B");
        var visitante = await h.AddPersonAsync("Sara", "Ríos");

        (await h.Sender.Send(new AddExistingPersonToGroupCommand(casaA, visitante))).ShouldSucceed();

        var segunda = await h.Sender.Send(new AddExistingPersonToGroupCommand(casaB, visitante));

        segunda.ShouldFailWith("GroupMember.AlreadyInAnotherGroup", ErrorType.Conflict);
        // Regla 7.5: el mensaje no dice a qué casa pertenece.
        Assert.DoesNotContain("Casa A", segunda.Error!.Message);

        // Y el índice de la base lo impide aunque alguien se salte el comando.
        h.Db.GroupMembers.Add(GroupMember.Add(
            casaB, visitante, new DateOnly(2026, 3, 1), pastorId));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => h.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Mover_a_alguien_de_un_grupo_a_otro_funciona_en_secuencia()
    {
        // Regla 7.3: quitar y agregar son dos actos. La fila cerrada deja de contar para el
        // índice, que es justo lo que hace posible el movimiento.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var casaA = await CrearGrupoConAnfitrionAsync(h, "Ana", "Gómez", "Casa A");
        var casaB = await CrearGrupoConAnfitrionAsync(h, "Luis", "Martínez", "Casa B");
        var visitante = await h.AddPersonAsync("Sara", "Ríos");

        (await h.Sender.Send(new AddExistingPersonToGroupCommand(casaA, visitante))).ShouldSucceed();
        (await h.Sender.Send(new RemoveGroupMemberCommand(casaA, visitante))).ShouldSucceed();
        (await h.Sender.Send(new AddExistingPersonToGroupCommand(casaB, visitante))).ShouldSucceed();

        // Dos filas: la cerrada conserva el historial, la abierta es la vigente (regla 7.6).
        var filas = await h.Db.GroupMembers.Where(m => m.PersonId == visitante).ToListAsync();
        Assert.Equal(2, filas.Count);
        Assert.Single(filas, m => m.LeftAt == null && m.FamilyGroupId == casaB);
    }

    [Fact]
    public async Task El_reporte_semanal_no_admite_una_fecha_futura()
    {
        await using var h = await TestHarness.CreateAsync();
        var (grupo, anfitrionId) = await CrearGrupoAsync(h);
        h.CurrentUser.SignInAs(anfitrionId, Guid.NewGuid());

        var manana = DateOnly.FromDateTime(h.Clock.UtcNow.UtcDateTime).AddDays(1);

        var result = await h.Sender.Send(
            new SubmitFamilyGroupReportCommand(grupo, manana, 250m, []));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task El_reporte_acepta_cualquier_dia_de_la_semana()
    {
        // Regla 7.7: el día habitual es informativo. Quien mueve su jueves porque de verdad
        // no pudo no debe encontrarse con que el sistema se lo discute.
        await using var h = await TestHarness.CreateAsync();
        var (grupo, anfitrionId) = await CrearGrupoAsync(h);
        h.CurrentUser.SignInAs(anfitrionId, Guid.NewGuid());

        var persona = await h.Sender.Send(
            new RegisterAndAddGroupMemberCommand(grupo, "Elena", "Ruiz", null));
        persona.ShouldSucceed();

        // 24 de febrero de 2026 es martes, no jueves — y anterior al reloj fijo del harness,
        // que está en domingo 1 de marzo.
        var martes = new DateOnly(2026, 2, 24);
        Assert.Equal(DayOfWeek.Tuesday, martes.DayOfWeek);

        var reporte = await h.Sender.Send(new SubmitFamilyGroupReportCommand(
            grupo, martes, 320.50m,
            [new FamilyGroupAttendanceInput(persona.Value, true)]));

        reporte.ShouldSucceed();
        var guardado = await h.Db.FamilyGroupMeetings
            .Include(m => m.Attendances)
            .FirstAsync(m => m.Id == reporte.Value);
        Assert.Equal(320.50m, guardado.TotalOffering);
        Assert.Single(guardado.Attendances);
    }

    [Fact]
    public async Task El_buscador_marca_a_quien_ya_tiene_grupo_sin_decir_cual()
    {
        // Regla 7.5. Se devuelven también las no disponibles, marcadas: si no aparecieran,
        // quien busca supondría que no están registradas y las daría de alta otra vez.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var casaA = await CrearGrupoConAnfitrionAsync(h, "Ana", "Gómez", "Casa A");
        var casaB = await CrearGrupoConAnfitrionAsync(h, "Luis", "Martínez", "Casa B");
        var ocupada = await h.AddPersonAsync("Sara", "Ríos");
        (await h.Sender.Send(new AddExistingPersonToGroupCommand(casaA, ocupada))).ShouldSucceed();

        var resultado = await h.Sender.Send(new SearchAvailablePersonsQuery(casaB, "Sara"));

        resultado.ShouldSucceed();
        var fila = Assert.Single(resultado.Value, p => p.PersonId == ocupada);
        Assert.False(fila.IsAvailable);
        // El contrato no tiene dónde meter el nombre del otro grupo, ni por descuido.
        Assert.DoesNotContain(typeof(AvailablePerson).GetProperties(),
            p => p.Name.Contains("Group", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task El_Anfitrion_solo_ve_su_grupo_y_con_todo_lo_que_su_pantalla_necesita()
    {
        // Sección 8.4: si devuelve exactamente uno, el frontend entra directo sin selector.
        await using var h = await TestHarness.CreateAsync();
        var (grupo, anfitrionId) = await CrearGrupoAsync(h);
        await CrearGrupoConAnfitrionAsync(h, "Otro", "Anfitrión", "Casa ajena");

        h.CurrentUser.SignInAs(anfitrionId, Guid.NewGuid());
        (await h.Sender.Send(new RegisterAndAddGroupMemberCommand(grupo, "Elena", "Ruiz", null)))
            .ShouldSucceed();

        var mios = await h.Sender.Send(new GetMyFamilyGroupsQuery());

        mios.ShouldSucceed();
        var unico = Assert.Single(mios.Value);
        Assert.Equal(grupo, unico.FamilyGroupId);
        Assert.True(unico.IsHost);
        Assert.True(unico.IsLeader);
        Assert.Single(unico.Members);   // la lista de asistencia viene en la misma llamada
    }

    [Fact]
    public async Task Sin_grupo_la_consulta_devuelve_vacio_y_no_falla()
    {
        // Es el caso de casi toda la congregación: no tiene grupo y no pasa nada.
        await using var h = await TestHarness.CreateAsync();
        var cualquiera = await h.AddPersonAsync("Cualquiera", "De La Iglesia");
        h.CurrentUser.SignInAs(cualquiera, Guid.NewGuid());

        var mios = await h.Sender.Send(new GetMyFamilyGroupsQuery());

        mios.ShouldSucceed();
        Assert.Empty(mios.Value);
    }

    /* ── ayudas ──────────────────────────────────────────────────────────── */

    /// <summary>Entra como Pastor, crea un grupo y devuelve su Id junto al del Anfitrión.</summary>
    private static async Task<(Guid GrupoId, Guid AnfitrionId)> CrearGrupoAsync(TestHarness h)
    {
        await h.SignInAsPastorAsync();
        var host = await h.AddPersonAsync("Rosa", "Ibarra");

        var creado = await h.Sender.Send(
            new CreateFamilyGroupCommand(host, null, "Calle Hidalgo 120", null));
        creado.ShouldSucceed();
        return (creado.Value, host);
    }

    private static async Task<Guid> CrearGrupoConAnfitrionAsync(
        TestHarness h, string nombre, string apellido, string direccion)
    {
        var host = await h.AddPersonAsync(nombre, apellido);
        var creado = await h.Sender.Send(new CreateFamilyGroupCommand(host, null, direccion, null));
        creado.ShouldSucceed();
        return creado.Value;
    }
}
