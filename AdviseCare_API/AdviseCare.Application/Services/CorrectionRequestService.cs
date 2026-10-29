using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Contracts.Responses.CorrectionRequest;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services;

public sealed class CorrectionRequestService(
    IGenericRepository<Correctionrequest> correctionRequestRepository,
    IGenericRepository<Studentindicator> studentIndicatorRepository,
    IGenericRepository<Advisorassignment> advisorAssignmentRepository,
    IAccessGuardService accessGuardService,
    IAuthenticatedEnvelopeEncryptionService encryptionService,
    INotificationService notificationService) : ICorrectionRequestService
{
    private const string PendingStatus = "pending";
    private const string StudentAndReviewerVisibility = "studentAndReviewer";
    private const string ActiveKeyStatus = "active";

    private readonly IGenericRepository<Correctionrequest> _correctionRequestRepository = correctionRequestRepository;
    private readonly IGenericRepository<Studentindicator> _studentIndicatorRepository = studentIndicatorRepository;
    private readonly IGenericRepository<Advisorassignment> _advisorAssignmentRepository = advisorAssignmentRepository;
    private readonly IAccessGuardService _accessGuardService = accessGuardService;
    private readonly IAuthenticatedEnvelopeEncryptionService _encryptionService = encryptionService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<GetMyCorrectionRequestResponse> GetMineAsync(
        Guid studentId,
        Guid correctionRequestId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correctionRequest = await _correctionRequestRepository.GetByIdAsync(correctionRequestId)
            ?? throw new NotFoundException("Correction request not found.");
        if (correctionRequest.StudentId != studentId)
        {
            throw new NotFoundException("Correction request not found.");
        }

        return new GetMyCorrectionRequestResponse
        {
            CorrectionRequestId = correctionRequest.CorrectionRequestId,
            StudentIndicatorId = correctionRequest.StudentIndicatorId,
            FieldName = correctionRequest.FieldName,
            Explanation = correctionRequest.Explanation,
            DecisionNotes = correctionRequest.TextVisibility == StudentAndReviewerVisibility
                ? correctionRequest.DecisionNotes
                : null,
            SubmittedAt = correctionRequest.SubmittedAt,
            DecidedAt = correctionRequest.DecidedAt,
            Status = correctionRequest.Status
        };
    }

    public async Task<CreateCorrectionRequestResponse> CreateAsync(
        Guid studentId,
        CreateCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.StudentIndicatorId == Guid.Empty)
        {
            throw new ValidationException("StudentIndicatorId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ProposedValue))
        {
            throw new ValidationException("A proposed value is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Explanation))
        {
            throw new ValidationException("An explanation is required.");
        }

        var source = await _studentIndicatorRepository.GetByIdAsyncInclude(
            request.StudentIndicatorId,
            record => record.Indicator,
            record => record.AttributeKey)
            ?? throw new NotFoundException("Student indicator not found.");

        if (source.StudentId != studentId)
        {
            throw new NotFoundException("Student indicator not found.");
        }

        if (source.IsMissing || source.ErasedAt.HasValue || source.IndicatorValue is null || source.ValueHmac is null)
        {
            throw new ConflictException("A missing or erased indicator value cannot be corrected.");
        }

        if (source.AttributeKey is null ||
            source.AttributeKey.AttributeKeyId != source.AttributeKeyId ||
            source.AttributeKey.StudentId != studentId ||
            source.AttributeKey.DataAttributeId != source.DataAttributeId ||
            source.AttributeKey.Status != ActiveKeyStatus)
        {
            throw new ConflictException("The indicator value is no longer available for correction.");
        }

        await EnsureCurrentSourceAsync(source);

        var proposedValue = request.ProposedValue.Trim();
        if (!IndicatorValueValidator.IsValid(source.Indicator.DataType, proposedValue))
        {
            throw new ValidationException($"The proposed value is not valid for the {source.Indicator.DataType} indicator type.");
        }

        var pendingRequestExists = await _correctionRequestRepository.ExistsAsync(
            record => record.StudentIndicatorId == source.StudentIndicatorId && record.Status == PendingStatus);
        if (pendingRequestExists)
        {
            throw new ConflictException("A pending correction request already exists for this indicator value.");
        }

        var accessDecision = (await _accessGuardService.CheckAccessAsync(
                studentId,
                studentId,
                new List<Guid> { source.DataAttributeId },
                null))
            .SingleOrDefault(decision => decision.DataAttributeId == source.DataAttributeId);
        if (accessDecision is null || !accessDecision.IsAllowed)
        {
            throw new ForbiddenException("The student is not allowed to read this indicator value.");
        }

        var currentValue = await _encryptionService.DecryptAsync(
            source.AttributeKey,
            source.StudentIndicatorId,
            "indicatorValue",
            source.IndicatorValue,
            source.ValueHmac,
            cancellationToken);
        var correctionRequestId = Guid.NewGuid();
        var encryptedCurrentValue = await _encryptionService.EncryptAsync(
            source.AttributeKey,
            correctionRequestId,
            "currentValue",
            currentValue,
            cancellationToken);
        var encryptedProposedValue = await _encryptionService.EncryptAsync(
            source.AttributeKey,
            correctionRequestId,
            "proposedValue",
            proposedValue,
            cancellationToken);
        var submittedAt = DateTime.UtcNow;
        var correctionRequest = new Correctionrequest
        {
            CorrectionRequestId = correctionRequestId,
            StudentId = studentId,
            StudentIndicatorId = source.StudentIndicatorId,
            DataAttributeId = source.DataAttributeId,
            AttributeKeyId = source.AttributeKey.AttributeKeyId,
            CurrentValue = encryptedCurrentValue.Ciphertext,
            CurrentValueHmac = encryptedCurrentValue.Hmac,
            ProposedValue = encryptedProposedValue.Ciphertext,
            ProposedValueHmac = encryptedProposedValue.Hmac,
            FieldName = source.Indicator.IndicatorName,
            Explanation = request.Explanation.Trim(),
            TextVisibility = StudentAndReviewerVisibility,
            SubmittedAt = submittedAt,
            Status = PendingStatus
        };

        _correctionRequestRepository.PrepareCreate(correctionRequest);
        await _correctionRequestRepository.SaveAsync();

        return new CreateCorrectionRequestResponse
        {
            CorrectionRequestId = correctionRequestId,
            StudentIndicatorId = source.StudentIndicatorId,
            Status = PendingStatus,
            SubmittedAt = submittedAt
        };
    }

    public async Task<List<CorrectionRequestReviewResponse>> SearchForAdvisorAsync(
        Guid advisorId,
        string status,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (status is not ("pending" or "approved" or "rejected" or "withdrawn"))
        {
            throw new ValidationException("Status must be pending, approved, rejected, or withdrawn.");
        }

        var assignments = await _advisorAssignmentRepository.SearchAsync(
            record => record.AdvisorId == advisorId &&
                      record.Status == ActiveKeyStatus &&
                      record.RevokedAt == null);
        var studentIds = assignments.Select(record => record.StudentId).Distinct().ToList();
        if (studentIds.Count == 0)
        {
            return new List<CorrectionRequestReviewResponse>();
        }

        var correctionRequests = await _correctionRequestRepository.SearchAsyncInclude(
            record => studentIds.Contains(record.StudentId) && record.Status == status,
            record => record.Student.Studentprofile,
            record => record.AttributeKey);
        var responses = new List<CorrectionRequestReviewResponse>(correctionRequests.Count);
        foreach (var correctionRequest in correctionRequests.OrderByDescending(record => record.SubmittedAt))
        {
            cancellationToken.ThrowIfCancellationRequested();
            responses.Add(await BuildReviewResponseAsync(advisorId, correctionRequest, cancellationToken));
        }

        return responses;
    }

    public async Task<CorrectionRequestReviewResponse> ApproveAsync(
        Guid advisorId,
        Guid correctionRequestId,
        CorrectionDecisionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDecisionRequest(request);
        var review = await LoadRequestForDecisionAsync(advisorId, correctionRequestId, cancellationToken);
        await EnsureCurrentSourceAsync(review.Source);

        if (review.Source.IsMissing ||
            review.Source.ErasedAt.HasValue ||
            review.Source.AttributeKey is null ||
            review.Source.AttributeKey.Status != ActiveKeyStatus ||
            review.Source.IndicatorValue is null ||
            review.Source.ValueHmac is null)
        {
            throw new ConflictException("The source indicator value is no longer available for correction.");
        }

        var sourceValue = await _encryptionService.DecryptAsync(
            review.Source.AttributeKey,
            review.Source.StudentIndicatorId,
            "indicatorValue",
            review.Source.IndicatorValue,
            review.Source.ValueHmac,
            cancellationToken);
        if (!string.Equals(sourceValue, review.CurrentValue, StringComparison.Ordinal))
        {
            throw new ConflictException("The indicator value changed after the correction request was submitted.");
        }

        var transactionFrom = DateTime.UtcNow;
        var correctedIndicatorId = Guid.NewGuid();
        var encryptedCorrectedValue = await _encryptionService.EncryptAsync(
            review.Source.AttributeKey,
            correctedIndicatorId,
            "indicatorValue",
            review.ProposedValue,
            cancellationToken);
        review.Request.Status = "approved";
        review.Request.ReviewedBy = advisorId;
        review.Request.DecidedAt = transactionFrom;
        review.Request.DecisionNotes = request.DecisionNotes.Trim();
        _correctionRequestRepository.PrepareUpdate(review.Request);
        _studentIndicatorRepository.PrepareCreate(new Studentindicator
        {
            StudentIndicatorId = correctedIndicatorId,
            DataSourceId = review.Source.DataSourceId,
            IndicatorId = review.Source.IndicatorId,
            DataAttributeId = review.Source.DataAttributeId,
            StudentId = review.Source.StudentId,
            AttributeKeyId = review.Source.AttributeKeyId,
            TermId = review.Source.TermId,
            IndicatorValue = encryptedCorrectedValue.Ciphertext,
            ValueHmac = encryptedCorrectedValue.Hmac,
            ProvenanceNote = $"Correction request {correctionRequestId:N} approved on {transactionFrom:yyyy-MM-dd}.",
            TransactionFrom = transactionFrom,
            ValidFrom = review.Source.ValidFrom,
            IsMissing = false,
            ErasedAt = null,
            Status = "correction"
        });
        await _correctionRequestRepository.SaveAsync();

        await _notificationService.NotifyAsync(
            review.Request.StudentId,
            "Correction request approved",
            "Your indicator correction request was approved.",
            "/profile");

        return ToReviewResponse(
            review.Request,
            review.Source,
            review.CurrentValue,
            review.ProposedValue,
            true,
            false);
    }

    public async Task<CorrectionRequestReviewResponse> RejectAsync(
        Guid advisorId,
        Guid correctionRequestId,
        CorrectionDecisionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDecisionRequest(request);
        var review = await LoadRequestForDecisionAsync(advisorId, correctionRequestId, cancellationToken);
        var decidedAt = DateTime.UtcNow;
        review.Request.Status = "rejected";
        review.Request.ReviewedBy = advisorId;
        review.Request.DecidedAt = decidedAt;
        review.Request.DecisionNotes = request.DecisionNotes.Trim();
        _correctionRequestRepository.PrepareUpdate(review.Request);
        await _correctionRequestRepository.SaveAsync();

        await _notificationService.NotifyAsync(
            review.Request.StudentId,
            "Correction request rejected",
            "Your indicator correction request was reviewed. Sign in to view the decision.",
            "/profile");

        return ToReviewResponse(
            review.Request,
            review.Source,
            review.CurrentValue,
            review.ProposedValue,
            true,
            false);
    }

    public async Task WithdrawAsync(
        Guid studentId,
        Guid correctionRequestId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correctionRequest = await _correctionRequestRepository.GetByIdAsync(correctionRequestId)
            ?? throw new NotFoundException("Correction request not found.");
        if (correctionRequest.StudentId != studentId)
        {
            throw new NotFoundException("Correction request not found.");
        }

        if (correctionRequest.Status != PendingStatus)
        {
            throw new ConflictException("Only a pending correction request can be withdrawn.");
        }

        correctionRequest.Status = "withdrawn";
        correctionRequest.DecidedAt = DateTime.UtcNow;
        _correctionRequestRepository.PrepareUpdate(correctionRequest);
        await _correctionRequestRepository.SaveAsync();
    }

    private async Task<ReviewRequestData> LoadRequestForDecisionAsync(
        Guid advisorId,
        Guid correctionRequestId,
        CancellationToken cancellationToken)
    {
        var correctionRequest = await _correctionRequestRepository.GetByIdAsyncInclude(
            correctionRequestId,
            record => record.Student.Studentprofile,
            record => record.AttributeKey)
            ?? throw new NotFoundException("Correction request not found.");
        if (correctionRequest.Status != PendingStatus)
        {
            throw new ConflictException("Only a pending correction request can be reviewed.");
        }

        await EnsureAdvisorAssignmentAsync(advisorId, correctionRequest.StudentId);
        if (correctionRequest.TextVisibility != StudentAndReviewerVisibility)
        {
            throw new ForbiddenException("This correction request is not visible to the assigned advisor.");
        }

        var source = await GetSourceIndicatorAsync(correctionRequest);
        var decision = await GetReviewAccessDecisionAsync(advisorId, correctionRequest);
        if (!decision.IsAllowed || !decision.ConsentPurposeId.HasValue)
        {
            throw new ForbiddenException("Consent and an active access policy are required to review this indicator.");
        }

        await _accessGuardService.LogAccessAsync(
            advisorId,
            correctionRequest.StudentId,
            decision.ConsentPurposeId.Value,
            $"Correction request: {correctionRequest.FieldName}");
        var values = await DecryptRequestValuesAsync(correctionRequest, cancellationToken);
        return new ReviewRequestData(correctionRequest, source, values.CurrentValue, values.ProposedValue);
    }

    private async Task<CorrectionRequestReviewResponse> BuildReviewResponseAsync(
        Guid advisorId,
        Correctionrequest correctionRequest,
        CancellationToken cancellationToken)
    {
        var source = await GetSourceIndicatorAsync(correctionRequest);
        var isShared = correctionRequest.TextVisibility == StudentAndReviewerVisibility;
        if (isShared)
        {
            var decision = await GetReviewAccessDecisionAsync(advisorId, correctionRequest);
            isShared = decision.IsAllowed;
            if (isShared)
            {
                if (!decision.ConsentPurposeId.HasValue)
                {
                    throw new InvalidOperationException("The access guard allowed a staff read without identifying its consent purpose.");
                }

                await _accessGuardService.LogAccessAsync(
                    advisorId,
                    correctionRequest.StudentId,
                    decision.ConsentPurposeId.Value,
                    $"Correction request: {correctionRequest.FieldName}");
            }
        }

        var currentValue = (string?)null;
        var proposedValue = (string?)null;
        var isErased = IsRequestValueErased(correctionRequest);
        if (isShared && !isErased)
        {
            var values = await DecryptRequestValuesAsync(correctionRequest, cancellationToken);
            currentValue = values.CurrentValue;
            proposedValue = values.ProposedValue;
        }

        return ToReviewResponse(
            correctionRequest,
            source,
            currentValue,
            proposedValue,
            isShared,
            isErased);
    }

    private async Task<AccessDecision> GetReviewAccessDecisionAsync(
        Guid advisorId,
        Correctionrequest correctionRequest)
    {
        var decisions = await _accessGuardService.CheckAccessAsync(
            advisorId,
            correctionRequest.StudentId,
            new List<Guid> { correctionRequest.DataAttributeId },
            null);
        return decisions.SingleOrDefault(decision => decision.DataAttributeId == correctionRequest.DataAttributeId)
            ?? throw new InvalidOperationException("The access guard did not return a decision for the correction request.");
    }

    private async Task EnsureAdvisorAssignmentAsync(Guid advisorId, Guid studentId)
    {
        var isAssigned = await _advisorAssignmentRepository.ExistsAsync(
            assignment => assignment.AdvisorId == advisorId &&
                          assignment.StudentId == studentId &&
                          assignment.Status == ActiveKeyStatus &&
                          assignment.RevokedAt == null);
        if (!isAssigned)
        {
            throw new NotFoundException("Correction request not found.");
        }
    }

    private async Task EnsureCurrentSourceAsync(Studentindicator source)
    {
        var rows = await _studentIndicatorRepository.SearchAsync(
            record => record.StudentId == source.StudentId &&
                      record.IndicatorId == source.IndicatorId &&
                      record.TermId == source.TermId);
        var current = rows
            .OrderByDescending(record => record.TransactionFrom)
            .ThenByDescending(record => record.ValidFrom)
            .ThenByDescending(record => record.StudentIndicatorId)
            .FirstOrDefault();
        if (current?.StudentIndicatorId != source.StudentIndicatorId)
        {
            throw new ConflictException("Only the latest indicator value can be corrected.");
        }
    }

    private async Task<Studentindicator> GetSourceIndicatorAsync(Correctionrequest correctionRequest)
    {
        var source = await _studentIndicatorRepository.GetByIdAsyncInclude(
            correctionRequest.StudentIndicatorId,
            record => record.Indicator,
            record => record.Term,
            record => record.AttributeKey)
            ?? throw new InvalidDataException("The correction request source indicator is missing.");
        if (source.StudentId != correctionRequest.StudentId ||
            source.DataAttributeId != correctionRequest.DataAttributeId ||
            source.AttributeKeyId != correctionRequest.AttributeKeyId)
        {
            throw new InvalidDataException("The correction request source does not match its student, attribute, or key.");
        }

        return source;
    }

    private async Task<(string CurrentValue, string ProposedValue)> DecryptRequestValuesAsync(
        Correctionrequest correctionRequest,
        CancellationToken cancellationToken)
    {
        if (correctionRequest.AttributeKey is null ||
            correctionRequest.AttributeKey.Status != ActiveKeyStatus ||
            correctionRequest.CurrentValue is null ||
            correctionRequest.CurrentValueHmac is null ||
            correctionRequest.ProposedValue is null ||
            correctionRequest.ProposedValueHmac is null)
        {
            throw new ConflictException("The correction request values are unavailable because the value was erased or its key is inactive.");
        }

        var currentValue = await _encryptionService.DecryptAsync(
            correctionRequest.AttributeKey,
            correctionRequest.CorrectionRequestId,
            "currentValue",
            correctionRequest.CurrentValue,
            correctionRequest.CurrentValueHmac,
            cancellationToken);
        var proposedValue = await _encryptionService.DecryptAsync(
            correctionRequest.AttributeKey,
            correctionRequest.CorrectionRequestId,
            "proposedValue",
            correctionRequest.ProposedValue,
            correctionRequest.ProposedValueHmac,
            cancellationToken);
        return (currentValue, proposedValue);
    }

    private static bool IsRequestValueErased(Correctionrequest correctionRequest) =>
        correctionRequest.AttributeKey?.Status != ActiveKeyStatus ||
        correctionRequest.CurrentValue is null ||
        correctionRequest.CurrentValueHmac is null ||
        correctionRequest.ProposedValue is null ||
        correctionRequest.ProposedValueHmac is null;

    private static CorrectionRequestReviewResponse ToReviewResponse(
        Correctionrequest correctionRequest,
        Studentindicator source,
        string? currentValue,
        string? proposedValue,
        bool isShared,
        bool isErased)
    {
        return new CorrectionRequestReviewResponse
        {
            CorrectionRequestId = correctionRequest.CorrectionRequestId,
            StudentId = correctionRequest.StudentId,
            StudentCode = correctionRequest.Student?.Studentprofile?.StudentCode ?? string.Empty,
            StudentIndicatorId = source.StudentIndicatorId,
            IndicatorId = source.IndicatorId,
            TermId = source.TermId,
            TermName = source.Term.TermName,
            FieldName = correctionRequest.FieldName,
            CurrentValue = isShared ? currentValue : null,
            ProposedValue = isShared ? proposedValue : null,
            Explanation = isShared ? correctionRequest.Explanation : null,
            DecisionNotes = isShared ? correctionRequest.DecisionNotes : null,
            SubmittedAt = correctionRequest.SubmittedAt,
            Status = correctionRequest.Status,
            IsShared = isShared,
            IsErased = isErased
        };
    }

    private static void ValidateDecisionRequest(CorrectionDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DecisionNotes))
        {
            throw new ValidationException("Decision notes are required.");
        }
    }

    private sealed record ReviewRequestData(
        Correctionrequest Request,
        Studentindicator Source,
        string CurrentValue,
        string ProposedValue);
}
