using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Contracts.Requests.Consent;
using AdviseCare.Application.Contracts.Responses.Case;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D2 support requests, D3 matching, D4 advisor case workflow (stages, close/reopen, priority)
    public class CaseService(
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Casestagehistory> stageHistoryRepository,
        IGenericRepository<Supportcategory> categoryRepository,
        IGenericRepository<Subject> subjectRepository,
        IGenericRepository<Consentpurpose> purposeRepository,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<Advisorassignment> assignmentRepository,
        IGenericRepository<AccountSubject> accountSubjectRepository,
        IGenericRepository<Faculty> facultyRepository,
        IGenericRepository<Referral> referralRepository,
        IGenericRepository<Notification> notificationRepository,
        IConsentService consentService,
        INotificationService notificationService,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : ICaseService
    {
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Casestagehistory> _stageHistoryRepository = stageHistoryRepository;
        private readonly IGenericRepository<Supportcategory> _categoryRepository = categoryRepository;
        private readonly IGenericRepository<Subject> _subjectRepository = subjectRepository;
        private readonly IGenericRepository<Consentpurpose> _purposeRepository = purposeRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly IGenericRepository<Advisorassignment> _assignmentRepository = assignmentRepository;
        private readonly IGenericRepository<AccountSubject> _accountSubjectRepository = accountSubjectRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository = facultyRepository;
        private readonly IGenericRepository<Referral> _referralRepository = referralRepository;
        private readonly IGenericRepository<Notification> _notificationRepository = notificationRepository;
        private readonly IConsentService _consentService = consentService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        // ==================== D2 / D4.2: create a case ====================

        public async Task<CaseResponse> CreateAsync(Guid callerId, CreateCaseRequest request)
        {
            var roles = await _workflow.GetRoleNamesAsync(callerId);
            var category = await _categoryRepository.GetByIdAsync(request.SupportCategoryId)
                ?? throw new NotFoundException("Support category not found.");
            if (category.Status != CaseRules.Active)
                throw new ValidationException($"The support category '{category.CategoryName}' is archived.");

            var reason = CaseWorkflowHelper.RequireText(request.RequestReason, "Request reason is required.");

            Subject? subject = null;
            if (request.SubjectId.HasValue && request.SubjectId.Value != Guid.Empty)
            {
                if (category.SupportType != CaseRules.Academic)
                    throw new ValidationException("Only academic requests can name a subject.");
                subject = await _subjectRepository.GetByIdAsync(request.SubjectId.Value)
                    ?? throw new NotFoundException("Subject not found.");
                if (subject.Status != CaseRules.Active)
                    throw new ValidationException($"The subject {subject.SubjectCode} is archived.");
            }

            var isOutreach = request.StudentId.HasValue && request.StudentId.Value != Guid.Empty;
            var attributeIds = request.SharedAttributeIds.Where(id => id != Guid.Empty).Distinct().ToList();
            Account student;
            string priority;

            if (isOutreach)
            {
                // EX3: an advisor opens a case for one of their assigned students
                var advisorRole = CaseRules.AdvisorRoleFor(category.SupportType);
                if (!roles.Contains(advisorRole))
                    throw new ForbiddenException($"Only a {advisorRole} can open a {category.SupportType} case for a student.");
                if (attributeIds.Count > 0)
                    throw new ValidationException("Shared data is chosen by the student. Leave sharedAttributeIds empty when an advisor opens a case.");

                student = await _accountRepository.GetByIdAsync(request.StudentId!.Value)
                    ?? throw new NotFoundException("Student not found.");
                if (!(await _workflow.GetRoleNamesAsync(student.AccountId)).Contains(CaseRules.StudentRole))
                    throw new ValidationException($"{student.FullName} is not a student.");
                if (student.Status != CaseRules.Active)
                    throw new ValidationException($"{student.FullName} is not an active account.");
                if (!await _assignmentRepository.ExistsAsync(a =>
                        a.AdvisorId == callerId && a.StudentId == student.AccountId && a.Status == CaseRules.Active))
                    throw new ForbiddenException("You can only open cases for students who are assigned to you.");

                priority = CaseWorkflowHelper.OptionalFilter(request.Priority, CaseRules.Priorities, "Priority") ?? "medium";
            }
            else
            {
                // WF2: the student requests support
                if (!roles.Contains(CaseRules.StudentRole))
                    throw new ForbiddenException("Only students can request support. Advisors must give studentId to open a case for a student.");

                student = await _accountRepository.GetByIdAsync(callerId)
                    ?? throw new NotFoundException("Account not found.");
                priority = "medium";

                if (attributeIds.Count > 0)
                {
                    // Checked here so a wrong attribute is refused before anything is staged (IConsentService checks again)
                    var purpose = await _purposeRepository.GetByIdAsyncInclude(category.ConsentPurposeId, p => p.DataAttributes)
                        ?? throw new NotFoundException("The consent purpose of this category was not found.");
                    if (purpose.Status != CaseRules.Active)
                        throw new ValidationException($"Data cannot be shared for '{category.CategoryName}' right now because '{purpose.PurposeName}' is not active. Submit the request without shared data.");
                    var allowed = purpose.DataAttributes.Where(a => a.Status == CaseRules.Active).Select(a => a.DataAttributeId).ToHashSet();
                    if (attributeIds.Any(id => !allowed.Contains(id)))
                        throw new ValidationException($"Only the data attributes of '{purpose.PurposeName}' can be shared with a '{category.CategoryName}' request.");
                }
            }

            var now = DateTime.UtcNow;
            var caseRecord = new Case
            {
                CaseId = Guid.NewGuid(),
                OpenedBy = callerId,
                StudentId = student.AccountId,
                SubjectId = subject?.SubjectId,
                SupportCategoryId = category.SupportCategoryId,
                RequestReason = reason,
                OpenedAt = now,
                CurrentStage = CaseRules.StagePending,
                Priority = priority,
                Status = CaseRules.StatusOpen
            };
            _caseRepository.PrepareCreate(caseRecord);

            if (isOutreach)
            {
                caseRecord.AdvisorId = callerId;
                _workflow.StageStageChange(caseRecord, callerId, CaseRules.StageAssigned, "Case opened by the advisor", now);
            }
            else
            {
                _workflow.StageStageChange(caseRecord, callerId, CaseRules.StagePending, "Support requested by the student", now);
            }

            if (attributeIds.Count > 0)
            {
                // Saves the staged case and history together with the consent rows (same DbContext, one SaveChanges)
                await _consentService.CreateAsync(student.AccountId, new CreateConsentRequest
                {
                    ConsentPurposeId = category.ConsentPurposeId,
                    DataAttributeIds = attributeIds
                });
            }
            await _caseRepository.SaveAsync();

            var subjectText = subject != null ? $" for {subject.SubjectCode}" : string.Empty;
            if (isOutreach)
            {
                var advisor = await _accountRepository.GetByIdAsync(callerId);
                await _activityLogService.LogAsync(callerId, "caseOpened",
                    $"Opened a {category.CategoryName} case{subjectText} for {student.FullName}");
                await _notificationService.NotifyAsync(student.AccountId,
                    "An advisor would like to meet you",
                    $"{advisor?.FullName} ({advisor?.Email}) opened a {category.CategoryName} case for you and would like to meet. Please contact them to arrange a meeting.",
                    CaseRules.CaseUrl(caseRecord.CaseId));
            }
            else
            {
                await _activityLogService.LogAsync(callerId, "supportRequested",
                    $"Requested {category.SupportType} support ({category.CategoryName}){subjectText}, sharing {attributeIds.Count} data attribute(s)");
                foreach (var programmeHead in await _workflow.GetActiveAccountsInRoleAsync(CaseRules.ProgrammeHeadRole))
                {
                    await _notificationService.NotifyAsync(programmeHead.AccountId,
                        "New support request",
                        $"{student.FullName} requested {category.SupportType} support ({category.CategoryName}{subjectText}). The case is waiting in the matching queue.",
                        CaseRules.CaseUrl(caseRecord.CaseId));
                }
            }

            return await _workflow.ToCaseResponseAsync(await _workflow.GetCaseWithDetailsAsync(caseRecord.CaseId));
        }

        public async Task<List<CaseResponse>> GetMyCasesAsync(Guid studentId, SearchMyCasesRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, CaseRules.CaseStatuses, "Status");
            var stage = CaseWorkflowHelper.OptionalFilter(request.CurrentStage, CaseRules.Stages, "Current stage");

            var cases = await _caseRepository.SearchAsyncIncludeOrderBy(
                c => c.StudentId == studentId &&
                     (status == null || c.Status == status) &&
                     (stage == null || c.CurrentStage == stage),
                c => c.OpenedAt,
                false,
                c => c.Student, c => c.Advisor, c => c.OpenedByNavigation, c => c.Subject, c => c.SupportCategory);

            return await _workflow.ToCaseResponsesAsync(cases);
        }

        public async Task<CaseResponse> GetByIdAsync(Guid callerId, Guid caseId)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            var roles = await _workflow.GetRoleNamesAsync(callerId);
            await _workflow.EnsureCanViewCaseAsync(caseRecord, callerId, roles);

            // Opening the case marks the caller's notifications about it as read (feeds the assigned list's unread count)
            var url = CaseRules.CaseUrl(caseId);
            var unread = await _notificationRepository.SearchAsync(n => n.AccountId == callerId && !n.IsRead && n.RedirectUrl == url);
            if (unread.Count > 0)
            {
                foreach (var notification in unread)
                    notification.IsRead = true;
                await _notificationRepository.SaveAsync();
            }

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        public async Task<List<CaseStageHistoryResponse>> GetStageHistoryAsync(Guid callerId, Guid caseId)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(caseId)
                ?? throw new NotFoundException("Case not found.");
            await _workflow.EnsureCanViewCaseAsync(caseRecord, callerId, await _workflow.GetRoleNamesAsync(callerId));

            var history = await _stageHistoryRepository.SearchAsyncIncludeOrderBy(
                h => h.CaseId == caseId, h => h.ChangedAt, true, h => h.ChangedByNavigation);

            return history.Select(h => new CaseStageHistoryResponse
            {
                CaseStageHistoryId = h.CaseStageHistoryId,
                CaseId = h.CaseId,
                ChangedBy = h.ChangedBy,
                ChangedByName = h.ChangedByNavigation?.FullName ?? string.Empty,
                Reason = h.Reason,
                ChangedAt = CaseRules.AsUtc(h.ChangedAt),
                FromStage = h.FromStage,
                ToStage = h.ToStage
            }).ToList();
        }

        // ==================== D3: matching ====================

        public async Task<List<CaseResponse>> GetMatchingQueueAsync()
        {
            var cases = await _caseRepository.SearchAsyncInclude(
                c => c.AdvisorId == null && c.Status != CaseRules.StatusClosed,
                c => c.Student, c => c.Advisor, c => c.OpenedByNavigation, c => c.Subject, c => c.SupportCategory);

            var ordered = cases
                .OrderByDescending(c => CaseRules.PriorityRank(c.Priority))
                .ThenBy(c => c.OpenedAt)
                .ToList();
            return await _workflow.ToCaseResponsesAsync(ordered);
        }

        public async Task<List<SuggestedAdvisorResponse>> GetSuggestedAdvisorsAsync(Guid caseId)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            var supportType = caseRecord.SupportCategory.SupportType;
            var candidates = await _workflow.GetActiveAccountsInRoleAsync(CaseRules.AdvisorRoleFor(supportType));
            if (candidates.Count == 0)
                return new List<SuggestedAdvisorResponse>();

            var candidateIds = candidates.Select(a => a.AccountId).ToList();
            var subjectLinks = await _accountSubjectRepository.SearchAsyncInclude(
                link => candidateIds.Contains(link.AccountId), link => link.Subject);
            var openCases = await _caseRepository.SearchAsync(c =>
                c.AdvisorId != null && candidateIds.Contains(c.AdvisorId.Value) && c.Status != CaseRules.StatusClosed);
            var familiarIds = (await _assignmentRepository.SearchAsync(a =>
                    a.StudentId == caseRecord.StudentId && a.Status == CaseRules.Active && candidateIds.Contains(a.AdvisorId)))
                .Select(a => a.AdvisorId)
                .ToHashSet();
            var facultyIds = candidates.Where(a => a.FacultyId != null).Select(a => a.FacultyId!.Value).Distinct().ToList();
            var faculties = (await _facultyRepository.SearchAsync(f => facultyIds.Contains(f.FacultyId)))
                .ToDictionary(f => f.FacultyId, f => f.FacultyName);
            var studentFacultyId = caseRecord.Student?.FacultyId;
            var isAcademic = supportType == CaseRules.Academic;

            var suggestions = candidates.Select(advisor =>
            {
                var links = subjectLinks.Where(l => l.AccountId == advisor.AccountId).ToList();
                var suggestion = new SuggestedAdvisorResponse
                {
                    AdvisorId = advisor.AccountId,
                    FullName = advisor.FullName,
                    Email = advisor.Email,
                    FacultyId = advisor.FacultyId,
                    FacultyName = advisor.FacultyId.HasValue ? faculties.GetValueOrDefault(advisor.FacultyId.Value) ?? string.Empty : string.Empty,
                    SpecialisedSubjectCodes = links.Select(l => l.Subject?.SubjectCode ?? string.Empty).OrderBy(code => code).ToList(),
                    IsSubjectSpecialist = isAcademic && caseRecord.SubjectId.HasValue && links.Any(l => l.SubjectId == caseRecord.SubjectId.Value),
                    IsGeneralAdvisor = isAcademic && links.Count == 0,
                    IsSameFaculty = studentFacultyId.HasValue && advisor.FacultyId == studentFacultyId,
                    IsFamiliarAdvisor = familiarIds.Contains(advisor.AccountId),
                    OpenCaseCount = openCases.Count(c => c.AdvisorId == advisor.AccountId)
                };
                if (suggestion.IsFamiliarAdvisor) suggestion.MatchReasons.Add("Already advises this student");
                if (suggestion.IsSubjectSpecialist) suggestion.MatchReasons.Add($"Specialised in {caseRecord.Subject?.SubjectCode}");
                if (suggestion.IsGeneralAdvisor) suggestion.MatchReasons.Add("General academic advisor");
                if (suggestion.IsSameFaculty) suggestion.MatchReasons.Add("Same faculty as the student");
                suggestion.MatchReasons.Add($"{suggestion.OpenCaseCount} open case(s)");
                return suggestion;
            });

            // Familiar pair first (optional add-on of WF2 3.2.3), then subject specialists, then the student's faculty
            // (general advisors before other specialists there), then the lightest workload
            return suggestions
                .OrderByDescending(s => s.IsFamiliarAdvisor)
                .ThenByDescending(s => s.IsSubjectSpecialist)
                .ThenByDescending(s => s.IsSameFaculty)
                .ThenByDescending(s => s.IsGeneralAdvisor)
                .ThenBy(s => s.OpenCaseCount)
                .ThenBy(s => s.FullName)
                .ToList();
        }

        public async Task<CaseResponse> AssignAsync(Guid programmeHeadId, Guid caseId, AssignCaseRequest request)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId != null || caseRecord.CurrentStage != CaseRules.StagePending)
                throw new ValidationException("The case already has an advisor. Use reassign or an assignment change request to change it.");

            var category = caseRecord.SupportCategory;
            var (advisor, _) = await _workflow.GetActiveAdvisorAsync(request.AdvisorId, CaseRules.AdvisorRoleFor(category.SupportType));
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Matched by the Programme Head" : request.Reason.Trim();

            var now = DateTime.UtcNow;
            await _workflow.StageEnsureAssignmentAsync(advisor.AccountId, caseRecord.StudentId, programmeHeadId, now);
            caseRecord.AdvisorId = advisor.AccountId;
            caseRecord.Advisor = advisor;
            _workflow.StageStageChange(caseRecord, programmeHeadId, CaseRules.StageAssigned, reason, now);
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(programmeHeadId, "caseAssigned",
                $"Assigned the {category.CategoryName} case of {caseRecord.Student.FullName} to {advisor.FullName}");
            await NotifyMatchAsync(caseRecord, advisor, isReassignment: false);

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        public async Task<CaseResponse> ReassignAsync(Guid programmeHeadId, Guid caseId, ReassignCaseRequest request)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId == null)
                throw new ValidationException("The case has no advisor yet. Use assign instead.");
            if (caseRecord.AdvisorId == request.AdvisorId)
                throw new ValidationException("The case is already handled by this advisor.");
            if (await _workflow.HasPendingReferralAsync(caseId))
                throw new ConflictException("A referral of this case is waiting for an answer. Reassign it after the referral is accepted or returned.");

            var category = caseRecord.SupportCategory;
            var (advisor, _) = await _workflow.GetActiveAdvisorAsync(request.AdvisorId, CaseRules.AdvisorRoleFor(category.SupportType));
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var oldAdvisor = caseRecord.Advisor;

            var now = DateTime.UtcNow;
            await _workflow.StageEnsureAssignmentAsync(advisor.AccountId, caseRecord.StudentId, programmeHeadId, now);
            caseRecord.AdvisorId = advisor.AccountId;
            caseRecord.Advisor = advisor;
            _workflow.StageStageChange(caseRecord, programmeHeadId, caseRecord.CurrentStage,
                $"Reassigned from {oldAdvisor?.FullName} to {advisor.FullName}: {reason}", now);
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(programmeHeadId, "caseReassigned",
                $"Moved the {category.CategoryName} case of {caseRecord.Student.FullName} from {oldAdvisor?.FullName} to {advisor.FullName}: {reason}");
            await NotifyMatchAsync(caseRecord, advisor, isReassignment: true);
            if (oldAdvisor != null)
            {
                await _notificationService.NotifyAsync(oldAdvisor.AccountId,
                    "Case moved to another advisor",
                    $"The {category.CategoryName} case of {caseRecord.Student.FullName} is now handled by {advisor.FullName}.",
                    CaseRules.CaseUrl(caseId));
            }

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        private async Task NotifyMatchAsync(Case caseRecord, Account advisor, bool isReassignment)
        {
            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _notificationService.NotifyAsync(caseRecord.StudentId,
                isReassignment ? "Your case has a new advisor" : "Your support request has been matched",
                $"{advisor.FullName} ({advisor.Email}) will handle your {categoryName} request. Please contact them to arrange a meeting.",
                CaseRules.CaseUrl(caseRecord.CaseId));
            await _notificationService.NotifyAsync(advisor.AccountId,
                "New case assigned",
                $"You have been assigned the {categoryName} case of {caseRecord.Student.FullName} ({caseRecord.Student.Email}).",
                CaseRules.CaseUrl(caseRecord.CaseId));
        }

        // ==================== D4: advisor workflow ====================

        public async Task<AssignedCasesResponse> GetAssignedAsync(Guid advisorId, SearchAssignedCasesRequest request)
        {
            var stage = CaseWorkflowHelper.OptionalFilter(request.CurrentStage, CaseRules.Stages, "Current stage");
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, CaseRules.CaseStatuses, "Status");
            var supportType = CaseWorkflowHelper.OptionalFilter(request.SupportType, CaseRules.SupportTypes, "Support type");
            var priority = CaseWorkflowHelper.OptionalFilter(request.Priority, CaseRules.Priorities, "Priority");
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "openedAt" : request.SortBy.Trim();
            if (sortBy is not ("openedAt" or "priority" or "stage" or "studentName"))
                throw new ValidationException("sortBy must be openedAt, priority, stage or studentName.");
            var descending = !string.Equals(request.SortDirection?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);
            if ((request.Page.HasValue || request.PageSize.HasValue) && (request.Page is null or < 1 || request.PageSize is null or < 1))
                throw new ValidationException("Page and page size must both be given and be at least 1.");

            var cases = await _caseRepository.SearchAsyncInclude(
                c => c.AdvisorId == advisorId &&
                     (stage == null || c.CurrentStage == stage) &&
                     (status == null || c.Status == status) &&
                     (supportType == null || c.SupportCategory.SupportType == supportType) &&
                     (priority == null || c.Priority == priority),
                c => c.Student, c => c.Advisor, c => c.OpenedByNavigation, c => c.Subject, c => c.SupportCategory);

            var items = await _workflow.ToCaseResponsesAsync(cases);

            var unread = await _notificationRepository.SearchAsync(n =>
                n.AccountId == advisorId && !n.IsRead && n.RedirectUrl != null && n.RedirectUrl.StartsWith("/cases/"));
            foreach (var item in items)
            {
                var url = CaseRules.CaseUrl(item.CaseId);
                item.UnreadNotificationCount = unread.Count(n => n.RedirectUrl == url);
            }

            var studentFilter = request.Student?.Trim() ?? string.Empty;
            IEnumerable<CaseResponse> filtered = items;
            if (studentFilter.Length > 0)
                filtered = filtered.Where(i =>
                    i.StudentName.Contains(studentFilter, StringComparison.OrdinalIgnoreCase) ||
                    i.StudentCode.Contains(studentFilter, StringComparison.OrdinalIgnoreCase));
            if (request.UnreadOnly == true)
                filtered = filtered.Where(i => i.UnreadNotificationCount > 0);

            var list = filtered.ToList();
            IOrderedEnumerable<CaseResponse> sorted = sortBy switch
            {
                "priority" => descending
                    ? list.OrderByDescending(i => CaseRules.PriorityRank(i.Priority))
                    : list.OrderBy(i => CaseRules.PriorityRank(i.Priority)),
                "stage" => descending
                    ? list.OrderByDescending(i => Array.IndexOf(CaseRules.Stages, i.CurrentStage))
                    : list.OrderBy(i => Array.IndexOf(CaseRules.Stages, i.CurrentStage)),
                "studentName" => descending ? list.OrderByDescending(i => i.StudentName) : list.OrderBy(i => i.StudentName),
                _ => descending ? list.OrderByDescending(i => i.OpenedAt) : list.OrderBy(i => i.OpenedAt)
            };
            var ordered = sorted.ThenByDescending(i => i.OpenedAt).ToList();

            var page = request.Page ?? 1;
            var pageSize = request.PageSize ?? Math.Max(ordered.Count, 1);
            return new AssignedCasesResponse
            {
                TotalCount = ordered.Count,
                Page = page,
                PageSize = pageSize,
                UnreadCaseCount = ordered.Count(i => i.UnreadNotificationCount > 0),
                Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public async Task<CaseResponse> ChangeStageAsync(Guid advisorId, Guid caseId, ChangeCaseStageRequest request)
        {
            var caseRecord = await GetCaseForAdvisorAsync(advisorId, caseId);
            var toStage = request.ToStage?.Trim() ?? string.Empty;
            if (!CaseRules.ManualStages.Contains(toStage))
                throw new ValidationException("toStage must be contacted, followUp or closed. A case becomes assigned through matching, a reopen or an accepted referral, and referred through a referral; it never goes back to pending.");
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed. Reopen it first.");
            if (caseRecord.CurrentStage == toStage)
                throw new ValidationException($"The case is already at stage {toStage}.");
            if (await _workflow.HasPendingReferralAsync(caseId))
                throw new ConflictException("A referral of this case is waiting for an answer.");

            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var now = DateTime.UtcNow;
            var fromStage = caseRecord.CurrentStage;
            _workflow.StageStageChange(caseRecord, advisorId, toStage, reason, now);
            if (toStage == CaseRules.StageClosed)
            {
                caseRecord.Status = CaseRules.StatusClosed;
                caseRecord.ClosedAt = now;
            }
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            if (toStage == CaseRules.StageClosed)
            {
                await _activityLogService.LogAsync(advisorId, "caseClosed",
                    $"Closed the {categoryName} case of {caseRecord.Student.FullName}: {reason}");
                await _notificationService.NotifyAsync(caseRecord.StudentId,
                    "Your case has been closed",
                    $"Your {categoryName} case was closed by {caseRecord.Advisor?.FullName}: {reason}",
                    CaseRules.CaseUrl(caseId));
            }
            else
            {
                await _activityLogService.LogAsync(advisorId, "caseStageChanged",
                    $"Moved the {categoryName} case of {caseRecord.Student.FullName} from {fromStage} to {toStage}");
            }

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        public async Task<CaseResponse> ReopenAsync(Guid advisorId, Guid caseId, ReopenCaseRequest request)
        {
            var caseRecord = await GetCaseForAdvisorAsync(advisorId, caseId);
            if (caseRecord.Status != CaseRules.StatusClosed)
                throw new ConflictException("Only a closed case can be reopened.");

            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var now = DateTime.UtcNow;
            _workflow.StageStageChange(caseRecord, advisorId, CaseRules.StageAssigned, reason, now);
            caseRecord.Status = CaseRules.StatusReopened;
            caseRecord.ClosedAt = null;
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(advisorId, "caseReopened",
                $"Reopened the {categoryName} case of {caseRecord.Student.FullName}: {reason}");
            await _notificationService.NotifyAsync(caseRecord.StudentId,
                "Your case has been reopened",
                $"{caseRecord.Advisor?.FullName} reopened your {categoryName} case: {reason}",
                CaseRules.CaseUrl(caseId));

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        public async Task<CaseResponse> ChangePriorityAsync(Guid callerId, Guid caseId, ChangeCasePriorityRequest request)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            var roles = await _workflow.GetRoleNamesAsync(callerId);
            await _workflow.EnsureCanViewCaseAsync(caseRecord, callerId, roles);

            // The case's advisor, the Programme Head, or a Welfare Officer who received a referral of the case (WF2b step 2.2)
            var allowed = caseRecord.AdvisorId == callerId ||
                          roles.Contains(CaseRules.ProgrammeHeadRole) ||
                          (roles.Contains(CaseRules.WelfareOfficerRole) &&
                           await _referralRepository.ExistsAsync(r => r.CaseId == caseId && r.ToAccountId == callerId));
            if (!allowed)
                throw new ForbiddenException("You cannot change the priority of this case.");
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed.");

            var priority = CaseWorkflowHelper.OptionalFilter(request.Priority, CaseRules.Priorities, "Priority")
                ?? throw new ValidationException("Priority is required (low, medium, high or urgent).");
            if (caseRecord.Priority == priority)
                return await _workflow.ToCaseResponseAsync(caseRecord);

            var oldPriority = caseRecord.Priority;
            caseRecord.Priority = priority;
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseRepository.SaveAsync();

            await _activityLogService.LogAsync(callerId, "casePriorityChanged",
                $"Changed the priority of the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} from {oldPriority} to {priority}");

            return await _workflow.ToCaseResponseAsync(caseRecord);
        }

        // The case must exist and be visible; only its current advisor may act on it
        private async Task<Case> GetCaseForAdvisorAsync(Guid advisorId, Guid caseId)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(caseId);
            var roles = await _workflow.GetRoleNamesAsync(advisorId);
            await _workflow.EnsureCanViewCaseAsync(caseRecord, advisorId, roles);
            if (caseRecord.AdvisorId != advisorId)
                throw new ForbiddenException("Only the case's current advisor can do this.");
            return caseRecord;
        }
    }
}
