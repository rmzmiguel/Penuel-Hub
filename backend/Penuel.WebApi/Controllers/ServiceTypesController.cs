using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Services.ServiceTypes.CreateServiceType;
using Penuel.Application.Services.ServiceTypes.GetServiceTypes;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>
/// Catálogo de tipos de servicio. Los cuatro actuales se siembran con la migración; esto
/// permite agregar uno nuevo sin tocar código.
/// </summary>
[Route("api/service-types")]
[Authorize]
public sealed class ServiceTypesController : ApiController
{
    public ServiceTypesController(ISender sender) : base(sender) { }

    /// <summary>
    /// Catálogo de tipos de servicio con sus tres banderas. El frontend las necesita para
    /// saber qué formulario mostrar.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceTypeOption>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetServiceTypesQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateServiceTypeCommand command,
        CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();
}
