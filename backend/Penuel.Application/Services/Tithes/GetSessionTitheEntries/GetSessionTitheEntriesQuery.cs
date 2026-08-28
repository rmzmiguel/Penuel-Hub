using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.Tithes.GetSessionTitheEntries;

public sealed record TitheEntryDetail(
    Guid TitheEntryId,
    Guid PersonId,
    string FirstName,
    string LastName,
    decimal Amount);

public sealed record SessionTitheDetailResponse(
    Guid ServiceSessionId,
    DateOnly SessionDate,
    /// <summary>El total confiable, contado completo.</summary>
    decimal? TotalTithe,
    /// <summary>La suma de lo identificado por persona. Es normal que sea MENOR que el total.</summary>
    decimal IdentifiedTotal,
    /// <summary>
    /// TotalTithe menos IdentifiedTotal: lo que se dio sin anotar el nombre en el sobre.
    /// Es un dato informativo, NO una discrepancia a corregir (regla 7.5).
    /// </summary>
    decimal? UnidentifiedAmount,
    IReadOnlyCollection<TitheEntryDetail> Entries);

/// <summary>
/// Diezmo desglosado por persona de una sesión. Información más sensible que el total:
/// solo Pastor y Tesorero (Sección 8.3).
/// </summary>
public sealed record GetSessionTitheEntriesQuery(Guid ServiceSessionId)
    : IRequest<Result<SessionTitheDetailResponse>>, IRequireTreasuryAccess;

public sealed class GetSessionTitheEntriesQueryValidator
    : AbstractValidator<GetSessionTitheEntriesQuery>
{
    public GetSessionTitheEntriesQueryValidator()
    {
        RuleFor(q => q.ServiceSessionId).NotEmpty()
            .WithMessage("El identificador de la sesión es obligatorio.");
    }
}

public sealed class GetSessionTitheEntriesQueryHandler
    : IRequestHandler<GetSessionTitheEntriesQuery, Result<SessionTitheDetailResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetSessionTitheEntriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SessionTitheDetailResponse>> Handle(
        GetSessionTitheEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _db.ServiceSessions
            .Where(s => s.Id == request.ServiceSessionId)
            .Select(s => new { s.Id, s.SessionDate, s.TotalTithe })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return Result.Failure<SessionTitheDetailResponse>(
                ServiceErrors.Session.NotFound(request.ServiceSessionId));
        }

        var entries = await _db.TitheEntries
            .Where(t => t.ServiceSessionId == request.ServiceSessionId)
            .OrderBy(t => t.Person.LastName)
            .ThenBy(t => t.Person.FirstName)
            .Select(t => new TitheEntryDetail(
                t.Id, t.PersonId, t.Person.FirstName, t.Person.LastName, t.Amount))
            .ToListAsync(cancellationToken);

        var identified = entries.Sum(e => e.Amount);

        return Result.Success(new SessionTitheDetailResponse(
            session.Id,
            session.SessionDate,
            session.TotalTithe,
            identified,
            session.TotalTithe.HasValue ? session.TotalTithe.Value - identified : null,
            entries));
    }
}
