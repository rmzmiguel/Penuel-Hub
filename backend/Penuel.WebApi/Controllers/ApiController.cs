using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Penuel.WebApi.Controllers;

/// <summary>
/// Base de todos los controladores. Deliberadamente mínima: los controladores de este
/// proyecto no contienen lógica, solo enrutan a MediatR y traducen el <c>Result</c>.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected ApiController(ISender sender) => Sender = sender;

    protected ISender Sender { get; }
}
