using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.ServiceTypes.GetServiceTypes;

/// <summary>
/// Tipo de servicio con las tres banderas que gobiernan su comportamiento. El frontend las
/// necesita para saber qué formulario mostrar: si pedir Sociedad, si pedir diezmo, y si
/// sugerir o no tomar asistencia.
/// </summary>
public sealed record ServiceTypeOption(
    Guid Id,
    string Name,
    bool RequiresSocietyGrouping,
    bool CollectsTithe,
    bool AttendanceCustomary);

public sealed record GetServiceTypesQuery
    : IRequest<Result<IReadOnlyCollection<ServiceTypeOption>>>, IRequireDirectoryAccess;

public sealed class GetServiceTypesQueryHandler
    : IRequestHandler<GetServiceTypesQuery, Result<IReadOnlyCollection<ServiceTypeOption>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetServiceTypesQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<ServiceTypeOption>>> Handle(
        GetServiceTypesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<IReadOnlyCollection<ServiceTypeOption>>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, actorId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<IReadOnlyCollection<ServiceTypeOption>>(
                ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var types = await _db.ServiceTypes
            .Where(t => t.ChurchId == churchId.Value)
            .OrderBy(t => t.Name)
            .Select(t => new ServiceTypeOption(
                t.Id, t.Name, t.RequiresSocietyGrouping, t.CollectsTithe, t.AttendanceCustomary))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<ServiceTypeOption>>(types);
    }
}
