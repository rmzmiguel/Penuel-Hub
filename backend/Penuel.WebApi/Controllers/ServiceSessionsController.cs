using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Services.Sessions.CorrectServiceSessionTotals;
using Penuel.Application.Services.Sessions.GetServiceSessionHistory;
using Penuel.Application.Services.Sessions.SubmitGeneralServiceReport;
using Penuel.Application.Services.Tithes.CorrectTitheEntry;
using Penuel.Application.Services.Tithes.GetSessionTitheEntries;
using Penuel.Application.Services.Tithes.RecordTitheEntry;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Corrección de los totales de una sesión ya capturada.</summary>
public sealed record CorrectSessionTotalsRequest(decimal TotalOffering, decimal? TotalTithe);

/// <summary>Diezmo identificado de una persona.</summary>
public sealed record RecordTitheRequest(Guid PersonId, decimal Amount);

/// <summary>Nuevo monto de un diezmo ya registrado.</summary>
public sealed record CorrectTitheRequest(decimal Amount);

/// <summary>
/// Cultos General, de Oración y de Jóvenes, y todo lo que toca dinero.
/// </summary>
/// <remarks>
/// Lleva <c>[Authorize]</c> a secas y no una política de rol: el acceso real es "Pastor o
/// cargo Tesorero General" (Sección 8.3), y un cargo no viaja en el JWT. Quien decide es
/// <c>AuthorizationBehavior</c> en Penuel.Application, que sí puede consultarlo contra la base.
/// </remarks>
[Route("api/service-sessions")]
[Authorize]
public sealed class ServiceSessionsController : ApiController
{
    public ServiceSessionsController(ISender sender) : base(sender) { }

    /// <summary>Levanta el reporte de un Culto General, de Oración o de Jóvenes.</summary>
    [HttpPost("general")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitGeneralReport(
        SubmitGeneralServiceReportCommand command,
        CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>Corrige los totales. Regla 7.1: se corrige, nunca se borra y recaptura.</summary>
    [HttpPut("{serviceSessionId:guid}/totals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CorrectTotals(
        Guid serviceSessionId,
        CorrectSessionTotalsRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new CorrectServiceSessionTotalsCommand(
                serviceSessionId, request.TotalOffering, request.TotalTithe),
            cancellationToken)).ToActionResult();

    /// <summary>Historial de sesiones. Todos los filtros son opcionales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceSessionSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? serviceTypeId,
        [FromQuery] Guid? societyId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new GetServiceSessionHistoryQuery(serviceTypeId, societyId, from, to),
            cancellationToken)).ToActionResult();

    /// <summary>Registra el diezmo identificado de una persona en esta sesión.</summary>
    [HttpPost("{serviceSessionId:guid}/tithes")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordTithe(
        Guid serviceSessionId,
        RecordTitheRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new RecordTitheEntryCommand(serviceSessionId, request.PersonId, request.Amount),
            cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Diezmo desglosado por persona. Información más sensible que el total: solo Pastor y
    /// Tesorero (Sección 8.3). Que lo identificado NO cuadre con el total es lo normal,
    /// no una discrepancia a corregir (regla 7.5).
    /// </summary>
    [HttpGet("{serviceSessionId:guid}/tithes")]
    [ProducesResponseType(typeof(SessionTitheDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTithes(
        Guid serviceSessionId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new GetSessionTitheEntriesQuery(serviceSessionId), cancellationToken))
        .ToActionResult();

    [HttpPut("tithes/{titheEntryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CorrectTithe(
        Guid titheEntryId,
        CorrectTitheRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new CorrectTitheEntryCommand(titheEntryId, request.Amount), cancellationToken))
        .ToActionResult();
}
