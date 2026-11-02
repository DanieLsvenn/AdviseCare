using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Contracts.Responses.CorrectionRequest;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Correction requests: a student disputes one exact STUDENTDATA row; the advisor of one of the student's open cases reviews it.
    // Approval inserts a new "correction" row (the original stays as history). Values are stored encrypted with the row's key
    public class CorrectionRequestService : ICorrectionRequestService
    {
        private const string CurrentValueField = "currentValue";
        private const string ProposedValueField = "proposedValue";
        private const string DataValueField = "dataValue";

        private readonly IGenericRepository<Correctionrequest> _correctionRequestRepository;
        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IAccessGuardService _accessGuardService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public CorrectionRequestService(
            IGenericRepository<Correctionrequest> correctionRequestRepository,
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Case> caseRepository,
            IAttributeKeyService attributeKeyService,
            IAccessGuardService accessGuardService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _correctionRequestRepository = correctionRequestRepository;
            _studentDataRepository = studentDataRepository;
            _caseRepository = caseRepository;
            _attributeKeyService = attributeKeyService;
            _accessGuardService = accessGuardService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Correction requests of the students the advisor has an open case with (values shown only when the access guard allows, and logged)
        public async Task<IEnumerable<GetAllCorrectionRequestResponse>> GetAll(Guid AdvisorId)
        {
            var studentIds = await GetCaseStudentIdsAsync(AdvisorId);
            var requests = await ReadRequestsAsync(AdvisorId, await GetRequestsAsync(c => studentIds.Contains(c.StudentId)));

            return requests.Select(r => new GetAllCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            });
        }

        // Get all Correction requests of the advisor's case students with pagination
        public async Task<IEnumerable<GetAllCorrectionRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid AdvisorId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var studentIds = await GetCaseStudentIdsAsync(AdvisorId);
            var pageOfRequests = (await GetRequestsAsync(c => studentIds.Contains(c.StudentId)))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var requests = await ReadRequestsAsync(AdvisorId, pageOfRequests);

            return requests.Select(r => new GetAllCorrectionRequestWithPaginationResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            });
        }

        // Get Correction request by Id (only for the advisor of one of the student's open cases)
        public async Task<GetCorrectionRequestByIdResponse> GetByIdAsync(Guid AdvisorId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            await EnsureCaseAdvisorAsync(AdvisorId, request.StudentId);
            var r = (await ReadRequestsAsync(AdvisorId, new List<Correctionrequest> { request })).Single();

            return new GetCorrectionRequestByIdResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            };
        }

        // Search Correction requests of the advisor's case students by student or status
        public async Task<List<SearchCorrectionRequestResponse>> SearchAsync(Guid AdvisorId, SearchCorrectionRequestRequest request)
        {
            var studentIds = await GetCaseStudentIdsAsync(AdvisorId);
            var requests = await ReadRequestsAsync(AdvisorId, await GetRequestsAsync(c =>
                studentIds.Contains(c.StudentId) &&
                (request.StudentId == null || c.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status)));

            return requests.Select(r => new SearchCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            }).ToList();
        }

        // Create Correction request: the student disputes one of their current STUDENTDATA rows; the value on record and the proposed value are stored encrypted
        public async Task<CreateCorrectionRequestResponse> CreateAsync(Guid CreatorId, CreateCorrectionRequestRequest request)
        {
            var source = await GetCurrentSourceAsync(request.StudentDataId);
            if (source.StudentId != CreatorId)
                throw new NotFoundException("Student data row not found.");

            var proposedValue = request.ProposedValue.Trim();
            if (!DataValueValidator.IsValid(source.DataAttribute.DataType, proposedValue))
                throw new ValidationException($"The proposed value is not valid for the {source.DataAttribute.DataType} data attribute '{source.DataAttribute.AttributeName}'.");
            if (await _correctionRequestRepository.ExistsAsync(c => c.StudentDataId == source.StudentDataId && c.Status == "pending"))
                throw new ConflictException("A pending correction request already exists for this value.");

            var currentValue = await DecryptSourceAsync(source);
            var correctionRequestId = Guid.NewGuid();
            var encrypted = await _attributeKeyService.EncryptValuesAsync(new List<PlainAttributeValue>
            {
                new PlainAttributeValue { StudentId = source.StudentId, DataAttributeId = source.DataAttributeId, RowId = correctionRequestId, FieldName = CurrentValueField, Value = currentValue },
                new PlainAttributeValue { StudentId = source.StudentId, DataAttributeId = source.DataAttributeId, RowId = correctionRequestId, FieldName = ProposedValueField, Value = proposedValue }
            });
            var correctionRequest = new Correctionrequest
            {
                CorrectionRequestId = correctionRequestId,
                StudentDataId = source.StudentDataId,
                StudentId = CreatorId,
                CurrentValue = encrypted[0].Payload,
                Explanation = request.Explanation.Trim(),
                ProposedValue = encrypted[1].Payload,
                SubmittedAt = DateTime.UtcNow,
                Status = "pending"
            };
            await _correctionRequestRepository.CreateAsync(correctionRequest);

            await _activityLogService.LogAsync(CreatorId, "correctionRequested",
                $"Asked to correct {source.DataAttribute.AttributeName} ({source.Term.TermName})");

            return new CreateCorrectionRequestResponse
            {
                CorrectionRequestId = correctionRequest.CorrectionRequestId,
                ReviewedBy = null,
                StudentDataId = correctionRequest.StudentDataId,
                StudentId = correctionRequest.StudentId,
                AttributeName = source.DataAttribute.AttributeName,
                CurrentValue = currentValue,
                DecisionNotes = string.Empty,
                Explanation = correctionRequest.Explanation,
                ProposedValue = proposedValue,
                ReviewedByFullName = string.Empty,
                StudentCode = string.Empty,
                StudentFullName = string.Empty,
                TermName = source.Term.TermName,
                DecidedAt = null,
                SubmittedAt = correctionRequest.SubmittedAt,
                IsErased = false,
                IsShared = true,
                Status = correctionRequest.Status
            };
        }

        // Approve Correction request: a new "correction" STUDENTDATA row with the proposed value is inserted (same valid-from as the original), the original stays as history
        public async Task<ApproveCorrectionRequestResponse> ApproveAsync(Guid AdvisorId, Guid CorrectionRequestId, ReviewCorrectionRequestRequest request)
        {
            var review = await LoadForReviewAsync(AdvisorId, CorrectionRequestId);
            var source = await GetCurrentSourceAsync(review.Request.StudentDataId);
            if (await DecryptSourceAsync(source) != review.CurrentValue)
                throw new ConflictException("The value changed after the correction request was submitted.");

            var now = DateTime.UtcNow;
            var correctedRow = new Studentdata
            {
                StudentDataId = Guid.NewGuid(),
                DataAttributeId = source.DataAttributeId,
                DataSourceId = source.DataSourceId,
                StudentId = source.StudentId,
                TermId = source.TermId,
                ProvenanceNote = $"Correction of the value imported on {source.TransactionFrom:yyyy-MM-dd}, approved on {now:yyyy-MM-dd}.",
                TransactionFrom = now,
                ValidFrom = source.ValidFrom,
                IsMissing = false,
                Status = "correction"
            };
            var encrypted = (await _attributeKeyService.EncryptValuesAsync(new List<PlainAttributeValue>
            {
                new PlainAttributeValue { StudentId = source.StudentId, DataAttributeId = source.DataAttributeId, RowId = correctedRow.StudentDataId, FieldName = DataValueField, Value = review.ProposedValue! }
            })).Single();
            correctedRow.AttributeKeyId = encrypted.AttributeKeyId;
            correctedRow.DataValue = encrypted.Payload;
            _studentDataRepository.PrepareCreate(correctedRow);

            review.Request.ReviewedBy = AdvisorId;
            review.Request.DecisionNotes = request.DecisionNotes.Trim();
            review.Request.DecidedAt = now;
            review.Request.Status = "approved";
            await _correctionRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(review.Request.StudentId, "Correction request approved",
                $"Your correction of {source.DataAttribute.AttributeName} ({source.Term.TermName}) was approved.", "/my-data");
            await _activityLogService.LogAsync(AdvisorId, "correctionReviewed",
                $"Approved the correction of {review.Request.Student.FullName}'s {source.DataAttribute.AttributeName} ({source.Term.TermName})");

            return new ApproveCorrectionRequestResponse
            {
                CorrectionRequestId = review.Request.CorrectionRequestId,
                ReviewedBy = review.Request.ReviewedBy,
                StudentDataId = review.Request.StudentDataId,
                StudentId = review.Request.StudentId,
                AttributeName = source.DataAttribute.AttributeName,
                CurrentValue = review.CurrentValue,
                DecisionNotes = review.Request.DecisionNotes,
                Explanation = review.Request.Explanation,
                ProposedValue = review.ProposedValue,
                ReviewedByFullName = string.Empty,
                StudentCode = review.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = review.Request.Student.FullName,
                TermName = source.Term.TermName,
                DecidedAt = review.Request.DecidedAt,
                SubmittedAt = review.Request.SubmittedAt,
                IsErased = false,
                IsShared = true,
                Status = review.Request.Status
            };
        }

        // Reject Correction request with the advisor's decision notes; the student is notified
        public async Task<RejectCorrectionRequestResponse> RejectAsync(Guid AdvisorId, Guid CorrectionRequestId, ReviewCorrectionRequestRequest request)
        {
            var review = await LoadForReviewAsync(AdvisorId, CorrectionRequestId);

            review.Request.ReviewedBy = AdvisorId;
            review.Request.DecisionNotes = request.DecisionNotes.Trim();
            review.Request.DecidedAt = DateTime.UtcNow;
            review.Request.Status = "rejected";
            await _correctionRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(review.Request.StudentId, "Correction request rejected",
                $"Your correction of {review.Request.StudentData.DataAttribute.AttributeName} was rejected: {review.Request.DecisionNotes}", "/my-data");
            await _activityLogService.LogAsync(AdvisorId, "correctionReviewed",
                $"Rejected the correction of {review.Request.Student.FullName}'s {review.Request.StudentData.DataAttribute.AttributeName}");

            return new RejectCorrectionRequestResponse
            {
                CorrectionRequestId = review.Request.CorrectionRequestId,
                ReviewedBy = review.Request.ReviewedBy,
                StudentDataId = review.Request.StudentDataId,
                StudentId = review.Request.StudentId,
                AttributeName = review.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = review.CurrentValue,
                DecisionNotes = review.Request.DecisionNotes,
                Explanation = review.Request.Explanation,
                ProposedValue = review.ProposedValue,
                ReviewedByFullName = string.Empty,
                StudentCode = review.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = review.Request.Student.FullName,
                TermName = review.Request.StudentData.Term.TermName,
                DecidedAt = review.Request.DecidedAt,
                SubmittedAt = review.Request.SubmittedAt,
                IsErased = false,
                IsShared = true,
                Status = review.Request.Status
            };
        }

        // Withdraw Correction request: the student cancels their own pending request
        public async Task<bool> WithdrawAsync(Guid StudentId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId && c.StudentId == StudentId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            if (request.Status != "pending")
                throw new ConflictException("Only a pending correction request can be withdrawn.");

            request.Status = "withdrawn";
            await _correctionRequestRepository.SaveAsync();

            await _activityLogService.LogAsync(StudentId, "correctionRequestWithdrawn",
                $"Withdrew the correction request on {request.StudentData.DataAttribute.AttributeName} ({request.StudentData.Term.TermName})");
            return true;
        }

        // Get all of the signed-in student's own Correction requests
        public async Task<List<GetMyCorrectionRequestResponse>> GetMyAllAsync(Guid StudentId)
        {
            var requests = await ReadRequestsAsync(StudentId, await GetRequestsAsync(c => c.StudentId == StudentId));

            return requests.Select(r => new GetMyCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            }).ToList();
        }

        // Get one of the signed-in student's own Correction requests
        public async Task<GetMyCorrectionRequestResponse> GetMyByIdAsync(Guid StudentId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId && c.StudentId == StudentId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            var r = (await ReadRequestsAsync(StudentId, new List<Correctionrequest> { request })).Single();

            return new GetMyCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                ReviewedByFullName = r.Request.ReviewedByNavigation?.FullName ?? string.Empty,
                StudentCode = r.Request.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = r.Request.Student.FullName,
                TermName = r.Request.StudentData.Term.TermName,
                DecidedAt = r.Request.DecidedAt,
                SubmittedAt = r.Request.SubmittedAt,
                IsErased = r.IsErased,
                IsShared = r.IsShared,
                Status = r.Request.Status
            };
        }

        // Helper: students the advisor currently has an open (not closed) case with
        private async Task<List<Guid>> GetCaseStudentIdsAsync(Guid AdvisorId)
        {
            return (await _caseRepository.SearchAsync(c => c.AdvisorId == AdvisorId && c.Status != "closed"))
                .Select(c => c.StudentId)
                .Distinct()
                .ToList();
        }

        // Helper: only the advisor of one of the student's open cases may see or review the student's correction requests
        private async Task EnsureCaseAdvisorAsync(Guid AdvisorId, Guid StudentId)
        {
            if (!await _caseRepository.ExistsAsync(c => c.AdvisorId == AdvisorId && c.StudentId == StudentId && c.Status != "closed"))
                throw new ForbiddenException("Only the advisor of one of the student's open cases can see this student's correction requests.");
        }

        // Helper: the disputed row must be the student's current (latest recorded) value for its attribute and term, with a value and an active key
        private async Task<Studentdata> GetCurrentSourceAsync(Guid StudentDataId)
        {
            var source = (await _studentDataRepository.SearchAsyncInclude(d => d.StudentDataId == StudentDataId,
                    d => d.DataAttribute, d => d.Term, d => d.AttributeKey)).FirstOrDefault()
                ?? throw new NotFoundException("Student data row not found.");
            if (source.IsMissing || source.ErasedAt != null || source.DataValue == null || source.AttributeKey == null || source.AttributeKey.Status != "active")
                throw new ConflictException("A missing or erased value cannot be corrected.");
            if (await _studentDataRepository.ExistsAsync(d =>
                    d.StudentId == source.StudentId && d.DataAttributeId == source.DataAttributeId && d.TermId == source.TermId &&
                    d.TransactionFrom > source.TransactionFrom))
                throw new ConflictException("Only the latest value of an attribute and term can be corrected.");
            return source;
        }

        // Helper: decrypt the value of a STUDENTDATA row
        private async Task<string> DecryptSourceAsync(Studentdata source)
        {
            var value = (await _attributeKeyService.DecryptValuesAsync(new List<EncryptedAttributeValue>
            {
                new EncryptedAttributeValue { AttributeKeyId = source.AttributeKeyId!.Value, StudentId = source.StudentId, DataAttributeId = source.DataAttributeId, RowId = source.StudentDataId, FieldName = DataValueField, Payload = source.DataValue }
            })).Single();
            if (value.IsErased || value.Value == null)
                throw new ConflictException("A missing or erased value cannot be corrected.");
            return value.Value;
        }

        // Helper: a pending request the advisor may review, with its values decrypted (the access guard must allow the attribute; the read is logged)
        private async Task<ReadRequest> LoadForReviewAsync(Guid AdvisorId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            await EnsureCaseAdvisorAsync(AdvisorId, request.StudentId);
            if (request.Status != "pending")
                throw new ConflictException("Only a pending correction request can be reviewed.");

            var review = (await ReadRequestsAsync(AdvisorId, new List<Correctionrequest> { request })).Single();
            if (!review.IsShared)
                throw new ForbiddenException("The student has not shared this data attribute with you (consent or emergency access is needed to review it).");
            if (review.IsErased)
                throw new ConflictException("The value was erased, so the request can no longer be reviewed.");
            return review;
        }

        // Helper: decrypt the values of the given requests that the reader may see (the student always sees their own;
        // staff go through the access guard, and every request shown with its values is written to the access log)
        private async Task<List<ReadRequest>> ReadRequestsAsync(Guid ReaderId, List<Correctionrequest> Requests)
        {
            var readable = new List<Correctionrequest>();
            var logEntries = new List<AccessLogEntry>();
            foreach (var group in Requests.GroupBy(r => r.StudentId))
            {
                var decisions = (await _accessGuardService.CheckAccessAsync(ReaderId, group.Key,
                        group.Select(r => r.StudentData.DataAttributeId).Distinct().ToList(), null))
                    .ToDictionary(d => d.DataAttributeId);
                foreach (var request in group.Where(r => decisions[r.StudentData.DataAttributeId].IsAllowed))
                {
                    readable.Add(request);
                    logEntries.Add(new AccessLogEntry
                    {
                        StudentId = request.StudentId,
                        StudentDataId = request.StudentDataId,
                        ConsentPurposeId = decisions[request.StudentData.DataAttributeId].ConsentPurposeId,
                        AccessedResource = $"Correction request: {request.StudentData.DataAttribute.AttributeName} ({request.StudentData.Term.TermName})"
                    });
                }
            }

            var encrypted = readable
                .Where(r => r.StudentData.AttributeKeyId.HasValue)
                .SelectMany(r => new[]
                {
                    new EncryptedAttributeValue { AttributeKeyId = r.StudentData.AttributeKeyId!.Value, StudentId = r.StudentId, DataAttributeId = r.StudentData.DataAttributeId, RowId = r.CorrectionRequestId, FieldName = CurrentValueField, Payload = r.CurrentValue },
                    new EncryptedAttributeValue { AttributeKeyId = r.StudentData.AttributeKeyId!.Value, StudentId = r.StudentId, DataAttributeId = r.StudentData.DataAttributeId, RowId = r.CorrectionRequestId, FieldName = ProposedValueField, Payload = r.ProposedValue }
                })
                .ToList();
            var decrypted = (await _attributeKeyService.DecryptValuesAsync(encrypted)).ToDictionary(v => (v.RowId, v.FieldName));
            await _accessGuardService.LogAccessBatchAsync(ReaderId, logEntries);

            var readableIds = readable.Select(r => r.CorrectionRequestId).ToHashSet();
            return Requests.Select(r =>
            {
                decrypted.TryGetValue((r.CorrectionRequestId, CurrentValueField), out var current);
                decrypted.TryGetValue((r.CorrectionRequestId, ProposedValueField), out var proposed);
                return new ReadRequest(r, current?.Value, proposed?.Value, readableIds.Contains(r.CorrectionRequestId), (current?.IsErased ?? false) || (proposed?.IsErased ?? false));
            }).ToList();
        }

        // Helper: load Correction requests with the source row (attribute, term), the student (and profile) and the reviewer, newest first
        private async Task<List<Correctionrequest>> GetRequestsAsync(Expression<Func<Correctionrequest, bool>> predicate)
        {
            return await _correctionRequestRepository.SearchAsyncIncludeOrderBy(predicate, c => c.SubmittedAt, false,
                c => c.StudentData.DataAttribute, c => c.StudentData.Term, c => c.Student.Studentprofile, c => c.ReviewedByNavigation);
        }

        // Helper type: a correction request with its decrypted values (null when not shared with the reader or erased)
        private record ReadRequest(Correctionrequest Request, string? CurrentValue, string? ProposedValue, bool IsShared, bool IsErased);
    }
}
