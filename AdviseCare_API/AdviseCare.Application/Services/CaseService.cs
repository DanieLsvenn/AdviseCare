using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Contracts.Requests.Consent;
using AdviseCare.Application.Contracts.Responses.Case;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Cases: support requests by students (WF2), cases opened by advisors (EX3), matching by the Programme Head (WF2 step 3),
    // the advisor's case workflow (stages, close, reopen, priority) and the case stage history (CASESTAGEHISTORIES, insert-only).
    // CASES.advisorId is the only advisor-student link: an advisor advises a student while holding one of their cases that is not closed.
    // A case change and its stage history row are staged first and saved together with one SaveAsync.
    public class CaseService : ICaseService
    {
        private static readonly string[] Stages = { "pending", "assigned", "contacted", "referred", "followUp", "closed" };

        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Casestagehistory> _caseStageHistoryRepository;
        private readonly IGenericRepository<Supportcategory> _supportCategoryRepository;
        private readonly IGenericRepository<Subject> _subjectRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<AccountSubject> _accountSubjectRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly IGenericRepository<Notification> _notificationRepository;
        private readonly IConsentService _consentService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public CaseService(
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Casestagehistory> caseStageHistoryRepository,
            IGenericRepository<Supportcategory> supportCategoryRepository,
            IGenericRepository<Subject> subjectRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<AccountSubject> accountSubjectRepository,
            IGenericRepository<Faculty> facultyRepository,
            IGenericRepository<Referral> referralRepository,
            IGenericRepository<Notification> notificationRepository,
            IConsentService consentService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _caseRepository = caseRepository;
            _caseStageHistoryRepository = caseStageHistoryRepository;
            _supportCategoryRepository = supportCategoryRepository;
            _subjectRepository = subjectRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _accountSubjectRepository = accountSubjectRepository;
            _facultyRepository = facultyRepository;
            _referralRepository = referralRepository;
            _notificationRepository = notificationRepository;
            _consentService = consentService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Cases, newest first (Programme Head)
        public async Task<IEnumerable<GetAllCaseResponse>> GetAll()
        {
            var cases = await GetCasesAsync(c => true);

            return cases.Select(c => new GetAllCaseResponse
            {
                CaseId = c.CaseId,
                AdvisorId = c.AdvisorId,
                ConsentPurposeId = c.SupportCategory.ConsentPurposeId,
                OpenedBy = c.OpenedBy,
                StudentId = c.StudentId,
                SubjectId = c.SubjectId,
                SupportCategoryId = c.SupportCategoryId,
                AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                CategoryName = c.SupportCategory.CategoryName,
                OpenedByFullName = c.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = c.RequestReason,
                StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = c.Student.Email,
                StudentFullName = c.Student.FullName,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                SubjectName = c.Subject?.SubjectName ?? string.Empty,
                ClosedAt = c.ClosedAt,
                OpenedAt = c.OpenedAt,
                SupportType = c.SupportCategory.SupportType,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            });
        }

        // Get all Cases with pagination
        public async Task<IEnumerable<GetAllCaseWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var cases = await GetCasesAsync(c => true);

            return cases
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new GetAllCaseWithPaginationResponse
                {
                    CaseId = c.CaseId,
                    AdvisorId = c.AdvisorId,
                    ConsentPurposeId = c.SupportCategory.ConsentPurposeId,
                    OpenedBy = c.OpenedBy,
                    StudentId = c.StudentId,
                    SubjectId = c.SubjectId,
                    SupportCategoryId = c.SupportCategoryId,
                    AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                    AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                    CategoryName = c.SupportCategory.CategoryName,
                    OpenedByFullName = c.OpenedByNavigation?.FullName ?? string.Empty,
                    RequestReason = c.RequestReason,
                    StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                    StudentEmail = c.Student.Email,
                    StudentFullName = c.Student.FullName,
                    SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = c.Subject?.SubjectName ?? string.Empty,
                    ClosedAt = c.ClosedAt,
                    OpenedAt = c.OpenedAt,
                    SupportType = c.SupportCategory.SupportType,
                    CurrentStage = c.CurrentStage,
                    Priority = c.Priority,
                    Status = c.Status
                });
        }

        // Get Case by Id (the student, the current advisor, the Programme Head, or anyone who sent or received a referral of it);
        // opening a case marks the caller's notifications that point to it as read
        public async Task<GetCaseByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseId)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            await EnsureCanViewCaseAsync(caseRecord, CallerId);

            var redirectUrl = $"/cases/{CaseId}";
            var unread = await _notificationRepository.SearchAsync(n => n.AccountId == CallerId && !n.IsRead && n.RedirectUrl == redirectUrl);
            if (unread.Count > 0)
            {
                foreach (var notification in unread)
                    notification.IsRead = true;
                await _notificationRepository.SaveAsync();
            }

            return new GetCaseByIdResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                ConsentPurposeId = caseRecord.SupportCategory.ConsentPurposeId,
                OpenedBy = caseRecord.OpenedBy,
                StudentId = caseRecord.StudentId,
                SubjectId = caseRecord.SubjectId,
                SupportCategoryId = caseRecord.SupportCategoryId,
                AdvisorEmail = caseRecord.Advisor?.Email ?? string.Empty,
                AdvisorFullName = caseRecord.Advisor?.FullName ?? string.Empty,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                OpenedByFullName = caseRecord.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = caseRecord.RequestReason,
                StudentCode = caseRecord.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = caseRecord.Student.Email,
                StudentFullName = caseRecord.Student.FullName,
                SubjectCode = caseRecord.Subject?.SubjectCode ?? string.Empty,
                SubjectName = caseRecord.Subject?.SubjectName ?? string.Empty,
                ClosedAt = caseRecord.ClosedAt,
                OpenedAt = caseRecord.OpenedAt,
                SupportType = caseRecord.SupportCategory.SupportType,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Search Cases by student, advisor, category, subject, support type, stage, priority and status, newest first (Programme Head)
        public async Task<List<SearchCaseResponse>> SearchAsync(SearchCaseRequest request)
        {
            var cases = await GetCasesAsync(c =>
                (request.AdvisorId == null || c.AdvisorId == request.AdvisorId) &&
                (request.StudentId == null || c.StudentId == request.StudentId) &&
                (request.SubjectId == null || c.SubjectId == request.SubjectId) &&
                (request.SupportCategoryId == null || c.SupportCategoryId == request.SupportCategoryId) &&
                (string.IsNullOrWhiteSpace(request.SupportType) || c.SupportCategory.SupportType == request.SupportType) &&
                (string.IsNullOrWhiteSpace(request.CurrentStage) || c.CurrentStage == request.CurrentStage) &&
                (string.IsNullOrWhiteSpace(request.Priority) || c.Priority == request.Priority) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status));

            return cases.Select(c => new SearchCaseResponse
            {
                CaseId = c.CaseId,
                AdvisorId = c.AdvisorId,
                ConsentPurposeId = c.SupportCategory.ConsentPurposeId,
                OpenedBy = c.OpenedBy,
                StudentId = c.StudentId,
                SubjectId = c.SubjectId,
                SupportCategoryId = c.SupportCategoryId,
                AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                CategoryName = c.SupportCategory.CategoryName,
                OpenedByFullName = c.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = c.RequestReason,
                StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = c.Student.Email,
                StudentFullName = c.Student.FullName,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                SubjectName = c.Subject?.SubjectName ?? string.Empty,
                ClosedAt = c.ClosedAt,
                OpenedAt = c.OpenedAt,
                SupportType = c.SupportCategory.SupportType,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();
        }

        // Create Case:
        // - a student requests support (WF2): stage pending, priority medium, no advisor; the chosen data attributes of the category's purpose
        //   are granted as consents in the same save; the Programme Heads are notified
        // - an advisor opens a case for a student (EX3): stage assigned with the advisor; the student is notified with the advisor's contact information
        public async Task<CreateCaseResponse> CreateAsync(Guid CreatorId, CreateCaseRequest request)
        {
            var creatorRoles = await GetRoleNamesAsync(CreatorId);
            var category = await _supportCategoryRepository.GetByIdAsync(request.SupportCategoryId)
                ?? throw new NotFoundException("Support category not found.");
            if (category.Status != "active")
                throw new ValidationException($"The support category '{category.CategoryName}' is archived.");

            var requestReason = request.RequestReason.Trim();
            if (requestReason.Length == 0)
                throw new ValidationException("Request reason is required.");

            Subject? subject = null;
            if (request.SubjectId != null && request.SubjectId != Guid.Empty)
            {
                if (category.SupportType != "academic")
                    throw new ValidationException("Only academic requests can name a subject.");
                subject = await _subjectRepository.GetByIdAsync(request.SubjectId.Value)
                    ?? throw new NotFoundException("Subject not found.");
                if (subject.Status != "active")
                    throw new ValidationException($"The subject {subject.SubjectCode} is archived.");
            }

            var isOutreach = request.StudentId != null && request.StudentId != Guid.Empty;
            var sharedAttributeIds = request.SharedAttributeIds.Where(id => id != Guid.Empty).Distinct().ToList();
            Account student;
            string priority;

            if (isOutreach)
            {
                var advisorRole = AdvisorRoleFor(category.SupportType);
                if (!creatorRoles.Contains(advisorRole))
                    throw new ForbiddenException($"Only a {advisorRole} can open a {category.SupportType} case for a student.");
                if (sharedAttributeIds.Count > 0)
                    throw new ValidationException("Shared data is chosen by the student. Leave the shared attributes empty when an advisor opens a case.");

                student = await _accountRepository.GetByIdAsync(request.StudentId!.Value)
                    ?? throw new NotFoundException("Student not found.");
                if (!(await GetRoleNamesAsync(student.AccountId)).Contains("Student"))
                    throw new ValidationException($"{student.FullName} is not a student.");
                if (student.Status != "active")
                    throw new ValidationException($"{student.FullName} is not an active account.");
                priority = string.IsNullOrWhiteSpace(request.Priority) ? "medium" : request.Priority;
            }
            else
            {
                if (!creatorRoles.Contains("Student"))
                    throw new ForbiddenException("Only students can request support. An advisor must give the student to open a case for them.");

                student = await _accountRepository.GetByIdAsync(CreatorId)
                    ?? throw new NotFoundException("Account not found.");
                priority = "medium";

                if (sharedAttributeIds.Count > 0)
                {
                    // Checked before anything is staged (the consent service checks again when it saves)
                    var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(category.ConsentPurposeId, p => p.DataAttributes)
                        ?? throw new NotFoundException("The consent purpose of this category was not found.");
                    if (purpose.Status != "active")
                        throw new ValidationException($"Data cannot be shared for '{category.CategoryName}' right now because '{purpose.PurposeName}' is not active. Submit the request without shared data.");
                    var allowedIds = purpose.DataAttributes.Where(a => a.Status == "active").Select(a => a.DataAttributeId).ToHashSet();
                    if (sharedAttributeIds.Any(id => !allowedIds.Contains(id)))
                        throw new ValidationException($"Only the data attributes of '{purpose.PurposeName}' can be shared with a '{category.CategoryName}' request.");
                }
            }

            var now = DateTime.UtcNow;
            var caseRecord = new Case
            {
                CaseId = Guid.NewGuid(),
                AdvisorId = isOutreach ? CreatorId : null,
                OpenedBy = CreatorId,
                StudentId = student.AccountId,
                SubjectId = subject?.SubjectId,
                SupportCategoryId = category.SupportCategoryId,
                RequestReason = requestReason,
                OpenedAt = now,
                CurrentStage = "pending",
                Priority = priority,
                Status = "open"
            };
            _caseRepository.PrepareCreate(caseRecord);
            AddStageHistory(caseRecord, CreatorId, isOutreach ? "assigned" : "pending",
                isOutreach ? "Case opened by the advisor" : "Support requested by the student", now);

            if (sharedAttributeIds.Count > 0)
            {
                // Saves the staged case and its history together with the consent rows (same DbContext, one save)
                await _consentService.CreateAsync(student.AccountId, new CreateConsentRequest
                {
                    ConsentPurposeId = category.ConsentPurposeId,
                    DataAttributeIds = sharedAttributeIds
                });
            }
            await _caseRepository.SaveAsync();

            var subjectText = subject != null ? $" for {subject.SubjectCode}" : string.Empty;
            if (isOutreach)
            {
                var advisor = await _accountRepository.GetByIdAsync(CreatorId);
                await _activityLogService.LogAsync(CreatorId, "caseOpened", $"Opened a {category.CategoryName} case{subjectText} for {student.FullName}");
                await _notificationService.NotifyAsync(student.AccountId, "An advisor would like to meet you",
                    $"{advisor?.FullName} ({advisor?.Email}) opened a {category.CategoryName} case for you and would like to meet. Please contact them to arrange a meeting.",
                    $"/cases/{caseRecord.CaseId}");
            }
            else
            {
                await _activityLogService.LogAsync(CreatorId, "supportRequested",
                    $"Requested {category.SupportType} support ({category.CategoryName}){subjectText}, sharing {sharedAttributeIds.Count} data attribute(s)");
                foreach (var programmeHead in await GetActiveAccountsInRoleAsync("Programme Head"))
                {
                    await _notificationService.NotifyAsync(programmeHead.AccountId, "New support request",
                        $"{student.FullName} requested {category.SupportType} support ({category.CategoryName}{subjectText}). The case is waiting in the matching queue.",
                        $"/cases/{caseRecord.CaseId}");
                }
            }

            var created = await GetCaseAsync(caseRecord.CaseId);

            return new CreateCaseResponse
            {
                CaseId = created.CaseId,
                AdvisorId = created.AdvisorId,
                ConsentPurposeId = created.SupportCategory.ConsentPurposeId,
                OpenedBy = created.OpenedBy,
                StudentId = created.StudentId,
                SubjectId = created.SubjectId,
                SupportCategoryId = created.SupportCategoryId,
                AdvisorEmail = created.Advisor?.Email ?? string.Empty,
                AdvisorFullName = created.Advisor?.FullName ?? string.Empty,
                CategoryName = created.SupportCategory.CategoryName,
                OpenedByFullName = created.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = created.RequestReason,
                StudentCode = created.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = created.Student.Email,
                StudentFullName = created.Student.FullName,
                SubjectCode = created.Subject?.SubjectCode ?? string.Empty,
                SubjectName = created.Subject?.SubjectName ?? string.Empty,
                ClosedAt = created.ClosedAt,
                OpenedAt = created.OpenedAt,
                SupportType = created.SupportCategory.SupportType,
                CurrentStage = created.CurrentStage,
                Priority = created.Priority,
                Status = created.Status
            };
        }

        // Get all of the signed-in student's own Cases, newest first, optionally by stage and status
        public async Task<List<GetMyCaseResponse>> GetMyAllAsync(Guid StudentId, SearchMyCaseRequest request)
        {
            var cases = await GetCasesAsync(c =>
                c.StudentId == StudentId &&
                (string.IsNullOrWhiteSpace(request.CurrentStage) || c.CurrentStage == request.CurrentStage) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status));

            return cases.Select(c => new GetMyCaseResponse
            {
                CaseId = c.CaseId,
                AdvisorId = c.AdvisorId,
                ConsentPurposeId = c.SupportCategory.ConsentPurposeId,
                OpenedBy = c.OpenedBy,
                StudentId = c.StudentId,
                SubjectId = c.SubjectId,
                SupportCategoryId = c.SupportCategoryId,
                AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                CategoryName = c.SupportCategory.CategoryName,
                OpenedByFullName = c.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = c.RequestReason,
                StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = c.Student.Email,
                StudentFullName = c.Student.FullName,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                SubjectName = c.Subject?.SubjectName ?? string.Empty,
                ClosedAt = c.ClosedAt,
                OpenedAt = c.OpenedAt,
                SupportType = c.SupportCategory.SupportType,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();
        }

        // Get the matching queue: Cases without an advisor that are not closed, most urgent first, then oldest first (Programme Head)
        public async Task<List<GetMatchingQueueCaseResponse>> GetMatchingQueueAsync()
        {
            var cases = (await GetCasesAsync(c => c.AdvisorId == null && c.Status != "closed"))
                .OrderByDescending(c => PriorityRank(c.Priority))
                .ThenBy(c => c.OpenedAt);

            return cases.Select(c => new GetMatchingQueueCaseResponse
            {
                CaseId = c.CaseId,
                AdvisorId = c.AdvisorId,
                ConsentPurposeId = c.SupportCategory.ConsentPurposeId,
                OpenedBy = c.OpenedBy,
                StudentId = c.StudentId,
                SubjectId = c.SubjectId,
                SupportCategoryId = c.SupportCategoryId,
                AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                CategoryName = c.SupportCategory.CategoryName,
                OpenedByFullName = c.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = c.RequestReason,
                StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = c.Student.Email,
                StudentFullName = c.Student.FullName,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                SubjectName = c.Subject?.SubjectName ?? string.Empty,
                ClosedAt = c.ClosedAt,
                OpenedAt = c.OpenedAt,
                SupportType = c.SupportCategory.SupportType,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();
        }

        // Get suggested advisors for a Case: active advisors of the case's type, ordered by an advisor already handling another open case
        // of the student, subject specialists, the student's faculty (general advisors first), then the fewest open cases
        public async Task<List<GetCaseSuggestedAdvisorResponse>> GetSuggestedAdvisorsAsync(Guid CaseId)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            var supportType = caseRecord.SupportCategory.SupportType;
            var candidates = await GetActiveAccountsInRoleAsync(AdvisorRoleFor(supportType));
            if (candidates.Count == 0)
                return new List<GetCaseSuggestedAdvisorResponse>();

            var candidateIds = candidates.Select(a => a.AccountId).ToList();
            var subjectLinks = await _accountSubjectRepository.SearchAsyncInclude(s => candidateIds.Contains(s.AccountId), s => s.Subject);
            var openCases = await _caseRepository.SearchAsync(c => c.AdvisorId != null && candidateIds.Contains(c.AdvisorId.Value) && c.Status != "closed");
            var familiarAdvisorIds = openCases
                .Where(c => c.StudentId == caseRecord.StudentId && c.CaseId != caseRecord.CaseId)
                .Select(c => c.AdvisorId!.Value)
                .ToHashSet();
            var facultyIds = candidates.Where(a => a.FacultyId != null).Select(a => a.FacultyId!.Value).Distinct().ToList();
            var facultyNames = (await _facultyRepository.SearchAsync(f => facultyIds.Contains(f.FacultyId))).ToDictionary(f => f.FacultyId, f => f.FacultyName);
            var isAcademic = supportType == "academic";

            var suggestions = candidates.Select(advisor =>
            {
                var links = subjectLinks.Where(s => s.AccountId == advisor.AccountId).ToList();
                var isFamiliarAdvisor = familiarAdvisorIds.Contains(advisor.AccountId);
                var isSubjectSpecialist = isAcademic && caseRecord.SubjectId != null && links.Any(s => s.SubjectId == caseRecord.SubjectId);
                var isGeneralAdvisor = isAcademic && links.Count == 0;
                var isSameFaculty = caseRecord.Student.FacultyId != null && advisor.FacultyId == caseRecord.Student.FacultyId;
                var openCaseCount = openCases.Count(c => c.AdvisorId == advisor.AccountId);

                var matchReasons = new List<string>();
                if (isFamiliarAdvisor) matchReasons.Add("Already handles another open case of this student");
                if (isSubjectSpecialist) matchReasons.Add($"Specialised in {caseRecord.Subject?.SubjectCode}");
                if (isGeneralAdvisor) matchReasons.Add("General academic advisor");
                if (isSameFaculty) matchReasons.Add("Same faculty as the student");
                matchReasons.Add($"{openCaseCount} open case(s)");

                return new GetCaseSuggestedAdvisorResponse
                {
                    AdvisorId = advisor.AccountId,
                    FacultyId = advisor.FacultyId,
                    Email = advisor.Email,
                    FacultyName = advisor.FacultyId != null ? facultyNames.GetValueOrDefault(advisor.FacultyId.Value) ?? string.Empty : string.Empty,
                    FullName = advisor.FullName,
                    MatchReasons = matchReasons,
                    SpecialisedSubjectCodes = links.Select(s => s.Subject?.SubjectCode ?? string.Empty).OrderBy(code => code).ToList(),
                    OpenCaseCount = openCaseCount,
                    IsFamiliarAdvisor = isFamiliarAdvisor,
                    IsGeneralAdvisor = isGeneralAdvisor,
                    IsSameFaculty = isSameFaculty,
                    IsSubjectSpecialist = isSubjectSpecialist
                };
            });

            return suggestions
                .OrderByDescending(s => s.IsFamiliarAdvisor)
                .ThenByDescending(s => s.IsSubjectSpecialist)
                .ThenByDescending(s => s.IsSameFaculty)
                .ThenByDescending(s => s.IsGeneralAdvisor)
                .ThenBy(s => s.OpenCaseCount)
                .ThenBy(s => s.FullName)
                .ToList();
        }

        // Assign Case: the Programme Head matches an unassigned case to an advisor of its type (pending -> assigned);
        // the student gets the advisor's contact information and the advisor is notified
        public async Task<AssignCaseResponse> AssignAsync(Guid ProgrammeHeadId, Guid CaseId, AssignCaseRequest request)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId != null || caseRecord.CurrentStage != "pending")
                throw new ConflictException("The case already has an advisor. Use reassign or a case change request to change it.");

            var advisor = await GetActiveAdvisorAsync(request.AdvisorId, AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Matched by the Programme Head" : request.Reason.Trim();

            caseRecord.AdvisorId = advisor.AccountId;
            caseRecord.Advisor = advisor;
            AddStageHistory(caseRecord, ProgrammeHeadId, "assigned", reason, DateTime.UtcNow);
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(ProgrammeHeadId, "caseAssigned",
                $"Assigned the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to {advisor.FullName}");
            await NotifyMatchAsync(caseRecord, advisor, false);

            return new AssignCaseResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                StudentId = caseRecord.StudentId,
                AdvisorEmail = advisor.Email,
                AdvisorFullName = advisor.FullName,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                StudentFullName = caseRecord.Student.FullName,
                ClosedAt = caseRecord.ClosedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Reassign Case: the Programme Head moves a case to another advisor of its type directly; the stage is kept,
        // the hand-over is written to the stage history, and both advisors and the student are notified
        public async Task<ReassignCaseResponse> ReassignAsync(Guid ProgrammeHeadId, Guid CaseId, ReassignCaseRequest request)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId == null)
                throw new ConflictException("The case has no advisor yet. Use assign instead.");
            if (caseRecord.AdvisorId == request.AdvisorId)
                throw new ValidationException("The case is already handled by this advisor.");
            if (await HasPendingReferralAsync(CaseId))
                throw new ConflictException("A referral of this case is waiting for an answer. Reassign it after the referral is accepted or returned.");

            var advisor = await GetActiveAdvisorAsync(request.AdvisorId, AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");
            var oldAdvisor = caseRecord.Advisor;

            caseRecord.AdvisorId = advisor.AccountId;
            caseRecord.Advisor = advisor;
            AddStageHistory(caseRecord, ProgrammeHeadId, caseRecord.CurrentStage, $"Advisor changed from {oldAdvisor?.FullName ?? "no advisor"} to {advisor.FullName}: {reason}", DateTime.UtcNow);
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(ProgrammeHeadId, "caseReassigned",
                $"Moved the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} from {oldAdvisor?.FullName} to {advisor.FullName}: {reason}");
            await NotifyMatchAsync(caseRecord, advisor, true);
            if (oldAdvisor != null)
            {
                await _notificationService.NotifyAsync(oldAdvisor.AccountId, "Case moved to another advisor",
                    $"The {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} is now handled by {advisor.FullName}.",
                    $"/cases/{CaseId}");
            }

            return new ReassignCaseResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                StudentId = caseRecord.StudentId,
                AdvisorEmail = advisor.Email,
                AdvisorFullName = advisor.FullName,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                StudentFullName = caseRecord.Student.FullName,
                ClosedAt = caseRecord.ClosedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Get the signed-in advisor's assigned Cases, filtered and sorted; each case carries the advisor's unread notifications that point to it
        public async Task<List<GetAssignedCaseResponse>> GetAssignedAsync(Guid AdvisorId, SearchAssignedCaseRequest request)
        {
            var assigned = await GetAssignedCasesAsync(AdvisorId, request);

            return assigned.Select(a => new GetAssignedCaseResponse
            {
                CaseId = a.Case.CaseId,
                AdvisorId = a.Case.AdvisorId,
                ConsentPurposeId = a.Case.SupportCategory.ConsentPurposeId,
                OpenedBy = a.Case.OpenedBy,
                StudentId = a.Case.StudentId,
                SubjectId = a.Case.SubjectId,
                SupportCategoryId = a.Case.SupportCategoryId,
                AdvisorEmail = a.Case.Advisor?.Email ?? string.Empty,
                AdvisorFullName = a.Case.Advisor?.FullName ?? string.Empty,
                CategoryName = a.Case.SupportCategory.CategoryName,
                OpenedByFullName = a.Case.OpenedByNavigation?.FullName ?? string.Empty,
                RequestReason = a.Case.RequestReason,
                StudentCode = a.Case.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentEmail = a.Case.Student.Email,
                StudentFullName = a.Case.Student.FullName,
                SubjectCode = a.Case.Subject?.SubjectCode ?? string.Empty,
                SubjectName = a.Case.Subject?.SubjectName ?? string.Empty,
                ClosedAt = a.Case.ClosedAt,
                OpenedAt = a.Case.OpenedAt,
                UnreadNotificationCount = a.UnreadNotificationCount,
                SupportType = a.Case.SupportCategory.SupportType,
                CurrentStage = a.Case.CurrentStage,
                Priority = a.Case.Priority,
                Status = a.Case.Status
            }).ToList();
        }

        // Get the signed-in advisor's assigned Cases with pagination
        public async Task<IEnumerable<GetAssignedCaseWithPaginationResponse>> GetAssignedWithPaginationAsync(Guid AdvisorId, SearchAssignedCaseRequest request, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var assigned = await GetAssignedCasesAsync(AdvisorId, request);

            return assigned
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new GetAssignedCaseWithPaginationResponse
                {
                    CaseId = a.Case.CaseId,
                    AdvisorId = a.Case.AdvisorId,
                    ConsentPurposeId = a.Case.SupportCategory.ConsentPurposeId,
                    OpenedBy = a.Case.OpenedBy,
                    StudentId = a.Case.StudentId,
                    SubjectId = a.Case.SubjectId,
                    SupportCategoryId = a.Case.SupportCategoryId,
                    AdvisorEmail = a.Case.Advisor?.Email ?? string.Empty,
                    AdvisorFullName = a.Case.Advisor?.FullName ?? string.Empty,
                    CategoryName = a.Case.SupportCategory.CategoryName,
                    OpenedByFullName = a.Case.OpenedByNavigation?.FullName ?? string.Empty,
                    RequestReason = a.Case.RequestReason,
                    StudentCode = a.Case.Student.Studentprofile?.StudentCode ?? string.Empty,
                    StudentEmail = a.Case.Student.Email,
                    StudentFullName = a.Case.Student.FullName,
                    SubjectCode = a.Case.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = a.Case.Subject?.SubjectName ?? string.Empty,
                    ClosedAt = a.Case.ClosedAt,
                    OpenedAt = a.Case.OpenedAt,
                    UnreadNotificationCount = a.UnreadNotificationCount,
                    SupportType = a.Case.SupportCategory.SupportType,
                    CurrentStage = a.Case.CurrentStage,
                    Priority = a.Case.Priority,
                    Status = a.Case.Status
                });
        }

        // Get how many of the signed-in advisor's assigned Cases have unread notifications
        public async Task<int> GetAssignedUnreadCountAsync(Guid AdvisorId)
        {
            var caseUrls = (await _caseRepository.SearchAsync(c => c.AdvisorId == AdvisorId))
                .Select(c => $"/cases/{c.CaseId}")
                .ToHashSet();
            var unread = await _notificationRepository.SearchAsync(n => n.AccountId == AdvisorId && !n.IsRead && n.RedirectUrl != null && n.RedirectUrl.StartsWith("/cases/"));

            return unread.Where(n => caseUrls.Contains(n.RedirectUrl!)).Select(n => n.RedirectUrl).Distinct().Count();
        }

        // Change Case stage: the case's advisor moves it to contacted, followUp or closed with a reason (written to the stage history);
        // closing sets status = closed and closedAt and notifies the student. Refused while a referral of the case is pending
        public async Task<ChangeCaseStageResponse> ChangeStageAsync(Guid AdvisorId, Guid CaseId, ChangeCaseStageRequest request)
        {
            var caseRecord = await GetCaseForAdvisorAsync(AdvisorId, CaseId);
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed. Reopen it first.");
            if (caseRecord.CurrentStage == request.ToStage)
                throw new ValidationException($"The case is already at stage {request.ToStage}.");
            if (await HasPendingReferralAsync(CaseId))
                throw new ConflictException("A referral of this case is waiting for an answer.");

            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");

            var now = DateTime.UtcNow;
            var fromStage = caseRecord.CurrentStage;
            AddStageHistory(caseRecord, AdvisorId, request.ToStage, reason, now);
            if (request.ToStage == "closed")
            {
                caseRecord.Status = "closed";
                caseRecord.ClosedAt = now;
            }
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            if (request.ToStage == "closed")
            {
                await _activityLogService.LogAsync(AdvisorId, "caseClosed", $"Closed the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}: {reason}");
                await _notificationService.NotifyAsync(caseRecord.StudentId, "Your case has been closed",
                    $"Your {caseRecord.SupportCategory.CategoryName} case was closed by {caseRecord.Advisor?.FullName}: {reason}",
                    $"/cases/{CaseId}");
            }
            else
            {
                await _activityLogService.LogAsync(AdvisorId, "caseStageChanged",
                    $"Moved the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} from {fromStage} to {request.ToStage}");
            }

            return new ChangeCaseStageResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                StudentId = caseRecord.StudentId,
                AdvisorEmail = caseRecord.Advisor?.Email ?? string.Empty,
                AdvisorFullName = caseRecord.Advisor?.FullName ?? string.Empty,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                StudentFullName = caseRecord.Student.FullName,
                ClosedAt = caseRecord.ClosedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Reopen Case: the case's advisor reopens a closed case (status = reopened, stage closed -> assigned, closedAt cleared); the student is notified
        public async Task<ReopenCaseResponse> ReopenAsync(Guid AdvisorId, Guid CaseId, ReopenCaseRequest request)
        {
            var caseRecord = await GetCaseForAdvisorAsync(AdvisorId, CaseId);
            if (caseRecord.Status != "closed")
                throw new ConflictException("Only a closed case can be reopened.");

            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");

            AddStageHistory(caseRecord, AdvisorId, "assigned", reason, DateTime.UtcNow);
            caseRecord.Status = "reopened";
            caseRecord.ClosedAt = null;
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(AdvisorId, "caseReopened", $"Reopened the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}: {reason}");
            await _notificationService.NotifyAsync(caseRecord.StudentId, "Your case has been reopened",
                $"{caseRecord.Advisor?.FullName} reopened your {caseRecord.SupportCategory.CategoryName} case: {reason}",
                $"/cases/{CaseId}");

            return new ReopenCaseResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                StudentId = caseRecord.StudentId,
                AdvisorEmail = caseRecord.Advisor?.Email ?? string.Empty,
                AdvisorFullName = caseRecord.Advisor?.FullName ?? string.Empty,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                StudentFullName = caseRecord.Student.FullName,
                ClosedAt = caseRecord.ClosedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Change Case priority: the case's advisor, the Programme Head, or a Welfare Officer who received a referral of the case
        public async Task<ChangeCasePriorityResponse> ChangePriorityAsync(Guid CallerId, Guid CaseId, ChangeCasePriorityRequest request)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            var callerRoles = await EnsureCanViewCaseAsync(caseRecord, CallerId);

            var isAllowed = caseRecord.AdvisorId == CallerId ||
                            callerRoles.Contains("Programme Head") ||
                            (callerRoles.Contains("Student Welfare Officer") && await _referralRepository.ExistsAsync(r => r.CaseId == CaseId && r.ToAccountId == CallerId));
            if (!isAllowed)
                throw new ForbiddenException("You cannot change the priority of this case.");
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");

            if (caseRecord.Priority != request.Priority)
            {
                var oldPriority = caseRecord.Priority;
                caseRecord.Priority = request.Priority;
                _caseRepository.PrepareUpdate(caseRecord);
                await _caseRepository.SaveAsync();

                await _activityLogService.LogAsync(CallerId, "casePriorityChanged",
                    $"Changed the priority of the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} from {oldPriority} to {request.Priority}");
            }

            return new ChangeCasePriorityResponse
            {
                CaseId = caseRecord.CaseId,
                AdvisorId = caseRecord.AdvisorId,
                StudentId = caseRecord.StudentId,
                AdvisorEmail = caseRecord.Advisor?.Email ?? string.Empty,
                AdvisorFullName = caseRecord.Advisor?.FullName ?? string.Empty,
                CategoryName = caseRecord.SupportCategory.CategoryName,
                StudentFullName = caseRecord.Student.FullName,
                ClosedAt = caseRecord.ClosedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Status = caseRecord.Status
            };
        }

        // Search Case stage histories of one case, oldest first (same access as the case; the student can see their own)
        public async Task<List<SearchCaseStageHistoryResponse>> SearchCaseStageHistoryAsync(Guid CallerId, SearchCaseStageHistoryRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            await EnsureCanViewCaseAsync(caseRecord, CallerId);

            var histories = await _caseStageHistoryRepository.SearchAsyncIncludeOrderBy(h => h.CaseId == request.CaseId, h => h.ChangedAt, true, h => h.ChangedByNavigation);

            return histories.Select(h => new SearchCaseStageHistoryResponse
            {
                CaseStageHistoryId = h.CaseStageHistoryId,
                CaseId = h.CaseId,
                ChangedBy = h.ChangedBy,
                ChangedByFullName = h.ChangedByNavigation?.FullName ?? string.Empty,
                Reason = h.Reason,
                ChangedAt = h.ChangedAt,
                FromStage = h.FromStage,
                ToStage = h.ToStage
            }).ToList();
        }

        // Helper: load Cases with the student (and profile), advisor, opener, subject and category, newest first
        private async Task<List<Case>> GetCasesAsync(Expression<Func<Case, bool>> Predicate)
        {
            return await _caseRepository.SearchAsyncIncludeOrderBy(Predicate, c => c.OpenedAt, false,
                c => c.Student.Studentprofile, c => c.Advisor, c => c.OpenedByNavigation, c => c.Subject, c => c.SupportCategory);
        }

        // Helper: load one Case with its details
        private async Task<Case> GetCaseAsync(Guid CaseId)
        {
            return (await GetCasesAsync(c => c.CaseId == CaseId)).FirstOrDefault()
                ?? throw new NotFoundException("Case not found.");
        }

        // Helper: the advisor's cases after the filters, each with the advisor's unread notifications that point to it, sorted as asked
        private async Task<List<AssignedCase>> GetAssignedCasesAsync(Guid AdvisorId, SearchAssignedCaseRequest request)
        {
            var student = request.Student.Trim();
            var cases = await GetCasesAsync(c =>
                c.AdvisorId == AdvisorId &&
                (string.IsNullOrWhiteSpace(request.SupportType) || c.SupportCategory.SupportType == request.SupportType) &&
                (string.IsNullOrWhiteSpace(request.CurrentStage) || c.CurrentStage == request.CurrentStage) &&
                (string.IsNullOrWhiteSpace(request.Priority) || c.Priority == request.Priority) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status));
            var unread = await _notificationRepository.SearchAsync(n => n.AccountId == AdvisorId && !n.IsRead && n.RedirectUrl != null && n.RedirectUrl.StartsWith("/cases/"));

            var assigned = cases
                .Select(c => new AssignedCase(c, unread.Count(n => n.RedirectUrl == $"/cases/{c.CaseId}")))
                .Where(a => student.Length == 0 ||
                            a.Case.Student.FullName.Contains(student, StringComparison.OrdinalIgnoreCase) ||
                            (a.Case.Student.Studentprofile?.StudentCode ?? string.Empty).Contains(student, StringComparison.OrdinalIgnoreCase))
                .Where(a => !request.UnreadOnly || a.UnreadNotificationCount > 0)
                .ToList();

            var isDescending = request.SortDirection != "asc";
            IOrderedEnumerable<AssignedCase> sorted = request.SortBy switch
            {
                "priority" => isDescending ? assigned.OrderByDescending(a => PriorityRank(a.Case.Priority)) : assigned.OrderBy(a => PriorityRank(a.Case.Priority)),
                "stage" => isDescending ? assigned.OrderByDescending(a => Array.IndexOf(Stages, a.Case.CurrentStage)) : assigned.OrderBy(a => Array.IndexOf(Stages, a.Case.CurrentStage)),
                "studentName" => isDescending ? assigned.OrderByDescending(a => a.Case.Student.FullName) : assigned.OrderBy(a => a.Case.Student.FullName),
                _ => isDescending ? assigned.OrderByDescending(a => a.Case.OpenedAt) : assigned.OrderBy(a => a.Case.OpenedAt)
            };
            return sorted.ThenByDescending(a => a.Case.OpenedAt).ToList();
        }

        // Helper: the case must be visible to the caller, and only its current advisor may act on it
        private async Task<Case> GetCaseForAdvisorAsync(Guid AdvisorId, Guid CaseId)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            await EnsureCanViewCaseAsync(caseRecord, AdvisorId);
            if (caseRecord.AdvisorId != AdvisorId)
                throw new ForbiddenException("Only the case's current advisor can do this.");
            return caseRecord;
        }

        // Helper: who may see a case: its student, its current advisor, the Programme Head, and anyone who sent or received a referral of it.
        // Anyone else gets "not found", so case ids cannot be probed. Returns the caller's role names
        private async Task<HashSet<string>> EnsureCanViewCaseAsync(Case CaseRecord, Guid CallerId)
        {
            var callerRoles = await GetRoleNamesAsync(CallerId);
            var canView = CaseRecord.StudentId == CallerId ||
                          CaseRecord.AdvisorId == CallerId ||
                          callerRoles.Contains("Programme Head") ||
                          await _referralRepository.ExistsAsync(r => r.CaseId == CaseRecord.CaseId && (r.FromAccountId == CallerId || r.ToAccountId == CallerId));
            if (!canView)
                throw new NotFoundException("Case not found.");
            return callerRoles;
        }

        // Helper: a pending referral blocks stage changes and advisor changes until the receiver accepts or returns it
        private async Task<bool> HasPendingReferralAsync(Guid CaseId)
        {
            return await _referralRepository.ExistsAsync(r => r.CaseId == CaseId && r.Status == "pending");
        }

        // Helper: stage one CASESTAGEHISTORIES row (insert-only) and move the case to the new stage; the caller saves both together
        private void AddStageHistory(Case CaseRecord, Guid ChangedBy, string ToStage, string Reason, DateTime ChangedAt)
        {
            _caseStageHistoryRepository.PrepareCreate(new Casestagehistory
            {
                CaseStageHistoryId = Guid.NewGuid(),
                CaseId = CaseRecord.CaseId,
                ChangedBy = ChangedBy,
                Reason = Reason,
                ChangedAt = ChangedAt,
                FromStage = CaseRecord.CurrentStage,
                ToStage = ToStage
            });
            CaseRecord.CurrentStage = ToStage;
        }

        // Helper: tell the student who their advisor is (with the contact information), and tell the advisor about the case
        private async Task NotifyMatchAsync(Case CaseRecord, Account Advisor, bool IsReassignment)
        {
            var categoryName = CaseRecord.SupportCategory.CategoryName;
            await _notificationService.NotifyAsync(CaseRecord.StudentId,
                IsReassignment ? "Your case has a new advisor" : "Your support request has been matched",
                $"{Advisor.FullName} ({Advisor.Email}) will handle your {categoryName} request. Please contact them to arrange a meeting.",
                $"/cases/{CaseRecord.CaseId}");
            await _notificationService.NotifyAsync(Advisor.AccountId, "New case assigned",
                $"You have been assigned the {categoryName} case of {CaseRecord.Student.FullName} ({CaseRecord.Student.Email}).",
                $"/cases/{CaseRecord.CaseId}");
        }

        // Helper: the role names of an account
        private async Task<HashSet<string>> GetRoleNamesAsync(Guid AccountId)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == AccountId, ar => ar.Role))
                .Select(ar => ar.Role.RoleName)
                .ToHashSet();
        }

        // Helper: the active accounts that have a role, by name
        private async Task<List<Account>> GetActiveAccountsInRoleAsync(string RoleName)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.Role.RoleName == RoleName && ar.Account.Status == "active", ar => ar.Account))
                .Select(ar => ar.Account)
                .OrderBy(a => a.FullName)
                .ToList();
        }

        // Helper: an active account with the given advisor role
        private async Task<Account> GetActiveAdvisorAsync(Guid AdvisorId, string RoleName)
        {
            var advisor = await _accountRepository.GetByIdAsync(AdvisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            if (advisor.Status != "active")
                throw new ValidationException($"{advisor.FullName} is not an active account.");
            if (!(await GetRoleNamesAsync(AdvisorId)).Contains(RoleName))
                throw new ValidationException($"{advisor.FullName} does not have the role {RoleName}.");
            return advisor;
        }

        // Helper: which advisor role handles which support type
        private static string AdvisorRoleFor(string SupportType)
        {
            return SupportType == "wellbeing" ? "Wellbeing Advisor" : "Academic Advisor";
        }

        // Helper: urgent = 4 ... low = 1, used to order by priority
        private static int PriorityRank(string Priority)
        {
            return Priority switch
            {
                "urgent" => 4,
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }

        // Helper type: an assigned case with the advisor's unread notification count for it
        private record AssignedCase(Case Case, int UnreadNotificationCount);
    }
}
