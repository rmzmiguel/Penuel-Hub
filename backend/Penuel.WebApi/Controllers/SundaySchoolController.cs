using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.Services.Sessions.CorrectServiceAttendance;
using Penuel.Application.Services.SundaySchool.AssignSundaySchoolTeacher;
using Penuel.Application.Services.SundaySchool.GetMySundaySchoolCaptureContext;
using Penuel.Application.Services.SundaySchool.GetReadingHabitsReport;
using Penuel.Application.Services.SundaySchool.RevokeSundaySchoolTeacher;
using Penuel.Application.Services.SundaySchool.SubmitSundaySchoolReport;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Corrección de una asistencia ya capturada.</summary>
public sealed record CorrectAttendanceRequest(
    bool WasPresent,
    bool? WasPunctual,
    bool? BroughtBible,
    int? ChaptersRead);

/// <summary>
/// Escuela Dominical: captura, maestros y reportes.
/// </summary>
/// <remarks>
/// La política exige el rol <c>SundaySchoolRecorder</c> (o Pastor). A diferencia del acceso de
/// tesorería, este SÍ se puede expresar como política de controlador, porque es un rol y los
/// roles viajan en el token. Asignar y revocar maestros son actos organizacionales y quedan
/// reservados al Pastor.
/// </remarks>
[Route("api/sunday-school")]
[Authorize(Policy = Policies.RequireSundaySchoolRecorder)]
public sealed class SundaySchoolController : ApiController
{
    public SundaySchoolController(ISender sender) : base(sender) { }

    /// <summary>
    /// Qué debe preguntar el frontend antes de capturar: si esta persona tiene un grupo fijo,
    /// varios, o ninguno (Sección 8.2).
    /// </summary>
    [HttpGet("capture-context")]
    [ProducesResponseType(typeof(MySundaySchoolCaptureContextResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaptureContext(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetMySundaySchoolCaptureContextQuery(), cancellationToken))
        .ToActionResult();

    /// <summary>
    /// Levanta el reporte completo de un grupo: sesión, ofrenda y todas las asistencias,
    /// en una sola transacción.
    /// </summary>
    [HttpPost("reports")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitReport(
        SubmitSundaySchoolReportCommand command,
        CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    [HttpPut("attendances/{serviceAttendanceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CorrectAttendance(
        Guid serviceAttendanceId,
        CorrectAttendanceRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new CorrectServiceAttendanceCommand(
                serviceAttendanceId,
                request.WasPresent,
                request.WasPunctual,
                request.BroughtBible,
                request.ChaptersRead),
            cancellationToken)).ToActionResult();

    /// <summary>
    /// Registra a un maestro. <c>societyId</c> nulo significa sustituto sin grupo fijo,
    /// disponible para cualquier Sociedad. Acto organizacional: solo el Pastor.
    /// </summary>
    [HttpPost("teachers")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignTeacher(
        AssignSundaySchoolTeacherCommand command,
        CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    [HttpDelete("teachers/{assignmentId:guid}")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeTeacher(
        Guid assignmentId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new RevokeSundaySchoolTeacherCommand(assignmentId), cancellationToken))
        .ToActionResult();

    /// <summary>
    /// % que trae la Biblia y promedio de capítulos leídos, por sesión y agregado del periodo.
    /// Es el resultado concreto de capturar el detalle granular de la hoja física.
    /// </summary>
    [HttpGet("reading-habits")]
    [ProducesResponseType(typeof(ReadingHabitsReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadingHabits(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? societyId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new GetReadingHabitsReportQuery(from, to, societyId), cancellationToken))
        .ToActionResult();
}
