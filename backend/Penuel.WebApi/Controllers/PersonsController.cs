using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Persons.DeactivatePerson;
using Penuel.Application.Persons.GetPersonAdministration;
using Penuel.Application.Persons.GetPersons;
using Penuel.Application.Persons.ReactivatePerson;
using Penuel.Application.Persons.RegisterPerson;
using Penuel.Application.Persons.UpdatePerson;
using Penuel.Application.UserAccounts.CreateUserAccount;
using Penuel.Application.UserAccounts.SetUserAccountAccess;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Credenciales de acceso para una persona ya registrada.</summary>
public sealed record CreateUserAccountRequest(string Email, string Password);

/// <summary>Encender o apagar el acceso de una cuenta ya existente.</summary>
public sealed record SetUserAccountAccessRequest(bool IsActive);

/// <summary>Corrección de la ficha. Nada de lo que la persona ES en la iglesia.</summary>
public sealed record UpdatePersonRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber);

[Route("api/persons")]
// La política va por ACCIÓN y no en la clase: la lectura del directorio la necesitan también
// el Tesorero y los capturistas de Escuela Dominical, no solo el Pastor. Los atributos
// [Authorize] se acumulan, no se sobrescriben, así que uno a nivel de clase encerraría todo.
[Authorize]
public sealed class PersonsController : ApiController
{
    public PersonsController(ISender sender) : base(sender) { }

    /// <summary>
    /// Directorio de personas activas, para poblar los selectores de captura.
    /// Devuelve solo nombre y apellido.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PersonOption>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new GetPersonsQuery(search), cancellationToken)).ToActionResult();

    /// <summary>Registra a una persona. No la hace miembro ni le da acceso (Sección 3).</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterPersonCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Corrige nombre, apellidos, fecha de nacimiento y teléfono. No toca membresía,
    /// cargos ni roles: cada una de esas cosas tiene su propia operación.
    /// </summary>
    [HttpPut("{personId:guid}")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid personId,
        UpdatePersonRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new UpdatePersonCommand(
                personId, request.FirstName, request.LastName,
                request.DateOfBirth, request.PhoneNumber),
            cancellationToken)).ToActionResult();

    /// <summary>Borrado lógico (regla 7.3). La fila nunca se elimina.</summary>
    [HttpPost("{personId:guid}/deactivate")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid personId, CancellationToken cancellationToken) =>
        (await Sender.Send(new DeactivatePersonCommand(personId), cancellationToken)).ToActionResult();

    [HttpPost("{personId:guid}/reactivate")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reactivate(Guid personId, CancellationToken cancellationToken) =>
        (await Sender.Send(new ReactivatePersonCommand(personId), cancellationToken)).ToActionResult();

    /// <summary>Le crea credenciales de acceso. Ortogonal a la membresía (Sección 3.3).</summary>
    [HttpPost("{personId:guid}/user-account")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUserAccount(
        Guid personId,
        CreateUserAccountRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new CreateUserAccountCommand(personId, request.Email, request.Password),
            cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Enciende o apaga el acceso de la cuenta. Es la vuelta atrás de crearla: la fila nunca
    /// se borra (regla 7.3), así que "quitar el acceso" solo puede ser esto.
    /// </summary>
    [HttpPut("{personId:guid}/user-account/access")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetUserAccountAccess(
        Guid personId,
        SetUserAccountAccessRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new SetUserAccountAccessCommand(personId, request.IsActive),
            cancellationToken)).ToActionResult();

    /// <summary>
    /// Estado administrativo completo de una persona: cuenta, roles, cargos, liderazgos,
    /// sociedades y membresía. Incluye los catálogos de roles y cargos con la marca de cuáles
    /// tiene, para que el panel de permisos se dibuje con una sola llamada.
    /// </summary>
    [HttpGet("{personId:guid}/administration")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(PersonAdministrationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdministration(Guid personId, CancellationToken cancellationToken) =>
        (await Sender.Send(new GetPersonAdministrationQuery(personId), cancellationToken)).ToActionResult();
}
