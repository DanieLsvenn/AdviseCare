using AdviseCare.Application.Contracts.Requests.WelfareStatistics;
using AdviseCare.Application.Contracts.Responses.WelfareStatistics;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace AdviseCare.Application.Services;

public sealed class WelfareStatisticsService(
    IGenericRepository<Case> caseRepository,
    IGenericRepository<Academicterm> academicTermRepository,
    IActivityLogService activityLogService) : IWelfareStatisticsService
{
    private const int MinimumDistinctStudentsPerCategory = 5;
    private readonly IGenericRepository<Case> _caseRepository = caseRepository;
    private readonly IGenericRepository<Academicterm> _academicTermRepository = academicTermRepository;
    private readonly IActivityLogService _activityLogService = activityLogService;

    public async Task<IReadOnlyList<WelfareStatisticsByCategoryResponse>> GetByCategoryAsync(
        SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        var range = await ResolveDateRangeAsync(request);
        return await AggregateByCategoryAsync(range, cancellationToken);
    }

    public async Task<WelfareStatisticsExportFile> ExportCsvAsync(
        SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        var range = await ResolveDateRangeAsync(request);
        var statistics = await AggregateByCategoryAsync(range, cancellationToken);

        await _activityLogService.LogCurrentAccountAsync(
            "welfareStatisticsExported",
            $"Exported anonymised welfare statistics as CSV with {statistics.Count} published categories.");

        var fromDate = range.FromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var toDate = range.ToDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var fileName = $"welfare-statistics-{fromDate}-{toDate}.csv";
        return new WelfareStatisticsExportFile(
            ToCsv(statistics),
            "text/csv; charset=utf-8",
            fileName);
    }

    private async Task<List<WelfareStatisticsByCategoryResponse>> AggregateByCategoryAsync(
        CaseDateRange range,
        CancellationToken cancellationToken)
    {
        return await _caseRepository.GetQueryable()
            .AsNoTracking()
            .Where(caseRecord =>
                caseRecord.OpenedAt >= range.StartInclusive &&
                caseRecord.OpenedAt <= range.EndInclusive)
            .GroupBy(caseRecord => caseRecord.SupportCategory.CategoryName)
            .Select(group => new
            {
                CategoryName = group.Key,
                CaseCount = group.Count(),
                StudentCount = group.Select(caseRecord => caseRecord.StudentId).Distinct().Count()
            })
            .Where(statistic => statistic.StudentCount >= MinimumDistinctStudentsPerCategory)
            .OrderBy(statistic => statistic.CategoryName)
            .Select(statistic => new WelfareStatisticsByCategoryResponse(
                statistic.CategoryName,
                statistic.CaseCount,
                statistic.StudentCount))
            .ToListAsync(cancellationToken);
    }

    private async Task<CaseDateRange> ResolveDateRangeAsync(SearchWelfareStatisticsRequest request)
    {
        if (request.TermId.HasValue)
        {
            if (request.FromDate.HasValue || request.ToDate.HasValue)
            {
                throw new ValidationException("Choose an academic term or a date range, not both.");
            }

            var term = await _academicTermRepository.GetByIdAsync(request.TermId.Value)
                ?? throw new NotFoundException("Academic term not found.");
            var fromDate = DateOnly.FromDateTime(term.StartDate);
            var toDate = DateOnly.FromDateTime(term.EndDate);

            if (toDate < fromDate)
            {
                throw new InvalidOperationException("The selected academic term has an invalid date range.");
            }

            return CreateDateRange(fromDate, toDate);
        }

        if (!request.FromDate.HasValue || !request.ToDate.HasValue)
        {
            throw new ValidationException("Choose an academic term or provide both fromDate and toDate.");
        }

        if (request.ToDate.Value < request.FromDate.Value)
        {
            throw new ValidationException("toDate must be the same as or later than fromDate.");
        }

        return CreateDateRange(request.FromDate.Value, request.ToDate.Value);
    }

    private static CaseDateRange CreateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        return new CaseDateRange(
            fromDate.ToDateTime(TimeOnly.MinValue),
            toDate.ToDateTime(TimeOnly.MaxValue),
            fromDate,
            toDate);
    }

    private static byte[] ToCsv(IReadOnlyList<WelfareStatisticsByCategoryResponse> statistics)
    {
        var builder = new StringBuilder();
        builder.AppendLine("categoryName,caseCount,studentCount");

        foreach (var statistic in statistics)
        {
            builder.Append(EscapeCsv(statistic.CategoryName));
            builder.Append(',');
            builder.Append(statistic.CaseCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(statistic.StudentCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();
    }

    private static string EscapeCsv(string value)
    {
        var firstNonWhitespace = value.FirstOrDefault(character => !char.IsWhiteSpace(character));
        var safeValue = firstNonWhitespace is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record CaseDateRange(
        DateTime StartInclusive,
        DateTime EndInclusive,
        DateOnly FromDate,
        DateOnly ToDate);
}
