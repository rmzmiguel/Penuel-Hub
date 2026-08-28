using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Application.FamilyGroups.SubmitFamilyGroupReport;
using Penuel.Domain.Common;

namespace Penuel.Application.FamilyGroups.CorrectFamilyGroupReport;

/// <summary>
/// Corrige un reporte ya levantado: la ofrenda y, si hace falta, la asistencia.
/// </summary>
/// <remarks>
/// Autorizado igual que levantarlo (Sección 8.2), con el mismo criterio que la rama de
/// Servicios: quien puede capturar puede corregir. Obligar a un Anfitrión a pedir ayuda para
/// arreglar un dígito mal tecleado sería fricción sin motivo.
///
/// Es un UPDATE controlado, nunca borrar y volver a capturar: la fila conserva quién levantó
/// el reporte original además de quién lo corrigió.
///
/// La lista de asistencia es OPCIONAL. Nula significa "solo corrijo la ofrenda" — que es el
/// caso frecuente — y evita que la pantalla tenga que reenviar veinte casillas para cambiar
/// un número.
/// </remarks>
public sealed record CorrectFamilyGroupReportCommand(
    Guid FamilyGroupMeetingId,
    decimal TotalOffering,
    IReadOnlyCollection<FamilyGroupAttendanceInput>? Attendances)
    : IRequest<Result>, IRequireFamilyGroupOwnership;

public sealed class CorrectFamilyGroupReportCommandValidator
    : AbstractValidator<CorrectFamilyGroupReportCommand>
{
    public CorrectFamilyGroupReportCommandValidator()
    {
        RuleFor(c => c.FamilyGroupMeetingId)
            .NotEmpty().WithMessage("El identificador del reporte es obligatorio.");

        RuleFor(c => c.TotalOffering)
            .GreaterThanOrEqualTo(0).WithMessage("La ofrenda no puede ser negativa.");
    }
}

public sealed class CorrectFamilyGroupReportCommandHandler
    : IRequestHandler<CorrectFamilyGroupReportCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CorrectFamilyGroupReportCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        CorrectFamilyGroupReportCommand request,
        CancellationToken cancellationToken)
    {
        var meeting = await _db.FamilyGroupMeetings
            .Include(m => m.Attendances)
            .FirstOrDefaultAsync(m => m.Id == request.FamilyGroupMeetingId, cancellationToken);

        if (meeting is null)
        {
            return Result.Failure(FamilyGroupErrors.Meeting.NotFound(request.FamilyGroupMeetingId));
        }

        // El permiso se resuelve contra el GRUPO del reporte, no contra el reporte: es el
        // grupo el que tiene Anfitrión y Encargado.
        var acceso = await FamilyGroupPermissions.LoadOwnedAsync(
            _db, _currentUser, meeting.FamilyGroupId, cancellationToken);

        if (!acceso.IsSuccess)
        {
            return Result.Failure(acceso.Error!);
        }

        var actorId = _currentUser.PersonId!.Value;
        var now = _clock.UtcNow;

        meeting.CorrectOffering(request.TotalOffering, actorId, now);

        if (request.Attendances is { } entradas)
        {
            if (entradas.Select(a => a.PersonId).Distinct().Count() != entradas.Count)
            {
                return Result.Failure(FamilyGroupErrors.Meeting.DuplicateAttendee);
            }

            var porPersona = meeting.Attendances.ToDictionary(a => a.PersonId);

            foreach (var entrada in entradas)
            {
                // Solo se corrigen marcas que YA existen en este reporte. Añadir a alguien
                // que no estuvo en la lista original no es corregir, es levantar otro reporte
                // con otra composición del grupo — y eso pediría decidir qué pasa con las
                // reuniones anteriores. Fuera del alcance de una corrección.
                if (!porPersona.TryGetValue(entrada.PersonId, out var asistencia))
                {
                    return Result.Failure(FamilyGroupErrors.Meeting.AttendeeNotInGroup);
                }

                asistencia.SetPresence(entrada.WasPresent);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
