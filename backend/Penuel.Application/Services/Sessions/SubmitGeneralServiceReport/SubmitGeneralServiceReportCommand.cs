using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;

namespace Penuel.Application.Services.Sessions.SubmitGeneralServiceReport;

/// <summary>
/// Levanta el reporte de un Culto General, de Oración o de Jóvenes: ofrenda siempre,
/// diezmo total solo donde se recoge, y el predicador como dato descriptivo.
/// </summary>
public sealed record SubmitGeneralServiceReportCommand(
    Guid ServiceTypeId,
    DateOnly SessionDate,
    decimal TotalOffering,
    decimal? TotalTithe,
    Guid? PreacherPersonId) : IRequest<Result<Guid>>, IRequireTreasuryAccess;

public sealed class SubmitGeneralServiceReportCommandValidator
    : AbstractValidator<SubmitGeneralServiceReportCommand>
{
    public SubmitGeneralServiceReportCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.ServiceTypeId).NotEmpty()
            .WithMessage("El tipo de servicio es obligatorio.");

        RuleFor(c => c.SessionDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .WithMessage("No se puede levantar un reporte con fecha futura.");

        RuleFor(c => c.TotalOffering)
            .GreaterThanOrEqualTo(0).WithMessage("La ofrenda no puede ser negativa.");

        RuleFor(c => c.TotalTithe)
            .GreaterThanOrEqualTo(0)
            .When(c => c.TotalTithe.HasValue)
            .WithMessage("El diezmo no puede ser negativo.");
    }
}

public sealed class SubmitGeneralServiceReportCommandHandler
    : IRequestHandler<SubmitGeneralServiceReportCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SubmitGeneralServiceReportCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        SubmitGeneralServiceReportCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var serviceType = await _db.ServiceTypes
            .FirstOrDefaultAsync(t => t.Id == request.ServiceTypeId, cancellationToken);

        if (serviceType is null)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.NotFound(request.ServiceTypeId));
        }

        // Regla 7.4: un tipo agrupado por Sociedad no se reporta por aquí.
        if (serviceType.RequiresSocietyGrouping)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.RequiresSocietyGrouping);
        }

        // Regla 7.3: sin CollectsTithe, TotalTithe debe quedar nulo.
        if (request.TotalTithe.HasValue && !serviceType.CollectsTithe)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.DoesNotCollectTithe);
        }

        if (request.PreacherPersonId is Guid preacherId
            && !await _db.Persons.AnyAsync(p => p.Id == preacherId, cancellationToken))
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(preacherId));
        }

        var alreadyReported = await _db.ServiceSessions.AnyAsync(
            s => s.ServiceTypeId == request.ServiceTypeId
                 && s.SessionDate == request.SessionDate
                 && s.SocietyId == null,
            cancellationToken);

        if (alreadyReported)
        {
            return Result.Failure<Guid>(ServiceErrors.Session.AlreadyExistsForDate);
        }

        var session = ServiceSession.ForGeneralService(
            request.ServiceTypeId,
            request.SessionDate,
            request.TotalOffering,
            request.TotalTithe,
            request.PreacherPersonId,
            actorId,
            _clock.UtcNow);

        _db.ServiceSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(session.Id);
    }
}
