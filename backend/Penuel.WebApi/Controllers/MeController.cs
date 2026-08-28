using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Capabilities.GetMyCapabilities;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>
/// Lo que la persona autenticada puede hacer hoy. Basta estar autenticado, sin rol
/// específico (Sección 8.2).
/// </summary>
[Route("api/me")]
[Authorize]
public sealed class MeController : ApiController
{
    public MeController(ISender sender) : base(sender) { }

    /// <summary>
    /// Sección 8.4: el frontend arma su navegación con esta respuesta y NUNCA con una lista
    /// fija programada de antemano, porque en esta iglesia los liderazgos y cargos rotan.
    /// </summary>
    [HttpGet("capabilities")]
    [ProducesResponseType(typeof(MyCapabilitiesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilities(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetMyCapabilitiesQuery(), cancellationToken)).ToActionResult();
}
