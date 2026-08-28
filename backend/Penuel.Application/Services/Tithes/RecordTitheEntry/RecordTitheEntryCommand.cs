using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Entities.Services;

namespace Penuel.Application.Services.Tithes.RecordTitheEntry;

/// <summary>
/// Registra el diezmo identificado de una persona en una sesión.
/// </summary>
/// <remarks>
/// Regla 7.5: NO se valida ni se fuerza que la suma de estos registros cuadre con
/// <c>ServiceSession.TotalTithe</c>. No todos anotan sus datos en el sobre, y que no coincidan
/// es lo normal, no un error de captura.
/// </remarks>
public sealed record RecordTitheEntryCommand(
    Guid ServiceSessionId,
    Guid PersonId,
    decimal Amount) : IRequest<Result<Guid>>, IRequireTreasuryAccess;

public sealed class RecordTitheEntryCommandValidator : AbstractValidator<RecordTitheEntryCommand>
{
    public RecordTitheEntryCommandValidator()
    {
        RuleFor(c => c.ServiceSessionId).NotEmpty()
            .WithMessage("El identificador de la sesión es obligatorio.");

        RuleFor(c => c.PersonId).NotEmpty()
            .WithMessage("El identificador de la persona es obligatorio.");

        RuleFor(c => c.Amount)
            .GreaterThan(0).WithMessage("El monto del diezmo debe ser mayor que cero.");
    }
}

public sealed class RecordTitheEntryCommandHandler
    : IRequestHandler<RecordTitheEntryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RecordTitheEntryCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        RecordTitheEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<Guid>(ApplicationErrors.Auth.NotAuthenticated);
        }

        var session = await _db.ServiceSessions
            .Include(s => s.ServiceType)
            .FirstOrDefaultAsync(s => s.Id == request.ServiceSessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure<Guid>(ServiceErrors.Session.NotFound(request.ServiceSessionId));
        }

        // Sección 6.4: se valida aquí, no con un CHECK de base de datos, porque la condición
        // atraviesa dos tablas.
        if (!session.ServiceType.CollectsTithe)
        {
            return Result.Failure<Guid>(ServiceErrors.ServiceType.DoesNotCollectTithe);
        }

        if (!await _db.Persons.AnyAsync(p => p.Id == request.PersonId, cancellationToken))
        {
            return Result.Failure<Guid>(ApplicationErrors.Person.NotFound(request.PersonId));
        }

        var alreadyRecorded = await _db.TitheEntries.AnyAsync(
            t => t.ServiceSessionId == request.ServiceSessionId && t.PersonId == request.PersonId,
            cancellationToken);

        if (alreadyRecorded)
        {
            return Result.Failure<Guid>(ServiceErrors.Tithe.AlreadyRecorded);
        }

        var entry = TitheEntry.Record(
            request.ServiceSessionId, request.PersonId, request.Amount, actorId, _clock.UtcNow);

        _db.TitheEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(entry.Id);
    }
}
