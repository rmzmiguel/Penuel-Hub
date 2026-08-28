using MediatR;
using Penuel.Application.Abstractions;
using Penuel.Application.Auth.Login;
using Penuel.Application.Capabilities.GetMyCapabilities;
using Penuel.Application.Persons.RegisterPerson;
using Penuel.Application.Tests.Harness;
using Penuel.Domain.Common;

namespace Penuel.Application.Tests;

/// <summary>
/// Regla 7.5 y regla por defecto de la Sección 8.2, verificadas en el pipeline de MediatR
/// —donde de verdad se aplican— y no solo en los atributos de los controladores.
/// </summary>
public sealed class AuthorizationTests
{
    [Fact]
    public async Task Sin_sesion_un_caso_de_uso_protegido_devuelve_401_y_no_una_excepcion()
    {
        await using var h = await TestHarness.CreateAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null));

        result.ShouldFailWith("Auth.NotAuthenticated", ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Con_sesion_pero_sin_el_rol_Pastor_devuelve_403_y_no_401()
    {
        // La distinción importa: ante un 401 el frontend manda a iniciar sesión;
        // ante un 403 no debe hacerlo, porque la sesión es válida.
        await using var h = await TestHarness.CreateAsync();
        var personId = await h.AddPersonAsync();
        var cuentaId = await h.AddUserAccountAsync(personId, "secretaria@penuel.mx");
        h.CurrentUser.SignInAs(personId, cuentaId, "Secretary");

        var result = await h.Sender.Send(new RegisterPersonCommand("Ana", "Ramírez", null, null));

        result.ShouldFailWith("Auth.PastorRoleRequired", ErrorType.Forbidden);
    }

    [Fact]
    public async Task La_autorizacion_corre_antes_que_la_validacion()
    {
        // A quien no tiene permiso no se le gastan ciclos validando su payload,
        // ni se le devuelven pistas sobre él.
        await using var h = await TestHarness.CreateAsync();

        var result = await h.Sender.Send(new RegisterPersonCommand("", "", null, null));

        Assert.Equal("Auth.NotAuthenticated", result.Error!.Code);
    }

    [Fact]
    public async Task Login_y_GetMyCapabilities_no_exigen_el_rol_Pastor()
    {
        await using var h = await TestHarness.CreateAsync();
        var personId = await h.AddPersonAsync();
        var cuentaId = await h.AddUserAccountAsync(personId, "cualquiera@penuel.mx", "contrasena-de-prueba");

        // Login: ni siquiera requiere sesión.
        (await h.Sender.Send(new LoginQuery("cualquiera@penuel.mx", "contrasena-de-prueba")))
            .ShouldSucceed();

        // GetMyCapabilities: basta con estar autenticado, sin ningún rol.
        h.CurrentUser.SignInAs(personId, cuentaId);
        var capacidades = await h.Sender.Send(new GetMyCapabilitiesQuery());

        capacidades.ShouldSucceed();
        Assert.Empty(capacidades.Value.Roles);
    }

    [Fact]
    public void Todo_caso_de_uso_declara_explicitamente_su_proteccion()
    {
        // Guardia estructural que sobrevive a las ramas: en vez de contar casos de uso o de
        // exigir un marcador concreto, comprueba el invariante que de verdad importa —
        // NINGÚN caso de uso queda sin declarar su autorización, salvo los tres que están
        // abiertos a propósito (Sección 8.2 del Core).
        string[] abiertos = ["LoginQuery", "RefreshTokenCommand", "GetMyCapabilitiesQuery"];

        var casosDeUso = typeof(Penuel.Application.DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToList();

        Assert.True(casosDeUso.Count >= 20,
            $"Se esperaban al menos los 20 casos de uso del Core; se encontraron {casosDeUso.Count}.");

        foreach (var nombre in abiertos)
        {
            Assert.Contains(casosDeUso, t => t.Name == nombre);
        }

        // `IAuthorizeInHandler` cuenta como declaración: dice explícitamente "la decisión
        // depende del recurso y la toma el handler". Lo que el guardián persigue es el caso
        // de uso que no declara NADA, no el que declara un patrón distinto.
        static bool DeclaraProteccion(Type t) =>
            typeof(IRequirePastor).IsAssignableFrom(t)
            || typeof(IRequireAuthorization).IsAssignableFrom(t)
            || typeof(IAuthorizeInHandler).IsAssignableFrom(t);

        var sinMarcador = casosDeUso
            .Where(t => !abiertos.Contains(t.Name))
            .Where(t => !DeclaraProteccion(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(sinMarcador.Count == 0,
            "Estos casos de uso no declaran ninguna autorización y quedarían abiertos a cualquiera: "
            + string.Join(", ", sinMarcador));

        // Y a la inversa: que nadie proteja por accidente uno de los que deben estar abiertos.
        var protegidosDeMas = casosDeUso
            .Where(t => abiertos.Contains(t.Name))
            .Where(DeclaraProteccion)
            .Select(t => t.Name)
            .ToList();

        Assert.True(protegidosDeMas.Count == 0,
            "Estos deben quedar abiertos y alguien los protegió: " + string.Join(", ", protegidosDeMas));
    }
}
