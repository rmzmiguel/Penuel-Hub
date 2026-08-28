using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Roles.AssignRole;
using Penuel.Application.Roles.RevokeRole;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Otorgamiento y retiro de roles de sistema. Exclusivo del Pastor (regla 7.5).</summary>
[Route("api/roles")]
[Authorize(Policy = Policies.RequirePastor)]
public sealed class RolesController : ApiController
{
    public RolesController(ISender sender) : base(sender) { }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign(AssignRoleCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Retira el rol Y cierra todas las sesiones vivas de esa cuenta, para que la revocación
    /// surta efecto de inmediato y no al expirar el token (Sección 8.1).
    /// </summary>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(RevokeRoleCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToActionResult();
}
