using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Contracts.Requests.IndicatorFlag;
using AdviseCare.Application.Contracts.Requests.TakeOnRequest;
using AdviseCare.Application.Contracts.Responses.TakeOnRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Take-on requests (TAKEONREQUESTS, "Students in need"): an advisor asks to take on an unassigned student-created case (stage pending,
    // no advisor, not closed) or an open indicator flag of their own support type, with an optional note; one pending request per advisor and item.
    // Every Programme Head is notified. The Programme Head approves one (a case: assigned to that advisor through CaseService.AssignAsync;
    // a flag: a case is opened for that advisor through IndicatorFlagService.OpenCaseAsync) or declines one with a reason; the advisor can
    // withdraw a pending one. The approved status is staged first and saved by the assign / open-case save, which also cancels the other
    // pending requests for the item. The advisor is notified of every decision (in-app, and by email when that preference is on).
    public class TakeOnRequestService : ITakeOnRequestService
    {
        private readonly IGenericRepository<Takeonrequest> _takeOnRequestRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Indicatorflag> _indicatorFlagRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly ICaseService _caseService;
        private readonly IIndicatorFlagService _indicatorFlagService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public TakeOnRequestService(
            IGenericRepository<Takeonrequest> takeOnRequestRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Indicatorflag> indicatorFlagRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            ICaseService caseService,
            IIndicatorFlagService indicatorFlagService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _takeOnRequestRepository = takeOnRequestRepository;
            _caseRepository = caseRepository;
            _indicatorFlagRepository = indicatorFlagRepository;
            _accountRoleRepository = accountRoleRepository;
            _caseService = caseService;
            _indicatorFlagService = indicatorFlagService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Take-on requests, pending first, then newest first (Programme Head)
        public async Task<IEnumerable<GetAllTakeOnRequestResponse>> GetAll()
        {
            var requests = await GetRequestsAsync(r => true);

            var openCaseCounts = await GetOpenCaseCountsAsync(requests);

            return requests.Select(r => new GetAllTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            });
        }

        // Get all Take-on requests with pagination
        public async Task<IEnumerable<GetAllTakeOnRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = (await GetRequestsAsync(r => true))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var openCaseCounts = await GetOpenCaseCountsAsync(requests);

            return requests.Select(r => new GetAllTakeOnRequestWithPaginationResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            });
        }

        // Get Take-on request by Id (the Programme Head, or the advisor who sent it)
        public async Task<GetTakeOnRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid TakeOnRequestId)
        {
            var r = await GetRequestAsync(TakeOnRequestId);
            if (r.AdvisorId != CallerId && !await HasRoleAsync(CallerId, "Programme Head"))
                throw new NotFoundException("Take-on request not found.");

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new GetTakeOnRequestByIdResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Search Take-on requests by advisor, case, flag and status, pending first, then newest first (Programme Head; e.g. the Requests pop-up of one case or flag)
        public async Task<List<SearchTakeOnRequestResponse>> SearchAsync(SearchTakeOnRequestRequest request)
        {
            var requests = await GetRequestsAsync(r =>
                (request.AdvisorId == null || r.AdvisorId == request.AdvisorId) &&
                (request.CaseId == null || r.CaseId == request.CaseId) &&
                (request.IndicatorFlagId == null || r.IndicatorFlagId == request.IndicatorFlagId) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            var openCaseCounts = await GetOpenCaseCountsAsync(requests);

            return requests.Select(r => new SearchTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            }).ToList();
        }

        // Create Take-on request: an advisor asks to take on an unassigned student-created case or an open flag of their support type; the Programme Heads are notified
        public async Task<CreateTakeOnRequestResponse> CreateAsync(Guid CreatorId, CreateTakeOnRequestRequest request)
        {
            var hasCase = request.CaseId != null && request.CaseId != Guid.Empty;
            var hasFlag = request.IndicatorFlagId != null && request.IndicatorFlagId != Guid.Empty;
            if (hasCase == hasFlag)
                throw new ValidationException("Choose either a case or an indicator flag.");

            var supportTypes = await GetSupportTypesAsync(CreatorId);
            string itemText;
            if (hasCase)
            {
                var caseRecord = await _caseRepository.GetByIdAsyncInclude(request.CaseId!.Value, c => c.SupportCategory, c => c.Student)
                    ?? throw new NotFoundException("Case not found.");
                if (!supportTypes.Contains(caseRecord.SupportCategory.SupportType))
                    throw new NotFoundException("Case not found.");
                if (caseRecord.Status == "closed")
                    throw new ConflictException("The case is closed.");
                if (caseRecord.AdvisorId != null || caseRecord.CurrentStage != "pending")
                    throw new ConflictException("The case already has an advisor.");
                if (await _takeOnRequestRepository.ExistsAsync(r => r.AdvisorId == CreatorId && r.CaseId == caseRecord.CaseId && r.Status == "pending"))
                    throw new ConflictException("You already have a pending request for this case.");
                itemText = $"the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}";
            }
            else
            {
                var flag = await _indicatorFlagRepository.FirstOrDefaultAsync(f => f.IndicatorFlagId == request.IndicatorFlagId!.Value,
                        f => f.Indicator.SupportCategory, f => f.Student)
                    ?? throw new NotFoundException("Indicator flag not found.");
                if (!supportTypes.Contains(flag.Indicator.SupportCategory.SupportType))
                    throw new NotFoundException("Indicator flag not found.");
                if (flag.Status != "open")
                    throw new ConflictException("This flag is no longer open.");
                if (await _takeOnRequestRepository.ExistsAsync(r => r.AdvisorId == CreatorId && r.IndicatorFlagId == flag.IndicatorFlagId && r.Status == "pending"))
                    throw new ConflictException("You already have a pending request for this flag.");
                itemText = $"{flag.Student.FullName} (flag '{flag.Indicator.IndicatorName}')";
            }

            var note = request.Note.Trim();
            var takeOnRequest = new Takeonrequest
            {
                TakeOnRequestId = Guid.NewGuid(),
                AdvisorId = CreatorId,
                CaseId = hasCase ? request.CaseId : null,
                IndicatorFlagId = hasFlag ? request.IndicatorFlagId : null,
                Note = note.Length == 0 ? null : note,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };
            await _takeOnRequestRepository.CreateAsync(takeOnRequest);

            var r = await GetRequestAsync(takeOnRequest.TakeOnRequestId);
            await _activityLogService.LogAsync(CreatorId, "takeOnRequested", $"Asked to take on {itemText}");
            foreach (var programmeHeadId in await GetActiveAccountIdsInRoleAsync("Programme Head"))
            {
                await _notificationService.NotifyAsync(programmeHeadId, "New take-on request",
                    $"{r.Advisor?.FullName} asked to take on {itemText}." + (note.Length == 0 ? string.Empty : $" Note: {note}"),
                    hasCase ? "/programme-head/matching-queue" : "/programme-head/students-in-need");
            }

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new CreateTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Update Take-on request: the advisor who sent it changes the note while it is pending
        public async Task<UpdateTakeOnRequestResponse> UpdateAsync(Guid CallerId, Guid TakeOnRequestId, UpdateTakeOnRequestRequest request)
        {
            var r = await GetRequestForAdvisorAsync(CallerId, TakeOnRequestId);
            if (r.Status != "pending")
                throw new ConflictException("Only a pending take-on request can be changed.");

            var note = request.Note.Trim();
            r.Note = note.Length == 0 ? null : note;
            await _takeOnRequestRepository.UpdateAsync(r);

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new UpdateTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Get the signed-in advisor's own Take-on requests ("My requests"), pending first, then newest first, optionally by status
        public async Task<List<GetMyTakeOnRequestResponse>> GetMyAllAsync(Guid AdvisorId, SearchMyTakeOnRequestRequest request)
        {
            var requests = await GetRequestsAsync(r => r.AdvisorId == AdvisorId &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            var openCaseCounts = await GetOpenCaseCountsAsync(requests);

            return requests.Select(r => new GetMyTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            }).ToList();
        }

        // Approve Take-on request: the Programme Head gives the item to the advisor (a case is assigned, or a case is opened from the flag);
        // the other pending requests for the item are cancelled and the advisors are notified by the assign / open-case step
        public async Task<ApproveTakeOnRequestResponse> ApproveAsync(Guid ProgrammeHeadId, Guid TakeOnRequestId)
        {
            var r = await GetRequestAsync(TakeOnRequestId);
            if (r.Status != "pending")
                throw new ConflictException("This take-on request has already been decided, withdrawn or cancelled.");

            // Staged here and saved by the assign / open-case save, so the request and the case change commit together
            r.DecidedBy = ProgrammeHeadId;
            r.DecidedAt = DateTime.UtcNow;
            r.Status = "approved";
            _takeOnRequestRepository.PrepareUpdate(r);

            var reason = $"Take-on request of {r.Advisor?.FullName} approved";
            if (r.CaseId != null)
            {
                await _caseService.AssignAsync(ProgrammeHeadId, r.CaseId.Value, new AssignCaseRequest { AdvisorId = r.AdvisorId, Reason = reason });
            }
            else
            {
                await _indicatorFlagService.OpenCaseAsync(ProgrammeHeadId, r.IndicatorFlagId!.Value, new OpenCaseFromIndicatorFlagRequest { AdvisorId = r.AdvisorId, Reason = reason });
            }

            r = await GetRequestAsync(TakeOnRequestId);
            await _activityLogService.LogAsync(ProgrammeHeadId, "takeOnApproved",
                $"Approved the take-on request of {r.Advisor?.FullName} for {DescribeItem(r)}");

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new ApproveTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Decline Take-on request: the Programme Head turns one request down with a reason; the case or flag stays open for others; the advisor is notified
        public async Task<DeclineTakeOnRequestResponse> DeclineAsync(Guid ProgrammeHeadId, Guid TakeOnRequestId, DeclineTakeOnRequestRequest request)
        {
            var r = await GetRequestAsync(TakeOnRequestId);
            if (r.Status != "pending")
                throw new ConflictException("This take-on request has already been decided, withdrawn or cancelled.");
            var reason = request.DeclineReason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Decline reason is required.");

            r.DecidedBy = ProgrammeHeadId;
            r.DeclineReason = reason;
            r.DecidedAt = DateTime.UtcNow;
            r.Status = "declined";
            await _takeOnRequestRepository.UpdateAsync(r);

            r = await GetRequestAsync(TakeOnRequestId);
            await _activityLogService.LogAsync(ProgrammeHeadId, "takeOnDeclined",
                $"Declined the take-on request of {r.Advisor?.FullName} for {DescribeItem(r)}: {reason}");
            await _notificationService.NotifyAsync(r.AdvisorId, "Take-on request declined",
                $"Your request to take on {DescribeItem(r)} was declined by {r.DecidedByNavigation?.FullName}: {reason}",
                "/advisor/students-in-need");

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new DeclineTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Withdraw Take-on request: the advisor who sent it takes back a pending request
        public async Task<WithdrawTakeOnRequestResponse> WithdrawAsync(Guid AdvisorId, Guid TakeOnRequestId)
        {
            var r = await GetRequestForAdvisorAsync(AdvisorId, TakeOnRequestId);
            if (r.Status != "pending")
                throw new ConflictException("Only a pending take-on request can be withdrawn.");

            r.DecidedAt = DateTime.UtcNow;
            r.Status = "withdrawn";
            await _takeOnRequestRepository.UpdateAsync(r);

            await _activityLogService.LogAsync(AdvisorId, "takeOnWithdrawn", $"Withdrew the take-on request for {DescribeItem(r)}");

            var openCaseCounts = await GetOpenCaseCountsAsync(new List<Takeonrequest> { r });

            return new WithdrawTakeOnRequestResponse
            {
                TakeOnRequestId = r.TakeOnRequestId,
                AdvisorId = r.AdvisorId,
                CaseId = r.CaseId,
                DecidedBy = r.DecidedBy,
                IndicatorFlagId = r.IndicatorFlagId,
                StudentId = r.Case?.StudentId ?? r.IndicatorFlag!.StudentId,
                AdvisorEmail = r.Advisor?.Email ?? string.Empty,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case?.SupportCategory?.CategoryName ?? r.IndicatorFlag?.Indicator?.SupportCategory?.CategoryName ?? string.Empty,
                DecidedByFullName = r.DecidedByNavigation?.FullName ?? string.Empty,
                DeclineReason = r.DeclineReason ?? string.Empty,
                IndicatorName = r.IndicatorFlag?.Indicator?.IndicatorName ?? string.Empty,
                Note = r.Note ?? string.Empty,
                StudentCode = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = (r.Case?.Student ?? r.IndicatorFlag?.Student)?.FullName ?? string.Empty,
                SubjectCode = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectCode ?? string.Empty,
                SubjectName = (r.Case?.Subject ?? r.IndicatorFlag?.Subject)?.SubjectName ?? string.Empty,
                TermName = r.IndicatorFlag?.Term?.TermName ?? string.Empty,
                AdvisorOpenCaseCount = openCaseCounts.GetValueOrDefault(r.AdvisorId),
                CreatedAt = r.CreatedAt,
                DecidedAt = r.DecidedAt,
                Priority = r.Case?.Priority ?? r.IndicatorFlag!.Priority,
                Severity = r.Case != null ? r.Case.Severity : r.IndicatorFlag!.Severity,
                SupportType = r.Case?.SupportCategory?.SupportType ?? r.IndicatorFlag?.Indicator?.SupportCategory?.SupportType ?? string.Empty,
                TargetType = r.CaseId != null ? "case" : "flag",
                Status = r.Status
            };
        }

        // Helper: load Take-on requests with the advisor, the decider and the item (case or flag with its student, category, subject and term),
        // pending first, then newest first
        private async Task<List<Takeonrequest>> GetRequestsAsync(Expression<Func<Takeonrequest, bool>> Predicate)
        {
            return (await _takeOnRequestRepository.SearchAsyncInclude(Predicate,
                    r => r.Advisor, r => r.DecidedByNavigation,
                    r => r.Case.Student.Studentprofile, r => r.Case.SupportCategory, r => r.Case.Subject,
                    r => r.IndicatorFlag.Student.Studentprofile, r => r.IndicatorFlag.Indicator.SupportCategory, r => r.IndicatorFlag.Subject, r => r.IndicatorFlag.Term))
                .OrderBy(r => r.Status == "pending" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
        }

        // Helper: load one Take-on request with its details
        private async Task<Takeonrequest> GetRequestAsync(Guid TakeOnRequestId)
        {
            return (await GetRequestsAsync(r => r.TakeOnRequestId == TakeOnRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Take-on request not found.");
        }

        // Helper: one of the advisor's own Take-on requests (anyone else's is "not found")
        private async Task<Takeonrequest> GetRequestForAdvisorAsync(Guid AdvisorId, Guid TakeOnRequestId)
        {
            var r = await GetRequestAsync(TakeOnRequestId);
            if (r.AdvisorId != AdvisorId)
                throw new NotFoundException("Take-on request not found.");
            return r;
        }

        // Helper: how many cases that are not closed each advisor of the requests holds
        private async Task<Dictionary<Guid, int>> GetOpenCaseCountsAsync(List<Takeonrequest> Requests)
        {
            var advisorIds = Requests.Select(r => r.AdvisorId).Distinct().ToList();
            return (await _caseRepository.SearchAsync(c => c.AdvisorId != null && advisorIds.Contains(c.AdvisorId.Value) && c.Status != "closed"))
                .GroupBy(c => c.AdvisorId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // Helper: the support types an advisor handles, from their advisor roles
        private async Task<HashSet<string>> GetSupportTypesAsync(Guid AdvisorId)
        {
            var supportTypes = new HashSet<string>();
            if (await HasRoleAsync(AdvisorId, "Academic Advisor"))
                supportTypes.Add("academic");
            if (await HasRoleAsync(AdvisorId, "Wellbeing Advisor"))
                supportTypes.Add("wellbeing");
            return supportTypes;
        }

        // Helper: does an account have a role, by name
        private async Task<bool> HasRoleAsync(Guid AccountId, string RoleName)
        {
            return await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == AccountId && ar.Role.RoleName == RoleName);
        }

        // Helper: the ids of the active accounts that have a role, by name
        private async Task<List<Guid>> GetActiveAccountIdsInRoleAsync(string RoleName)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.Role.RoleName == RoleName && ar.Account.Status == "active", ar => ar.Account))
                .Select(ar => ar.AccountId)
                .Distinct()
                .ToList();
        }

        // Helper: the item of a request in words, e.g. "the Study plan case of Le Van An" or "Le Van An (flag 'Low attendance')"
        private static string DescribeItem(Takeonrequest Request)
        {
            return Request.CaseId != null
                ? $"the {Request.Case?.SupportCategory?.CategoryName} case of {Request.Case?.Student?.FullName}"
                : $"{Request.IndicatorFlag?.Student?.FullName} (flag '{Request.IndicatorFlag?.Indicator?.IndicatorName}')";
        }
    }
}
