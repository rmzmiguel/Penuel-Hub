using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penuel.Application.FamilyGroups.AddExistingPersonToGroup;
using Penuel.Application.FamilyGroups.CorrectFamilyGroupReport;
using Penuel.Application.FamilyGroups.CreateFamilyGroup;
using Penuel.Application.FamilyGroups.GetAllFamilyGroups;
using Penuel.Application.FamilyGroups.GetFamilyGroupDetail;
using Penuel.Application.FamilyGroups.GetMyFamilyGroups;
using Penuel.Application.FamilyGroups.ReassignFamilyGroupHostOrLeader;
using Penuel.Application.FamilyGroups.RegisterAndAddGroupMember;
using Penuel.Application.FamilyGroups.RemoveGroupMember;
using Penuel.Application.FamilyGroups.SearchAvailablePersons;
using Penuel.Application.FamilyGroups.SetFamilyGroupStatus;
using Penuel.Application.FamilyGroups.SubmitFamilyGroupReport;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Extensions;

namespace Penuel.WebApi.Controllers;

/// <summary>Sumar al grupo a alguien que ya está en el directorio.</summary>
public sealed record AddGroupMemberRequest(Guid PersonId);

/// <summary>Alta de una persona nueva directamente en el grupo. Sin nada de membresía (7.4).</summary>
public sealed record RegisterGroupMemberRequest(string FirstName, string LastName, string? PhoneNumber);

/// <summary>Reporte semanal: fecha real, ofrenda y una casilla por persona.</summary>
public sealed record SubmitFamilyGroupReportRequest(
    DateOnly MeetingDate,
    decimal TotalOffering,
    IReadOnlyCollection<FamilyGroupAttendanceInput> Attendances);

/// <summary>Corrección de un reporte. La lista es opcional: nula = solo la ofrenda.</summary>
public sealed record CorrectFamilyGroupReportRequest(
    decimal TotalOffering,
    IReadOnlyCollection<FamilyGroupAttendanceInput>? Attendances);

public sealed record ReassignFamilyGroupRequest(Guid HostPersonId, Guid? LeaderPersonId);

public sealed record SetFamilyGroupStatusRequest(bool IsActive);

/// <summary>
/// Grupos Familiares: las casas donde se reúne la iglesia entre semana.
/// </summary>
/// <remarks>
/// La política va POR ACCIÓN y nunca en la clase. Es la diferencia entera de esta rama: los
/// actos organizacionales (crear, reasignar, ver todo) son del Pastor, pero los operativos
/// —agregar gente, registrar a alguien nuevo, levantar el reporte— los ejecuta el Anfitrión o
/// el Encargado, que puede no tener ningún rol ni cargo. Esos llevan <c>[Authorize]</c> a
/// secas y la decisión real la toma el handler contra el propio grupo (Sección 8.2), igual que
/// hacen los endpoints de dinero de la rama de Servicios.
///
/// Un <c>[Authorize(Policy = RequirePastor)]</c> a nivel de clase encerraría toda la rama y
/// dejaría fuera precisamente a quien la usa cada jueves.
/// </remarks>
[Route("api/family-groups")]
[Authorize]
public sealed class FamilyGroupsController : ApiController
{
    public FamilyGroupsController(ISender sender) : base(sender) { }

    // ── Actos organizacionales: solo el Pastor (Sección 8.1) ────────────────

    /// <summary>Da de alta una casa como punto de reunión oficial.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateFamilyGroupCommand command,
        CancellationToken cancellationToken) =>
        (await Sender.Send(command, cancellationToken)).ToCreatedResult();

    /// <summary>Todos los grupos, activos e inactivos.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(IReadOnlyCollection<FamilyGroupSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetAllFamilyGroupsQuery(), cancellationToken)).ToActionResult();

    /// <summary>Detalle completo de un grupo, incluida su dirección.</summary>
    [HttpGet("{familyGroupId:guid}")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(typeof(FamilyGroupDetail), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(
        Guid familyGroupId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new GetFamilyGroupDetailQuery(familyGroupId), cancellationToken))
            .ToActionResult();

    /// <summary>Cambia quién es Anfitrión y quién Encargado.</summary>
    [HttpPut("{familyGroupId:guid}/assignment")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reassign(
        Guid familyGroupId,
        ReassignFamilyGroupRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new ReassignFamilyGroupHostOrLeaderCommand(
                familyGroupId, request.HostPersonId, request.LeaderPersonId),
            cancellationToken)).ToActionResult();

    /// <summary>Detiene o reanuda el grupo. Nunca lo borra (regla 7.6).</summary>
    [HttpPut("{familyGroupId:guid}/status")]
    [Authorize(Policy = Policies.RequirePastor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(
        Guid familyGroupId,
        SetFamilyGroupStatusRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new SetFamilyGroupStatusCommand(familyGroupId, request.IsActive),
            cancellationToken)).ToActionResult();

    // ── Lo que ve el Anfitrión al abrir la aplicación (Sección 8.4) ─────────

    /// <summary>
    /// Los grupos que lleva la persona autenticada. Basta con estar autenticado: solo
    /// devuelve lo suyo.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MyFamilyGroup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        (await Sender.Send(new GetMyFamilyGroupsQuery(), cancellationToken)).ToActionResult();

    // ── Actos operativos del propio grupo (Sección 8.2) ─────────────────────
    //  Sin política de controlador: el permiso se resuelve dentro del handler
    //  comparando a quien llama con el Anfitrión y el Encargado DE ESE grupo.

    /// <summary>Personas del directorio, marcando cuáles ya están en algún grupo (regla 7.5).</summary>
    [HttpGet("{familyGroupId:guid}/available-persons")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailablePerson>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAvailable(
        Guid familyGroupId,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new SearchAvailablePersonsQuery(familyGroupId, search), cancellationToken))
            .ToActionResult();

    /// <summary>Suma al grupo a alguien que ya está en el directorio.</summary>
    [HttpPost("{familyGroupId:guid}/members")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMember(
        Guid familyGroupId,
        AddGroupMemberRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new AddExistingPersonToGroupCommand(familyGroupId, request.PersonId),
            cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Registra a alguien nuevo y lo suma al grupo. NO decide su membresía oficial: el
    /// comando no tiene ni siquiera un parámetro para eso (regla 7.4).
    /// </summary>
    [HttpPost("{familyGroupId:guid}/members/register")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterMember(
        Guid familyGroupId,
        RegisterGroupMemberRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new RegisterAndAddGroupMemberCommand(
                familyGroupId, request.FirstName, request.LastName, request.PhoneNumber),
            cancellationToken)).ToCreatedResult();

    /// <summary>Quita a alguien del grupo. Cierra la fila, no la borra (regla 7.6).</summary>
    [HttpDelete("{familyGroupId:guid}/members/{personId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(
        Guid familyGroupId,
        Guid personId,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new RemoveGroupMemberCommand(familyGroupId, personId), cancellationToken))
            .ToActionResult();

    /// <summary>Levanta el reporte de una reunión.</summary>
    [HttpPost("{familyGroupId:guid}/meetings")]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitReport(
        Guid familyGroupId,
        SubmitFamilyGroupReportRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new SubmitFamilyGroupReportCommand(
                familyGroupId, request.MeetingDate, request.TotalOffering, request.Attendances),
            cancellationToken)).ToCreatedResult();

    /// <summary>
    /// Corrige un reporte ya levantado. Va por el Id del REPORTE y no del grupo: el handler
    /// resuelve el grupo desde él y comprueba el permiso contra ese grupo.
    /// </summary>
    [HttpPut("meetings/{familyGroupMeetingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CorrectReport(
        Guid familyGroupMeetingId,
        CorrectFamilyGroupReportRequest request,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new CorrectFamilyGroupReportCommand(
                familyGroupMeetingId, request.TotalOffering, request.Attendances),
            cancellationToken)).ToActionResult();
}
