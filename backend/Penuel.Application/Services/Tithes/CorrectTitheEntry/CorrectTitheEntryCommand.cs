using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.Tithes.CorrectTitheEntry;

/// <summary>
/// Corrige el monto de un diezmo ya registrado. Sección 6.4: es un UPDATE controlado y no una
/// fila nueva, porque esto es captura operativa y no una asignación organizacional auditada
/// como las del Core. Aun así queda registrado quién corrigió.
/// </summary>
public sealed record CorrectTitheEntryCommand(
    Guid TitheEntryId,
    decimal Amount) : IRequest<Result>, IRequireTreasuryAccess;

public sealed class CorrectTitheEntryCommandValidator : AbstractValidator<CorrectTitheEntryCommand>
{
    public CorrectTitheEntryCommandValidator()
    {
        RuleFor(c => c.TitheEntryId).NotEmpty()
            .WithMessage("El identificador del registro de diezmo es obligatorio.");

        RuleFor(c => c.Amount)
            .GreaterThan(0).WithMessage("El monto del diezmo debe ser mayor que cero.");
    }
}

public sealed class CorrectTitheEntryCommandHandler
    : IRequestHandler<CorrectTitheEntryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CorrectTitheEntryCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        CorrectTitheEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _db.TitheEntries
            .FirstOrDefaultAsync(t => t.Id == request.TitheEntryId, cancellationToken);

        if (entry is null)
        {
            return Result.Failure(ServiceErrors.Tithe.NotFound(request.TitheEntryId));
        }

        entry.Correct(request.Amount, _currentUser.PersonId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
