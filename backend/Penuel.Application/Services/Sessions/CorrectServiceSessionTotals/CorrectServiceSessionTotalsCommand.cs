using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Constants;

namespace Penuel.Application.Services.Sessions.CorrectServiceSessionTotals;

/// <summary>
/// Corrige los totales de una sesión ya capturada. Regla 7.1: UPDATE controlado, nunca
/// borrar y recapturar.
/// </summary>
public sealed record CorrectServiceSessionTotalsCommand(
    Guid ServiceSessionId,
    decimal TotalOffering,
    decimal? TotalTithe) : IRequest<Result>, IRequireServiceCaptureAccess;

public sealed class CorrectServiceSessionTotalsCommandValidator
    : AbstractValidator<CorrectServiceSessionTotalsCommand>
{
    public CorrectServiceSessionTotalsCommandValidator()
    {
        RuleFor(c => c.ServiceSessionId).NotEmpty()
            .WithMessage("El identificador de la sesión es obligatorio.");

        RuleFor(c => c.TotalOffering)
            .GreaterThanOrEqualTo(0).WithMessage("La ofrenda no puede ser negativa.");

        RuleFor(c => c.TotalTithe)
            .GreaterThanOrEqualTo(0)
            .When(c => c.TotalTithe.HasValue)
            .WithMessage("El diezmo no puede ser negativo.");
    }
}

public sealed class CorrectServiceSessionTotalsCommandHandler
    : IRequestHandler<CorrectServiceSessionTotalsCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CorrectServiceSessionTotalsCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        CorrectServiceSessionTotalsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure(ApplicationErrors.Auth.NotAuthenticated);
        }

        var session = await _db.ServiceSessions
            .Include(s => s.ServiceType)
            .FirstOrDefaultAsync(s => s.Id == request.ServiceSessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(ServiceErrors.Session.NotFound(request.ServiceSessionId));
        }

        // El behavior ya dejó pasar a quien puede capturar ALGO; aquí se afina según QUÉ
        // sesión es, que solo se sabe habiéndola cargado.
        var permitted = await IsPermittedForAsync(session.ServiceType.RequiresSocietyGrouping,
            actorId, cancellationToken);

        if (!permitted.IsSuccess)
        {
            return permitted;
        }

        // Regla 7.3: sin CollectsTithe, TotalTithe debe quedar nulo.
        if (request.TotalTithe.HasValue && !session.ServiceType.CollectsTithe)
        {
            return Result.Failure(ServiceErrors.ServiceType.DoesNotCollectTithe);
        }

        session.CorrectTotals(request.TotalOffering, request.TotalTithe, actorId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> IsPermittedForAsync(
        bool isSundaySchool,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var scope = await ServiceCapturePermissions.ResolveAsync(
            _db, _currentUser, actorId, cancellationToken);

        if (scope.IsPastor)
        {
            return Result.Success();
        }

        if (isSundaySchool)
        {
            return scope.IsSundaySchoolRecorder
                ? Result.Success()
                : Result.Failure(ApplicationErrors.Auth.InsufficientPermissions(
                    [RoleNames.Pastor, RoleNames.SundaySchoolRecorder], []));
        }

        return scope.IsTreasurer
            ? Result.Success()
            : Result.Failure(ApplicationErrors.Auth.InsufficientPermissions(
                [RoleNames.Pastor], [PositionNames.TesoreroGeneral]));
    }
}
