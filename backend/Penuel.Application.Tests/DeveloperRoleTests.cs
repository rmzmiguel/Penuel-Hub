using Penuel.Application.Memberships.CreateMembership;
using Penuel.Application.Memberships.SetMembershipStatus;
using Penuel.Application.Persons.GetPersonAdministration;
using Penuel.Application.Persons.GetPersons;
using Penuel.Application.Persons.RegisterPerson;
using Penuel.Application.Roles.AssignRole;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;
using Penuel.Domain.Entities;
using Penuel.Domain.Enums;
using Penuel.Infrastructure.Persistence.Seed;

namespace Penuel.Application.Tests;

/// <summary>
/// El rol Desarrollador no es "un Pastor más": SALTA la autorización en lugar de acumular
/// permisos. Estas pruebas fijan esa diferencia, que es justo la que hace que el rol no se
/// quede incompleto cuando mañana alguien añada un marcador nuevo.
/// </summary>
public sealed class DeveloperRoleTests
{
    [Fact]
    public async Task Un_Desarrollador_ejecuta_un_caso_de_uso_exclusivo_del_Pastor()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsDeveloperAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null));

        result.ShouldSucceed();
    }

    [Fact]
    public async Task Un_Desarrollador_pasa_un_marcador_cuyos_roles_y_cargos_no_tiene()
    {
        // GetPersonsQuery exige IRequireDirectoryAccess: Pastor, SundaySchoolRecorder o el
        // cargo de Tesorero General. El Desarrollador no tiene ninguno de los tres y aun así
        // entra — porque no se le comprueba el marcador, se le salta.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsDeveloperAsync();

        (await h.Sender.Send(new GetPersonsQuery(null))).ShouldSucceed();
    }

    [Fact]
    public async Task El_Desarrollador_no_necesita_cargo_ni_liderazgo_ni_membresia()
    {
        // Es el punto entero del rol: quien mantiene el sistema no pertenece a la estructura
        // de la iglesia. Si esta prueba dejara de pasar, el rol se habría convertido en un
        // puesto de la congregación.
        await using var h = await TestHarness.CreateAsync();
        var (personId, _) = await h.SignInAsDeveloperAsync();

        Assert.Empty(h.Db.PersonPositions.Where(pp => pp.PersonId == personId));
        Assert.Empty(h.Db.MinistryLeaderships.Where(l => l.PersonId == personId));
        Assert.Empty(h.Db.SocietyLeaderships.Where(l => l.PersonId == personId));
        Assert.Empty(h.Db.Memberships.Where(m => m.PersonId == personId));

        (await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null))).ShouldSucceed();
    }

    [Fact]
    public async Task Una_cuenta_sin_rol_de_superusuario_sigue_recibiendo_403()
    {
        // Guarda de regresión del atajo: que exista no puede haber abierto la puerta a nadie más.
        await using var h = await TestHarness.CreateAsync();
        var personId = await h.AddPersonAsync();
        var cuentaId = await h.AddUserAccountAsync(personId, "cualquiera@penuel.mx");
        h.CurrentUser.SignInAs(personId, cuentaId, "Secretary");

        (await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null)))
            .ShouldFailWith("Auth.PastorRoleRequired", ErrorType.Forbidden);
    }

    [Fact]
    public async Task Quitarle_el_rol_le_cierra_la_puerta_igual_que_a_cualquiera()
    {
        // El atajo lee los claims, así que conviene dejar escrito que NO sobrevive a la
        // revocación: OnTokenValidated revalida contra la base en cada petición.
        await using var h = await TestHarness.CreateAsync();
        var (personId, cuentaId) = await h.SignInAsDeveloperAsync();

        var userRole = h.Db.UserRoles.Single(ur => ur.UserAccountId == cuentaId);
        userRole.Revoke(personId, h.Clock.UtcNow);
        await h.Db.SaveChangesAsync();

        // Así es como llega la siguiente petición una vez revalidado el token: sin el rol.
        h.CurrentUser.SignInAs(personId, cuentaId);

        (await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null)))
            .ShouldFailWith("Auth.PastorRoleRequired", ErrorType.Forbidden);
    }

    [Fact]
    public async Task El_rol_esta_sembrado_y_el_Pastor_puede_otorgarlo()
    {
        // Sin la fila en `roles`, la constante de RoleNames no serviría de nada: AssignRole
        // busca el rol por nombre en la tabla.
        await using var h = await TestHarness.CreateAsync();
        await h.SignInAsPastorAsync();

        var personId = await h.AddPersonAsync("Miguel", "Ramírez");
        var cuentaId = await h.AddUserAccountAsync(personId, "miguel@penuel.mx");

        (await h.Sender.Send(new AssignRoleCommand(cuentaId, RoleNames.Developer))).ShouldSucceed();

        var rol = h.Db.Roles.Single(r => r.Id == CoreSeedData.RoleIds.Developer);
        Assert.Equal(RoleNames.Developer, rol.Name);
        Assert.True(rol.IsSystemRole);
    }
}
