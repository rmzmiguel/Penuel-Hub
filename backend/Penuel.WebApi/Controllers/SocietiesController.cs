using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Societies.AddSocietyMember;
using Penuel.Application.Societies.AssignSocietyLeader;
using Penuel.Application.Societies.GetSocietyMembers;
using Penuel.Application.Societies.RemoveSocietyMember;
using Penuel.Application.Societies.CreateSociety;
using Penuel.Application.Societies.RevokeSocietyLeader;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Persona a agregar a un grupo.</summary>
public sealed record AddSocietyMemberRequest(Guid PersonId);

[Route("api/societies")]
// Igual que en PersonsController: la política va por acción, porque leer los integrantes de
// un grupo lo necesita quien captura el reporte dominical, no solo el Pastor.
[Authorize]
public sealed class SocietiesController : ApiController
{
    public SocietiesController(ISender sender) : base(sender) { }

    [HttpPost]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateSocietyCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>Mismas reglas 7.11 y 7.14 que el liderazgo de un ministerio.</summary>
    [HttpPost("{societyId:guid}/leader")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignLeader(
        Guid societyId,
        AssignLeaderRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new AssignSocietyLeaderCommand(societyId, request.PersonId),
            cancellationToken)).ToCreatedResult();

    [HttpDelete("{societyId:guid}/leader")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeLeader(Guid societyId, CancellationToken cancellationToken) =>
        (await Sender.Send(new RevokeSocietyLeaderCommand(societyId), cancellationToken)).ToActionResult();

    /// <summary>
    /// Integrantes activos del grupo. Es lo que precarga la lista de asistencia dominical.
    /// </summary>
    [HttpGet("{societyId:guid}/members")]
    [ProducesResponseType(typeof(SocietyMembersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(
        Guid societyId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new GetSocietyMembersQuery(societyId), cancellationToken)).ToActionResult();

    /// <summary>Agrega a una persona al grupo. Acto organizacional: solo el Pastor.</summary>
    [HttpPost("{societyId:guid}/members")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMember(
        Guid societyId,
        AddSocietyMemberRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new AddSocietyMemberCommand(societyId, request.PersonId), cancellationToken))
        .ToCreatedResult();

    /// <summary>Da de baja la pertenencia. La fila se conserva revocada (regla 7.3).</summary>
    [HttpDelete("members/{societyMembershipId:guid}")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(
        Guid societyMembershipId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new RemoveSocietyMemberCommand(societyMembershipId), cancellationToken))
        .ToActionResult();
}
