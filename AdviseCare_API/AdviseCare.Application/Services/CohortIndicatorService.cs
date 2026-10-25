using AdviseCare.Application.Contracts.Requests.CohortIndicator;
using AdviseCare.Application.Contracts.Responses.CohortIndicator;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AdviseCare.Application.Services;

public sealed class CohortIndicatorService(
    IGenericRepository<Studentindicator> studentIndicatorRepository,
    IGenericRepository<Studentprofile> studentProfileRepository,
    IGenericRepository<Programme> programmeRepository,
    IGenericRepository<Academicterm> academicTermRepository,
    IGenericRepository<Faculty> facultyRepository,
    IGenericRepository<Consentpurpose> consentPurposeRepository,
    IAccessGuardService accessGuardService,
    IAuthenticatedEnvelopeEncryptionService encryptionService,
    IActivityLogService activityLogService) : ICohortIndicatorService
{
    private const string CohortAnalyticsPurposeName = "Cohort Analytics";
    private const int MinimumStudentsPerReleasedCell = 10;
    private readonly IGenericRepository<Studentindicator> _studentIndicatorRepository = studentIndicatorRepository;
    private readonly IGenericRepository<Studentprofile> _studentProfileRepository = studentProfileRepository;
    private readonly IGenericRepository<Programme> _programmeRepository = programmeRepository;
    private readonly IGenericRepository<Academicterm> _academicTermRepository = academicTermRepository;
    private readonly IGenericRepository<Faculty> _facultyRepository = facultyRepository;
    private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository = consentPurposeRepository;
    private readonly IAccessGuardService _accessGuardService = accessGuardService;
    private readonly IAuthenticatedEnvelopeEncryptionService _encryptionService = encryptionService;
    private readonly IActivityLogService _activityLogService = activityLogService;

    public Task<IReadOnlyList<CohortIndicatorSummaryResponse>> GetSummaryAsync(
        Guid accessorId,
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        return BuildSummaryAsync(accessorId, request, cancellationToken);
    }

    public async Task<CohortIndicatorExportFile> ExportCsvAsync(
        Guid accessorId,
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(accessorId, request, cancellationToken);
        var rows = summary.Select(item => (IReadOnlyList<string>)
            new[]
            {
                item.TermId.ToString("D"),
                CsvExportWriter.EscapeTextCell(item.TermName),
                item.IndicatorId.ToString("D"),
                CsvExportWriter.EscapeTextCell(item.IndicatorName),
                item.DataType,
                CsvExportWriter.EscapeTextCell(item.CategoryValue ?? string.Empty),
                item.MeanValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                item.StudentCount.ToString(CultureInfo.InvariantCulture)
            });

        await _activityLogService.LogCurrentAccountAsync(
            "cohortIndicatorExported",
            $"Exported anonymised cohort indicator statistics as CSV with {summary.Count} released cells.");

        var fileName = $"cohort-indicators-{request.TermId?.ToString("N") ?? "all-terms"}-{request.FacultyId?.ToString("N") ?? "all-faculties"}.csv";
        return new CohortIndicatorExportFile(
            CsvExportWriter.WriteRows(
                new[] { "termId", "termName", "indicatorId", "indicatorName", "dataType", "categoryValue", "meanValue", "studentCount" },
                rows),
            "text/csv; charset=utf-8",
            fileName);
    }

    private async Task<IReadOnlyList<CohortIndicatorSummaryResponse>> BuildSummaryAsync(
        Guid accessorId,
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFiltersAsync(request);

        var purpose = await _consentPurposeRepository.FirstOrDefaultAsync(consentPurpose =>
            consentPurpose.PurposeName == CohortAnalyticsPurposeName &&
            consentPurpose.Status == "active")
            ?? throw new ConflictException(
                "The active Cohort Analytics consent purpose is not configured.");

        var latestRows = await GetLatestRowsAsync(request, cancellationToken);
        var currentRows = latestRows
            .Where(row =>
                row.Indicator.Status == "active" &&
                !row.IsMissing &&
                row.ErasedAt is null)
            .ToList();

        if (currentRows.Count == 0)
            return Array.Empty<CohortIndicatorSummaryResponse>();

        var decisions = await _accessGuardService.CheckAccessForStudentsAsync(
            accessorId,
            currentRows.Select(row => row.StudentId).Distinct().ToList(),
            currentRows.Select(row => row.DataAttributeId).Distinct().ToList(),
            purpose.ConsentPurposeId);
        var decisionsByStudentAndAttribute = decisions.ToDictionary(
            decision => (decision.StudentId, decision.DataAttributeId));
        var authorizedRows = new List<Studentindicator>();
        var accessLogEntries = new List<AccessLogBatchEntry>();

        foreach (var row in currentRows)
        {
            if (!decisionsByStudentAndAttribute.TryGetValue((row.StudentId, row.DataAttributeId), out var decision))
            {
                throw new InvalidOperationException(
                    "The access guard did not return a decision for a cohort indicator value.");
            }

            if (!decision.IsAllowed)
                continue;

            if (decision.ConsentPurposeId != purpose.ConsentPurposeId)
            {
                throw new InvalidOperationException(
                    "The access guard allowed a cohort indicator read without the Cohort Analytics purpose.");
            }

            ValidateEncryptedValue(row);
            authorizedRows.Add(row);
            accessLogEntries.Add(new AccessLogBatchEntry(
                row.StudentId,
                purpose.ConsentPurposeId,
                $"Student indicator: {row.Indicator.IndicatorName}"));
        }

        await _accessGuardService.LogAccessBatchAsync(accessorId, accessLogEntries);
        return await AggregateAuthorizedRowsAsync(authorizedRows, cancellationToken);
    }

    private async Task ValidateFiltersAsync(SearchCohortIndicatorSummaryRequest request)
    {
        if (!request.TermId.HasValue && !request.FacultyId.HasValue)
            throw new ValidationException("Choose an academic term, a faculty, or both.");

        if (request.TermId.HasValue &&
            await _academicTermRepository.GetByIdAsync(request.TermId.Value) is null)
        {
            throw new NotFoundException("Academic term not found.");
        }

        if (request.FacultyId.HasValue &&
            await _facultyRepository.GetByIdAsync(request.FacultyId.Value) is null)
        {
            throw new NotFoundException("Faculty not found.");
        }
    }

    private async Task<List<Studentindicator>> GetLatestRowsAsync(
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var query = _studentIndicatorRepository.GetQueryable().AsNoTracking();

        if (request.TermId.HasValue)
            query = query.Where(row => row.TermId == request.TermId.Value);

        if (request.FacultyId.HasValue)
        {
            var studentIdsInFaculty = _studentProfileRepository.GetQueryable()
                .Where(profile => profile.ProgrammeId.HasValue)
                .Join(
                    _programmeRepository.GetQueryable(),
                    profile => profile.ProgrammeId!.Value,
                    programme => programme.ProgrammeId,
                    (profile, programme) => new { profile.StudentId, programme.FacultyId })
                .Where(student => student.FacultyId == request.FacultyId.Value)
                .Select(student => student.StudentId);

            query = query.Where(row => studentIdsInFaculty.Contains(row.StudentId));
        }

        var latestRowIds = query
            .GroupBy(row => new { row.StudentId, row.IndicatorId, row.TermId })
            .Select(group => group
                .OrderByDescending(row => row.TransactionFrom)
                .ThenByDescending(row => row.ValidFrom)
                .ThenByDescending(row => row.StudentIndicatorId)
                .Select(row => row.StudentIndicatorId)
                .First());

        return await _studentIndicatorRepository.GetQueryable()
            .AsNoTracking()
            .Where(row => latestRowIds.Contains(row.StudentIndicatorId))
            .Include(row => row.Indicator)
            .Include(row => row.Term)
            .Include(row => row.AttributeKey)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CohortIndicatorSummaryResponse>> AggregateAuthorizedRowsAsync(
        IReadOnlyList<Studentindicator> rows,
        CancellationToken cancellationToken)
    {
        var response = new List<CohortIndicatorSummaryResponse>();

        foreach (var indicatorGroup in rows.GroupBy(row => new { row.IndicatorId, row.TermId }))
        {
            var firstRow = indicatorGroup.First();
            var studentsInIndicator = new HashSet<Guid>();

            if (firstRow.Indicator.DataType == "numeric")
            {
                decimal sum = 0;

                foreach (var row in indicatorGroup)
                {
                    EnsureOneCurrentValuePerStudent(studentsInIndicator, row.StudentId);
                    var plaintext = await DecryptValueAsync(row, cancellationToken);
                    if (!decimal.TryParse(
                            plaintext,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var numericValue))
                    {
                        throw new InvalidDataException(
                            $"The encrypted value for indicator '{firstRow.Indicator.IndicatorName}' is not numeric.");
                    }

                    sum = checked(sum + numericValue);
                }

                if (studentsInIndicator.Count >= MinimumStudentsPerReleasedCell)
                {
                    response.Add(new CohortIndicatorSummaryResponse(
                        firstRow.IndicatorId,
                        firstRow.Indicator.IndicatorName,
                        firstRow.Indicator.DataType,
                        firstRow.TermId,
                        firstRow.Term.TermName,
                        null,
                        sum / studentsInIndicator.Count,
                        studentsInIndicator.Count));
                }

                continue;
            }

            if (firstRow.Indicator.DataType is not ("categorical" or "boolean"))
            {
                throw new InvalidDataException(
                    $"Indicator '{firstRow.Indicator.IndicatorName}' has an unsupported cohort data type.");
            }

            var studentsByCategory = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);
            foreach (var row in indicatorGroup)
            {
                EnsureOneCurrentValuePerStudent(studentsInIndicator, row.StudentId);
                var plaintext = await DecryptValueAsync(row, cancellationToken);
                var categoryValue = firstRow.Indicator.DataType == "boolean"
                    ? ParseBooleanCategory(plaintext, firstRow.Indicator.IndicatorName)
                    : plaintext.Trim();

                if (string.IsNullOrWhiteSpace(categoryValue))
                {
                    throw new InvalidDataException(
                        $"The encrypted value for indicator '{firstRow.Indicator.IndicatorName}' is empty.");
                }

                if (!studentsByCategory.TryGetValue(categoryValue, out var studentIds))
                {
                    studentIds = new HashSet<Guid>();
                    studentsByCategory.Add(categoryValue, studentIds);
                }

                studentIds.Add(row.StudentId);
            }

            foreach (var category in studentsByCategory.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                if (category.Value.Count < MinimumStudentsPerReleasedCell)
                    continue;

                response.Add(new CohortIndicatorSummaryResponse(
                    firstRow.IndicatorId,
                    firstRow.Indicator.IndicatorName,
                    firstRow.Indicator.DataType,
                    firstRow.TermId,
                    firstRow.Term.TermName,
                    category.Key,
                    null,
                    category.Value.Count));
            }
        }

        return response
            .OrderBy(item => item.TermName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.IndicatorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.CategoryValue, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<string> DecryptValueAsync(
        Studentindicator row,
        CancellationToken cancellationToken)
    {
        return await _encryptionService.DecryptAsync(
            row.AttributeKey,
            row.StudentIndicatorId,
            "indicatorValue",
            row.IndicatorValue,
            row.ValueHmac,
            cancellationToken);
    }

    private static void ValidateEncryptedValue(Studentindicator row)
    {
        if (row.AttributeKey is null ||
            row.AttributeKey.AttributeKeyId != row.AttributeKeyId ||
            row.AttributeKey.StudentId != row.StudentId ||
            row.AttributeKey.DataAttributeId != row.DataAttributeId ||
            row.AttributeKey.Status != "active" ||
            row.IndicatorValue is null ||
            row.ValueHmac is null)
        {
            throw new InvalidDataException(
                "A cohort indicator payload is not bound to an active student-attribute key.");
        }
    }

    private static void EnsureOneCurrentValuePerStudent(HashSet<Guid> studentIds, Guid studentId)
    {
        if (!studentIds.Add(studentId))
        {
            throw new InvalidOperationException(
                "More than one current value was found for a student, indicator, and term.");
        }
    }

    private static string ParseBooleanCategory(string value, string indicatorName)
    {
        if (!bool.TryParse(value, out var booleanValue))
        {
            throw new InvalidDataException(
                $"The encrypted value for indicator '{indicatorName}' is not boolean.");
        }

        return booleanValue ? "true" : "false";
    }
}
