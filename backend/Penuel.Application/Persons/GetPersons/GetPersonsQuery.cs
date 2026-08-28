using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.Persons.GetPersons;

/// <summary>Persona vista desde un selector: lo mínimo para identificarla y nada más.</summary>
public sealed record PersonOption(Guid Id, string FirstName, string LastName);

/// <summary>
/// Directorio de personas activas, para poblar los selectores de captura.
/// </summary>
/// <remarks>
/// Devuelve deliberadamente solo el nombre: teléfono y fecha de nacimiento no hacen falta para
/// elegir a alguien de una lista, y no exponerlos es gratis.
/// </remarks>
public sealed record GetPersonsQuery(string? Search)
    : IRequest<Result<IReadOnlyCollection<PersonOption>>>, IRequireDirectoryAccess;

public sealed class GetPersonsQueryHandler
    : IRequestHandler<GetPersonsQuery, Result<IReadOnlyCollection<PersonOption>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetPersonsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<PersonOption>>> Handle(
        GetPersonsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<IReadOnlyCollection<PersonOption>>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, actorId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<IReadOnlyCollection<PersonOption>>(
                ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var query = _db.Persons
            .Where(p => p.ChurchId == churchId.Value && p.Status == PersonStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) || p.LastName.ToLower().Contains(term));
        }

        var people = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PersonOption(p.Id, p.FirstName, p.LastName))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<PersonOption>>(people);
    }
}
