using AdviseCare.Application.Contracts.Responses.StudentIndicator;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services;

public sealed class StudentIndicatorProfileService(
    IGenericRepository<Case> caseRepository,
    IGenericRepository<Studentindicator> studentIndicatorRepository,
    IAccessGuardService accessGuardService,
    IAuthenticatedEnvelopeEncryptionService encryptionService) : IStudentIndicatorProfileService
{
    private readonly IGenericRepository<Case> _caseRepository = caseRepository;
    private readonly IGenericRepository<Studentindicator> _studentIndicatorRepository = studentIndicatorRepository;
    private readonly IAccessGuardService _accessGuardService = accessGuardService;
    private readonly IAuthenticatedEnvelopeEncryptionService _encryptionService = encryptionService;

    public async Task<StudentIndicatorProfileResponse> GetOwnProfileAsync(
        Guid studentId,
        Guid? termId,
        CancellationToken cancellationToken)
    {
        return await GetSnapshotAsync(studentId, studentId, null, null, termId, cancellationToken);
    }

    public async Task<StudentIndicatorProfileResponse> GetProfileAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        Guid? termId,
        CancellationToken cancellationToken)
    {
        return await GetSnapshotAsync(accessorId, studentId, caseId, null, termId, cancellationToken);
    }

    public async Task<StudentIndicatorProfileResponse> GetAsOfAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        DateOnly date,
        Guid? termId,
        CancellationToken cancellationToken)
    {
        return await GetSnapshotAsync(accessorId, studentId, caseId, date, termId, cancellationToken);
    }

    public async Task<StudentIndicatorComparisonResponse> CompareAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        DateOnly dateA,
        DateOnly dateB,
        Guid? termId,
        CancellationToken cancellationToken)
    {
        var snapshotA = await GetSnapshotAsync(accessorId, studentId, caseId, dateA, termId, cancellationToken);
        var snapshotB = await GetSnapshotAsync(accessorId, studentId, caseId, dateB, termId, cancellationToken);
        var valuesA = snapshotA.Indicators.ToDictionary(item => (item.IndicatorId, item.TermId));
        var valuesB = snapshotB.Indicators.ToDictionary(item => (item.IndicatorId, item.TermId));
        var keys = valuesA.Keys.Union(valuesB.Keys).ToList();
        var response = new StudentIndicatorComparisonResponse
        {
            StudentId = studentId,
            CaseId = caseId,
            DateA = dateA,
            DateB = dateB
        };

        foreach (var key in keys)
        {
            valuesA.TryGetValue(key, out var valueA);
            valuesB.TryGetValue(key, out var valueB);
            var value = valueA ?? valueB!;
            response.Indicators.Add(new StudentIndicatorComparisonValueResponse
            {
                IndicatorId = key.IndicatorId,
                IndicatorName = value.IndicatorName,
                TermId = key.TermId,
                TermName = value.TermName,
                DateAValue = valueA,
                DateBValue = valueB
            });
        }

        response.Indicators = response.Indicators
            .OrderBy(item => item.TermName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.IndicatorName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return response;
    }

    private async Task<StudentIndicatorProfileResponse> GetSnapshotAsync(
        Guid accessorId,
        Guid studentId,
        Guid? caseId,
        DateOnly? asOfDate,
        Guid? termId,
        CancellationToken cancellationToken)
    {
        Guid? consentPurposeId = null;
        if (caseId.HasValue)
        {
            var caseRecord = await _caseRepository.GetByIdAsyncInclude(caseId.Value, record => record.SupportCategory)
                ?? throw new NotFoundException("Case not found.");

            if (caseRecord.StudentId != studentId)
            {
                throw new NotFoundException("Case not found for this student.");
            }

            if (caseRecord.AdvisorId != accessorId)
            {
                throw new ForbiddenException("Only the advisor assigned to this case may read the student's profile.");
            }

            if (caseRecord.SupportCategory is null)
            {
                throw new InvalidDataException("The case has no support category for its consent purpose.");
            }

            consentPurposeId = caseRecord.SupportCategory.ConsentPurposeId;
        }
        else if (accessorId != studentId)
        {
            throw new ForbiddenException("Only the student may read their own profile without a case.");
        }

        DateTime? transactionCutoff = asOfDate.HasValue && asOfDate.Value < DateOnly.MaxValue
            ? asOfDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;
        var rows = await _studentIndicatorRepository.SearchAsyncInclude(
            record => record.StudentId == studentId &&
                      (!termId.HasValue || record.TermId == termId.Value) &&
                      (!transactionCutoff.HasValue || record.TransactionFrom < transactionCutoff.Value),
            record => record.Indicator,
            record => record.Term,
            record => record.AttributeKey);
        var currentRows = rows
            .GroupBy(record => new { record.IndicatorId, record.TermId })
            .Select(group => group
                .OrderByDescending(record => record.TransactionFrom)
                .ThenByDescending(record => record.ValidFrom)
                .ThenByDescending(record => record.StudentIndicatorId)
                .First())
            .OrderBy(record => record.Term.StartDate)
            .ThenBy(record => record.Indicator.IndicatorName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var decisions = currentRows.Count == 0
            ? new List<AccessDecision>()
            : await _accessGuardService.CheckAccessAsync(
                accessorId,
                studentId,
                currentRows.Select(record => record.DataAttributeId).Distinct().ToList(),
                consentPurposeId);
        var decisionsByAttribute = decisions.ToDictionary(decision => decision.DataAttributeId);
        var response = new StudentIndicatorProfileResponse
        {
            StudentId = studentId,
            CaseId = caseId,
            AsOfDate = asOfDate
        };

        foreach (var record in currentRows)
        {
            if (!decisionsByAttribute.TryGetValue(record.DataAttributeId, out var decision))
            {
                throw new InvalidOperationException("The access guard did not return a decision for an indicator attribute.");
            }

            var hasValue = !record.IsMissing && record.ErasedAt is null;
            var item = new StudentIndicatorProfileValueResponse
            {
                IndicatorId = record.IndicatorId,
                IndicatorName = record.Indicator.IndicatorName,
                TermId = record.TermId,
                TermName = record.Term.TermName,
                IsShared = decision.IsAllowed,
                HasValue = hasValue
            };

            if (!decision.IsAllowed)
            {
                response.Indicators.Add(item);
                continue;
            }

            item.DataSourceId = record.DataSourceId;
            item.TransactionFrom = record.TransactionFrom;
            item.ValidFrom = record.ValidFrom;
            item.ProvenanceNote = record.ProvenanceNote;
            item.IsMissing = record.IsMissing;
            item.IsErased = record.ErasedAt is not null;

            if (decision.ConsentPurposeId.HasValue)
            {
                await _accessGuardService.LogAccessAsync(
                    accessorId,
                    studentId,
                    decision.ConsentPurposeId.Value,
                    $"Student indicator: {record.Indicator.IndicatorName}");
            }
            else if (accessorId != studentId)
            {
                throw new InvalidOperationException(
                    "The access guard allowed a staff read without identifying its consent purpose.");
            }

            if (hasValue)
            {
                if (record.AttributeKey is null ||
                    record.AttributeKey.AttributeKeyId != record.AttributeKeyId ||
                    record.AttributeKey.StudentId != record.StudentId ||
                    record.AttributeKey.DataAttributeId != record.DataAttributeId ||
                    record.AttributeKey.Status != "active" ||
                    record.IndicatorValue is null ||
                    record.ValueHmac is null)
                {
                    throw new InvalidDataException("The indicator payload is not bound to an active student-attribute key.");
                }

                item.Value = await _encryptionService.DecryptAsync(
                    record.AttributeKey,
                    record.StudentIndicatorId,
                    "indicatorValue",
                    record.IndicatorValue,
                    record.ValueHmac,
                    cancellationToken);
            }

            response.Indicators.Add(item);
        }

        return response;
    }
}
