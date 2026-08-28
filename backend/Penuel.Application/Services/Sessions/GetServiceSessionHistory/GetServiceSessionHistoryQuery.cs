using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Common;
using Penuel.Application.Services.Abstractions;
using Penuel.Application.Services.Common;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.Sessions.GetServiceSessionHistory;

public sealed record ServiceSessionSummary(
    Guid SessionId,
    DateOnly SessionDate,
    string ServiceTypeName,
    Guid? SocietyId,
    string? SocietyName,
    decimal TotalOffering,
    decimal? TotalTithe,
    string? TeacherName,
    string? PreacherName,
    string RecordedByName,
    int PresentCount);

/// <summary>
/// Historial de sesiones, con filtros opcionales. Sin filtros devuelve todo el historial,
/// ordenado de lo más reciente a lo más antiguo.
/// </summary>
/// <remarks>
/// El alcance depende de quién pregunta (Sección 8.4):
///   - Pastor o Tesorero General  -> ve TODO el historial.
///   - Solo SundaySchoolRecorder  -> ve únicamente lo agrupado por Sociedad, o sea Escuela
///                                    Dominical. Nunca los cultos generales.
/// Es el mismo patrón de "puerta amplia en el behavior, filtro fino en el handler" que usa
/// <c>CorrectServiceSessionTotalsCommand</c>, y por la misma razón: el permiso depende del
/// TIPO de sesión, que no se conoce hasta consultar.
/// Devuelve el TOTAL de diezmo, nunca el desglose por persona — para eso está
/// <c>GetSessionTitheEntriesQuery</c>, que es información más sensible.
/// </remarks>
public sealed record GetServiceSessionHistoryQuery(
    Guid? ServiceTypeId,
    Guid? SocietyId,
    DateOnly? From,
    DateOnly? To) : IRequest<Result<IReadOnlyCollection<ServiceSessionSummary>>>, IRequireServiceCaptureAccess;

public sealed class GetServiceSessionHistoryQueryValidator
    : AbstractValidator<GetServiceSessionHistoryQuery>
{
    public GetServiceSessionHistoryQueryValidator()
    {
        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From!.Value)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("La fecha final no puede ser anterior a la inicial.");
    }
}

public sealed class GetServiceSessionHistoryQueryHandler
    : IRequestHandler<GetServiceSessionHistoryQuery, Result<IReadOnlyCollection<ServiceSessionSummary>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetServiceSessionHistoryQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<ServiceSessionSummary>>> Handle(
        GetServiceSessionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.PersonId is not Guid actorId)
        {
            return Result.Failure<IReadOnlyCollection<ServiceSessionSummary>>(
                ApplicationErrors.Auth.NotAuthenticated);
        }

        var scope = await ServiceCapturePermissions.ResolveAsync(
            _db, _currentUser, actorId, cancellationToken);

        var query = _db.ServiceSessions.AsQueryable();

        // Quien solo tiene el rol de captura de Escuela Dominical no ve los cultos generales:
        // ahí es donde vive el dinero que administra la Tesorería.
        if (scope.IsSundaySchoolOnly)
        {
            query = query.Where(s => s.ServiceType.RequiresSocietyGrouping);
        }

        if (request.ServiceTypeId is Guid typeId)
        {
            query = query.Where(s => s.ServiceTypeId == typeId);
        }

        if (request.SocietyId is Guid societyId)
        {
            query = query.Where(s => s.SocietyId == societyId);
        }

        if (request.From is DateOnly from)
        {
            query = query.Where(s => s.SessionDate >= from);
        }

        if (request.To is DateOnly to)
        {
            query = query.Where(s => s.SessionDate <= to);
        }

        // Se ordena por la COLUMNA antes de proyectar: ordenar por una propiedad del record ya
        // proyectado no es traducible a SQL.
        var rows = await query
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new
            {
                s.Id,
                s.SessionDate,
                ServiceTypeName = s.ServiceType.Name,
                s.SocietyId,
                SocietyName = s.Society != null ? s.Society.Name : null,
                s.TotalOffering,
                s.TotalTithe,
                TeacherFirst = s.Teacher != null ? s.Teacher.FirstName : null,
                TeacherLast = s.Teacher != null ? s.Teacher.LastName : null,
                PreacherFirst = s.Preacher != null ? s.Preacher.FirstName : null,
                PreacherLast = s.Preacher != null ? s.Preacher.LastName : null,
                s.CreatedByPersonId,
                PresentCount = _db.ServiceAttendances
                    .Count(a => a.ServiceSessionId == s.Id && a.WasPresent)
            })
            .ToListAsync(cancellationToken);

        var recorderIds = rows.Select(r => r.CreatedByPersonId).Distinct().ToArray();

        var recorders = await _db.Persons
            .Where(p => recorderIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var summaries = rows
            .Select(r => new ServiceSessionSummary(
                r.Id,
                r.SessionDate,
                r.ServiceTypeName,
                r.SocietyId,
                r.SocietyName,
                r.TotalOffering,
                r.TotalTithe,
                r.TeacherFirst is null ? null : $"{r.TeacherFirst} {r.TeacherLast}",
                r.PreacherFirst is null ? null : $"{r.PreacherFirst} {r.PreacherLast}",
                recorders.TryGetValue(r.CreatedByPersonId, out var name) ? name : "(desconocido)",
                r.PresentCount))
            .ToList();

        return Result.Success<IReadOnlyCollection<ServiceSessionSummary>>(summaries);
    }
}
