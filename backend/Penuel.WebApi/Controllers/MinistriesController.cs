using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Ministries.AssignMinistryLeader;
using Penuel.Application.Ministries.CreateMinistry;
using Penuel.Application.Ministries.RevokeMinistryLeader;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Persona a designar como responsable de un recurso.</summary>
public sealed record AssignLeaderRequest(Guid PersonId);

[Route("api/ministries")]
[Authorize(Policy = Policies.RequirePastor)]
public sealed class MinistriesController : ApiController
{
    public MinistriesController(ISender sender) : base(sender) { }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateMinistryCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Asigna el líder. Falla con 409 si ya hay uno activo: reasignar es revocar y luego
    /// asignar, nunca un reemplazo silencioso (reglas 7.11 y 7.14).
    /// </summary>
    [HttpPost("{ministryId:guid}/leader")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignLeader(
        Guid ministryId,
        AssignLeaderRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new AssignMinistryLeaderCommand(ministryId, request.PersonId),
            cancellationToken)).ToCreatedResult();

    [HttpDelete("{ministryId:guid}/leader")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeLeader(Guid ministryId, CancellationToken cancellationToken) =>
        (await Sender.Send(new RevokeMinistryLeaderCommand(ministryId), cancellationToken)).ToActionResult();
}
