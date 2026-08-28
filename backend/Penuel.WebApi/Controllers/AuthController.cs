using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Auth;
using Penuel.Application.Auth.Login;
using Penuel.Application.Auth.Refresh;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Inicio y renovación de sesión. Ambos abiertos, sin token previo (Sección 8.2).</summary>
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ApiController
{
    public AuthController(ISender sender) : base(sender) { }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginQuery query, CancellationToken cancellationToken) =>
        (await Sender.Send(query, cancellationToken)).ToActionResult();

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToActionResult();
}
