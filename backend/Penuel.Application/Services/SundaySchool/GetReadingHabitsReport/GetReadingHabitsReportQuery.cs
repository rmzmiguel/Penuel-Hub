using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Penuel.Application.Abstractions;
using Penuel.Application.Services.Abstractions;
using Penuel.Domain.Common;

namespace Penuel.Application.Services.SundaySchool.GetReadingHabitsReport;

/// <summary>Una sesión del periodo, con sus números.</summary>
public sealed record ReadingHabitsEntry(
    DateOnly SessionDate,
    Guid? SocietyId,
    string? SocietyName,
    int PresentCount,
    int BroughtBibleCount,
    decimal BiblePercentage,
    int TotalChaptersRead,
    decimal AverageChaptersPerPerson);

public sealed record ReadingHabitsReportResponse(
    DateOnly From,
    DateOnly To,
    Guid? SocietyId,
    int SessionCount,
    int TotalPresent,
    int TotalBroughtBible,
    /// <summary>% de los presentes que trajo Biblia en todo el periodo.</summary>
    decimal BiblePercentage,
    /// <summary>Promedio de capítulos leídos por persona presente en todo el periodo.</summary>
    decimal AverageChaptersPerPerson,
    IReadOnlyCollection<ReadingHabitsEntry> Sessions);

/// <summary>
/// El resultado concreto de capturar el detalle granular: % que trae la Biblia y promedio de
/// capítulos leídos, por sesión y agregado del periodo.
/// </summary>
/// <remarks>
/// Son exactamente las métricas que la iglesia ya calcula a mano desde las hojas físicas
/// (Core, Sección 4.6). Solo cuenta a quienes estuvieron PRESENTES: incluir ausentes hundiría
/// los promedios y haría el número inservible.
/// La tendencia se lee de la serie por sesión, que va de la más antigua a la más reciente.
/// </remarks>
public sealed record GetReadingHabitsReportQuery(
    DateOnly From,
    DateOnly To,
    Guid? SocietyId) : IRequest<Result<ReadingHabitsReportResponse>>, IRequireSundaySchoolRecorder;

public sealed class GetReadingHabitsReportQueryValidator
    : AbstractValidator<GetReadingHabitsReportQuery>
{
    public GetReadingHabitsReportQueryValidator()
    {
        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From)
            .WithMessage("La fecha final no puede ser anterior a la inicial.");
    }
}

public sealed class GetReadingHabitsReportQueryHandler
    : IRequestHandler<GetReadingHabitsReportQuery, Result<ReadingHabitsReportResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetReadingHabitsReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ReadingHabitsReportResponse>> Handle(
        GetReadingHabitsReportQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = _db.ServiceSessions
            .Where(s => s.ServiceType.RequiresSocietyGrouping
                        && s.SessionDate >= request.From
                        && s.SessionDate <= request.To);

        if (request.SocietyId is Guid societyId)
        {
            sessions = sessions.Where(s => s.SocietyId == societyId);
        }

        // Solo los PRESENTES: contar ausentes hundiría los promedios sin significar nada.
        var rows = await sessions
            .OrderBy(s => s.SessionDate)
            .Select(s => new
            {
                s.Id,
                s.SessionDate,
                s.SocietyId,
                SocietyName = s.Society != null ? s.Society.Name : null,
                Present = _db.ServiceAttendances
                    .Count(a => a.ServiceSessionId == s.Id && a.WasPresent),
                Bibles = _db.ServiceAttendances
                    .Count(a => a.ServiceSessionId == s.Id && a.WasPresent && a.BroughtBible == true),
                Chapters = _db.ServiceAttendances
                    .Where(a => a.ServiceSessionId == s.Id && a.WasPresent)
                    .Sum(a => a.ChaptersRead ?? 0)
            })
            .ToListAsync(cancellationToken);

        static decimal Percentage(int part, int whole) =>
            whole == 0 ? 0m : Math.Round(part * 100m / whole, 1);

        static decimal Average(int total, int whole) =>
            whole == 0 ? 0m : Math.Round(total / (decimal)whole, 2);

        var entries = rows
            .Select(r => new ReadingHabitsEntry(
                r.SessionDate,
                r.SocietyId,
                r.SocietyName,
                r.Present,
                r.Bibles,
                Percentage(r.Bibles, r.Present),
                r.Chapters,
                Average(r.Chapters, r.Present)))
            .ToList();

        var totalPresent = rows.Sum(r => r.Present);
        var totalBibles = rows.Sum(r => r.Bibles);
        var totalChapters = rows.Sum(r => r.Chapters);

        return Result.Success(new ReadingHabitsReportResponse(
            request.From,
            request.To,
            request.SocietyId,
            rows.Count,
            totalPresent,
            totalBibles,
            Percentage(totalBibles, totalPresent),
            Average(totalChapters, totalPresent),
            entries));
    }
}
