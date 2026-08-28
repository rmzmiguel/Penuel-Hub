using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Memberships.CreateMembership;
using Penuel.Application.Memberships.SetMembershipStatus;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Dar de baja o restituir la membresía oficial.</summary>
public sealed record SetMembershipStatusRequest(bool IsMember);

[Route("api/memberships")]
[Authorize(Policy = Policies.RequirePastor)]
public sealed class MembershipsController : ApiController
{
    public MembershipsController(ISender sender) : base(sender) { }

    /// <summary>
    /// Convierte a una persona en miembro oficial. Decisión administrativa separada y
    /// posterior a que asista: asistir no hace miembro a nadie (Sección 3.2).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateMembershipCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Da de baja o restituye la membresía. Va por PersonId y no por MembershipId porque la
    /// regla 7.2 garantiza que hay como mucho una por persona: pedir el Id de la membresía
    /// obligaría a buscarlo antes para identificar algo que ya está identificado.
    /// </summary>
    [HttpPut("{personId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(
        Guid personId,
        SetMembershipStatusRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new SetMembershipStatusCommand(personId, request.IsMember),
            cancellationToken)).ToActionResult();
}
