using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Capabilities.GetExecutiveBody;
using Penuel.Application.Positions.AssignPosition;
using Penuel.Application.Positions.CreatePosition;
using Penuel.Application.Positions.RevokePosition;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Persona a nombrar titular de un cargo.</summary>
public sealed record AssignPositionHolderRequest(Guid PersonId);

[Route("api/positions")]
[Authorize(Policy = Policies.RequirePastor)]
public sealed class PositionsController : ApiController
{
    public PositionsController(ISender sender) : base(sender) { }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreatePositionCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Nombra un titular. A diferencia de los liderazgos, un cargo admite VARIOS titulares
    /// activos a la vez (Sección 6.13): esto no falla porque el cargo ya esté ocupado,
    /// solo si esta misma persona ya lo ostenta.
    /// </summary>
    [HttpPost("{positionId:guid}/holders")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignHolder(
        Guid positionId,
        AssignPositionHolderRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new AssignPositionCommand(positionId, request.PersonId),
            cancellationToken)).ToCreatedResult();

    /// <summary>Requiere ambos identificadores, porque el cargo puede tener varios titulares.</summary>
    [HttpDelete("{positionId:guid}/holders/{personId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeHolder(
        Guid positionId,
        Guid personId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new RevokePositionCommand(positionId, personId), cancellationToken)).ToActionResult();

    /// <summary>
    /// Cuerpo Ejecutivo vigente. NO se lee de ninguna tabla: se computa en el momento a partir
    /// de los cargos activos con IsExecutiveBody = true (regla 7.9).
    /// </summary>
    [HttpGet("executive-body")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ExecutiveBodyMemberResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutiveBody(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetExecutiveBodyQuery(), cancellationToken)).ToActionResult();
}
