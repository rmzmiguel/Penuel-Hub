using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Capabilities.GetExecutiveBody;

public sealed record ExecutiveBodyPositionResponse(Guid PositionId, string Name, DateTimeOffset AssignedAt);

public sealed record ExecutiveBodyMemberResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    IReadOnlyCollection<ExecutiveBodyPositionResponse> Positions);

/// <summary>
/// Devuelve el Cuerpo Ejecutivo vigente.
/// </summary>
/// <remarks>
/// Regla 7.9: el Cuerpo Ejecutivo NUNCA se almacena. Se computa aquí, en el momento, como
/// las personas con una fila ACTIVA en <c>PersonPosition</c> cuyo <c>Position.IsExecutiveBody</c>
/// es true — lo que ya incluye al Pastor, sembrado como Position con ese flag. Cualquier lista
/// guardada aparte se desincronizaría de esta, que es la fuente real de verdad.
/// Una misma persona puede aparecer con varios cargos a la vez (regla 7.13): por eso la
/// respuesta agrupa por persona y lista sus cargos, en vez de repetir la persona.
/// </remarks>
public sealed record GetExecutiveBodyQuery
    : IRequest<Result<IReadOnlyCollection<ExecutiveBodyMemberResponse>>>, IRequirePastor;

public sealed class GetExecutiveBodyQueryHandler
    : IRequestHandler<GetExecutiveBodyQuery, Result<IReadOnlyCollection<ExecutiveBodyMemberResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetExecutiveBodyQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<ExecutiveBodyMemberResponse>>> Handle(
        GetExecutiveBodyQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<IReadOnlyCollection<ExecutiveBodyMemberResponse>>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var churchId = await ChurchScope.ResolveChurchIdAsync(_db, actorId, cancellationToken);

        if (churchId is null)
        {
            return Result.Failure<IReadOnlyCollection<ExecutiveBodyMemberResponse>>(
                ApplicationErrors.Auth.OperatorPersonNotFound);
        }

        var rows = await _db.PersonPositions
            .Where(pp => pp.RevokedAt == null
                         && pp.Position.IsExecutiveBody
                         && pp.Position.ChurchId == churchId.Value)
            .Select(pp => new
            {
                pp.PersonId,
                pp.Person.FirstName,
                pp.Person.LastName,
                pp.PositionId,
                PositionName = pp.Position.Name,
                pp.AssignedAt
            })
            .ToListAsync(cancellationToken);

        var members = rows
            .GroupBy(r => new { r.PersonId, r.FirstName, r.LastName })
            .Select(g => new ExecutiveBodyMemberResponse(
                g.Key.PersonId,
                g.Key.FirstName,
                g.Key.LastName,
                g.Select(r => new ExecutiveBodyPositionResponse(r.PositionId, r.PositionName, r.AssignedAt))
                 .OrderBy(p => p.Name)
                 .ToList()))
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToList();

        return Result.Success<IReadOnlyCollection<ExecutiveBodyMemberResponse>>(members);
    }
}
