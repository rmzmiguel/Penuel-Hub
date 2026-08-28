using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.FamilyGroups.Abstractions;
using Penuel.Application.FamilyGroups.Common;
using Penuel.Domain.Common;
using Penuel.Domain.Enums;

namespace Penuel.Application.FamilyGroups.SearchAvailablePersons;

/// <summary>
/// Una persona del directorio y si se puede sumar a un grupo.
/// </summary>
/// <remarks>
/// <c>IsAvailable</c> en false significa exclusivamente "ya pertenece a algún grupo". NO se
/// dice a cuál, ni se insinúa (regla 7.5): quien lleva una casa no tiene por qué saber quién
/// va a las demás. Devolver el nombre del otro grupo convertiría un buscador de personas en
/// un mapa de la congregación.
/// </remarks>
public sealed record AvailablePerson(Guid PersonId, string FirstName, string LastName, bool IsAvailable);

/// <summary>
/// Busca personas del directorio para sumarlas al grupo (Sección 8.2).
/// </summary>
/// <remarks>
/// Devuelve nombre y apellido, nada más — ni teléfono ni fecha de nacimiento, mismo criterio
/// que <c>IRequireDirectoryAccess</c> del Core: el propósito es poblar un selector, no exponer
/// el padrón.
///
/// Devuelve también a las NO disponibles, marcadas, en vez de esconderlas. Si alguien busca a
/// "Rosa" y no aparece, va a suponer que no está registrada y la va a dar de alta otra vez;
/// verla en gris con "ya pertenece a un grupo" evita ese duplicado.
/// </remarks>
public sealed record SearchAvailablePersonsQuery(Guid FamilyGroupId, string? Search)
    : IRequest<Result<IReadOnlyCollection<AvailablePerson>>>, IRequireFamilyGroupOwnership;

public sealed class SearchAvailablePersonsQueryHandler
    : IRequestHandler<SearchAvailablePersonsQuery, Result<IReadOnlyCollection<AvailablePerson>>>
{
    /// <summary>Tope de resultados: es un selector, no un listado del padrón.</summary>
    private const int Limite = 40;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SearchAvailablePersonsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<AvailablePerson>>> Handle(
        SearchAvailablePersonsQuery request,
        CancellationToken cancellationToken)
    {
        var acceso = await FamilyGroupPermissions.LoadOwnedAsync(
            _db, _currentUser, request.FamilyGroupId, cancellationToken);

        if (!acceso.IsSuccess)
        {
            return Result.Failure<IReadOnlyCollection<AvailablePerson>>(acceso.Error!);
        }

        var ocupadas = _db.GroupMembers
            .Where(m => m.LeftAt == null)
            .Select(m => m.PersonId);

        var query = _db.Persons.Where(p => p.Status == PersonStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var texto = request.Search.Trim().ToLower();
            query = query.Where(p =>
                (p.FirstName + " " + p.LastName).ToLower().Contains(texto));
        }

        var personas = await query
            .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Take(Limite)
            .Select(p => new AvailablePerson(
                p.Id, p.FirstName, p.LastName, !ocupadas.Contains(p.Id)))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<AvailablePerson>>(personas);
    }
}
