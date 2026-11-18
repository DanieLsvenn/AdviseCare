using AdviseCare.Application.Contracts.Requests.Consent;
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
    // Correction requests: a student disputes one exact STUDENTDATA row under a consent purpose and shares that one attribute with it (QC-214);
    // no case is needed. Every advisor whose role has an active access policy for the purpose sees it (the shared reviewer pool), and the first
    // one to approve or reject decides. Approval inserts a new "correction" row (the original stays as history). Values are stored encrypted with the row's key
    public class CorrectionRequestService : ICorrectionRequestService
    {
        private const string CurrentValueField = "currentValue";
        private const string ProposedValueField = "proposedValue";
        private const string DataValueField = "dataValue";

        private readonly IGenericRepository<Correctionrequest> _correctionRequestRepository;
        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Accesspolicy> _accessPolicyRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IConsentService _consentService;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IAccessGuardService _accessGuardService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public CorrectionRequestService(
            IGenericRepository<Correctionrequest> correctionRequestRepository,
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Accesspolicy> accessPolicyRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IConsentService consentService,
            IAttributeKeyService attributeKeyService,
            IAccessGuardService accessGuardService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _correctionRequestRepository = correctionRequestRepository;
            _studentDataRepository = studentDataRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _accessPolicyRepository = accessPolicyRepository;
            _accountRoleRepository = accountRoleRepository;
            _consentService = consentService;
            _attributeKeyService = attributeKeyService;
            _accessGuardService = accessGuardService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Correction requests in the advisor's reviewer pool (purposes the advisor's roles have an active policy for; values shown only when the access guard allows, and logged)
        public async Task<IEnumerable<GetAllCorrectionRequestResponse>> GetAll(Guid AdvisorId)
        {
            var purposeIds = await GetPoolPurposeIdsAsync(AdvisorId);
            var requests = await ReadRequestsAsync(AdvisorId, await GetRequestsAsync(c => purposeIds.Contains(c.ConsentPurposeId)));

            return requests.Select(r => new GetAllCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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

        // Get all Correction requests in the advisor's reviewer pool with pagination
        public async Task<IEnumerable<GetAllCorrectionRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid AdvisorId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var purposeIds = await GetPoolPurposeIdsAsync(AdvisorId);
            var pageOfRequests = (await GetRequestsAsync(c => purposeIds.Contains(c.ConsentPurposeId)))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var requests = await ReadRequestsAsync(AdvisorId, pageOfRequests);

            return requests.Select(r => new GetAllCorrectionRequestWithPaginationResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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

        // Get Correction request by Id (only for an advisor of the request's reviewer pool)
        public async Task<GetCorrectionRequestByIdResponse> GetByIdAsync(Guid AdvisorId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            await EnsurePoolAdvisorAsync(AdvisorId, request.ConsentPurposeId);
            var r = (await ReadRequestsAsync(AdvisorId, new List<Correctionrequest> { request })).Single();

            return new GetCorrectionRequestByIdResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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

        // Search Correction requests in the advisor's reviewer pool by student, consent purpose or status
        public async Task<List<SearchCorrectionRequestResponse>> SearchAsync(Guid AdvisorId, SearchCorrectionRequestRequest request)
        {
            var purposeIds = await GetPoolPurposeIdsAsync(AdvisorId);
            var requests = await ReadRequestsAsync(AdvisorId, await GetRequestsAsync(c =>
                purposeIds.Contains(c.ConsentPurposeId) &&
                (request.ConsentPurposeId == null || c.ConsentPurposeId == request.ConsentPurposeId) &&
                (request.StudentId == null || c.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status)));

            return requests.Select(r => new SearchCorrectionRequestResponse
            {
                CorrectionRequestId = r.Request.CorrectionRequestId,
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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

        // Create Correction request: the student disputes one of their current STUDENTDATA rows under a consent purpose and consents to share that one
        // attribute with it (an existing consent is kept); the value on record and the proposed value are stored encrypted
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
            var purpose = await GetReviewPurposeAsync(request.ConsentPurposeId, source.DataAttributeId);

            // The request works like a one-attribute consent: the reviewer pool can only read the disputed value through it
            await _consentService.CreateAsync(CreatorId, new CreateConsentRequest
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                DataAttributeIds = new List<Guid> { source.DataAttributeId },
                ExpiresAt = null
            });

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
                ConsentPurposeId = purpose.ConsentPurposeId,
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
                $"Asked to correct {source.DataAttribute.AttributeName} ({source.Term.TermName}) under {purpose.PurposeName}");

            return new CreateCorrectionRequestResponse
            {
                CorrectionRequestId = correctionRequest.CorrectionRequestId,
                ConsentPurposeId = correctionRequest.ConsentPurposeId,
                ReviewedBy = null,
                StudentDataId = correctionRequest.StudentDataId,
                StudentId = correctionRequest.StudentId,
                AttributeName = source.DataAttribute.AttributeName,
                CurrentValue = currentValue,
                DecisionNotes = string.Empty,
                Explanation = correctionRequest.Explanation,
                ProposedValue = proposedValue,
                PurposeName = purpose.PurposeName,
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
                ConsentPurposeId = review.Request.ConsentPurposeId,
                ReviewedBy = review.Request.ReviewedBy,
                StudentDataId = review.Request.StudentDataId,
                StudentId = review.Request.StudentId,
                AttributeName = source.DataAttribute.AttributeName,
                CurrentValue = review.CurrentValue,
                DecisionNotes = review.Request.DecisionNotes,
                Explanation = review.Request.Explanation,
                ProposedValue = review.ProposedValue,
                PurposeName = review.Request.ConsentPurpose.PurposeName,
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
                ConsentPurposeId = review.Request.ConsentPurposeId,
                ReviewedBy = review.Request.ReviewedBy,
                StudentDataId = review.Request.StudentDataId,
                StudentId = review.Request.StudentId,
                AttributeName = review.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = review.CurrentValue,
                DecisionNotes = review.Request.DecisionNotes,
                Explanation = review.Request.Explanation,
                ProposedValue = review.ProposedValue,
                PurposeName = review.Request.ConsentPurpose.PurposeName,
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
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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
                ConsentPurposeId = r.Request.ConsentPurposeId,
                ReviewedBy = r.Request.ReviewedBy,
                StudentDataId = r.Request.StudentDataId,
                StudentId = r.Request.StudentId,
                AttributeName = r.Request.StudentData.DataAttribute.AttributeName,
                CurrentValue = r.CurrentValue,
                DecisionNotes = r.Request.DecisionNotes ?? string.Empty,
                Explanation = r.Request.Explanation,
                ProposedValue = r.ProposedValue,
                PurposeName = r.Request.ConsentPurpose.PurposeName,
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

        // Helper: the consent purposes of the advisor's reviewer pool (active purposes, not the statistics one, with an active access policy for one of the advisor's roles)
        private async Task<List<Guid>> GetPoolPurposeIdsAsync(Guid AdvisorId)
        {
            var roleIds = (await _accountRoleRepository.SearchAsync(ar => ar.AccountId == AdvisorId))
                .Select(ar => ar.RoleId)
                .ToList();
            return (await _accessPolicyRepository.SearchAsync(p =>
                    p.Status == "active" &&
                    roleIds.Contains(p.RoleId) &&
                    p.ConsentPurpose.Status == "active" &&
                    !p.ConsentPurpose.IsForStatistics))
                .Select(p => p.ConsentPurposeId)
                .Distinct()
                .ToList();
        }

        // Helper: only an advisor of the request's reviewer pool may see or review it
        private async Task EnsurePoolAdvisorAsync(Guid AdvisorId, Guid ConsentPurposeId)
        {
            if (!(await GetPoolPurposeIdsAsync(AdvisorId)).Contains(ConsentPurposeId))
                throw new ForbiddenException("Your role has no active access policy for this request's consent purpose, so it is not in your review pool.");
        }

        // Helper: the purpose a correction is reviewed under must be active, not the statistics purpose, cover the attribute and have at least one advisor role with an active policy
        private async Task<Consentpurpose> GetReviewPurposeAsync(Guid ConsentPurposeId, Guid DataAttributeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            if (purpose.Status != "active")
                throw new ConflictException($"'{purpose.PurposeName}' is not active, so a correction cannot be reviewed under it.");
            if (purpose.IsForStatistics)
                throw new ValidationException($"'{purpose.PurposeName}' is the statistics purpose; choose an advising purpose for a correction.");
            if (!purpose.DataAttributes.Any(a => a.DataAttributeId == DataAttributeId))
                throw new ValidationException($"'{purpose.PurposeName}' does not cover this data attribute; choose a purpose that does.");
            if (!await _accessPolicyRepository.ExistsAsync(p => p.ConsentPurposeId == ConsentPurposeId && p.Status == "active" &&
                    (p.Role.RoleName == "Academic Advisor" || p.Role.RoleName == "Wellbeing Advisor")))
                throw new ConflictException($"No advisor role has an active access policy for '{purpose.PurposeName}', so nobody could review the correction.");
            return purpose;
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

        // Helper: a pending request an advisor of its pool may review, with its values decrypted (the access guard must allow the attribute; the read is logged)
        private async Task<ReadRequest> LoadForReviewAsync(Guid AdvisorId, Guid CorrectionRequestId)
        {
            var request = (await GetRequestsAsync(c => c.CorrectionRequestId == CorrectionRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Correction request not found.");
            await EnsurePoolAdvisorAsync(AdvisorId, request.ConsentPurposeId);
            if (request.Status != "pending")
                throw new ConflictException("Only a pending correction request can be reviewed.");

            var review = (await ReadRequestsAsync(AdvisorId, new List<Correctionrequest> { request })).Single();
            if (!review.IsShared)
                throw new ForbiddenException("The student no longer shares this data attribute under the request's consent purpose, so it cannot be reviewed.");
            if (review.IsErased)
                throw new ConflictException("The value was erased, so the request can no longer be reviewed.");
            return review;
        }

        // Helper: decrypt the values of the given requests that the reader may see (the student always sees their own;
        // staff go through the access guard under the request's purpose, and every request shown with its values is written to the access log)
        private async Task<List<ReadRequest>> ReadRequestsAsync(Guid ReaderId, List<Correctionrequest> Requests)
        {
            var readable = new List<Correctionrequest>();
            var logEntries = new List<AccessLogEntry>();
            foreach (var group in Requests.GroupBy(r => (r.StudentId, r.ConsentPurposeId)))
            {
                var decisions = (await _accessGuardService.CheckAccessAsync(ReaderId, group.Key.StudentId,
                        group.Select(r => r.StudentData.DataAttributeId).Distinct().ToList(), group.Key.ConsentPurposeId))
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

        // Helper: load Correction requests with the purpose, the source row (attribute, term), the student (and profile) and the reviewer, newest first
        private async Task<List<Correctionrequest>> GetRequestsAsync(Expression<Func<Correctionrequest, bool>> predicate)
        {
            return await _correctionRequestRepository.SearchAsyncIncludeOrderBy(predicate, c => c.SubmittedAt, false,
                c => c.ConsentPurpose, c => c.StudentData.DataAttribute, c => c.StudentData.Term, c => c.Student.Studentprofile, c => c.ReviewedByNavigation);
        }

        // Helper type: a correction request with its decrypted values (null when not shared with the reader or erased)
        private record ReadRequest(Correctionrequest Request, string? CurrentValue, string? ProposedValue, bool IsShared, bool IsErased);
    }
}
