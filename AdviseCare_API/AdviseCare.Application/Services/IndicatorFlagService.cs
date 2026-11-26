using AdviseCare.Application.Contracts.Requests.IndicatorFlag;
using AdviseCare.Application.Contracts.Responses.IndicatorFlag;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Globalization;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Indicator flags (INDICATORFLAGS, early-support "Students in need"): the server compares each enrolled student's current STUDENTDATA value
    // (per term, and per subject for a per-subject data attribute) with every active indicator (CheckAsync, after every student data import and on
    // request), raises one open flag per student, rule, term and subject, and clears an open flag whose student no longer hits the rule.
    // A flag the Programme Head already dismissed or opened a case from is not raised again for the same student, rule, term and subject.
    // The Programme Head lists the flags by urgency, dismisses one with a reason, or opens a case from it for an advisor (directly, or by approving
    // an advisor's take-on request); the case copies the flag's severity and priority. Dismissing, clearing or opening a case cancels the other
    // pending take-on requests for the flag. Flags never show the student's value, so reading them writes no ACCESSLOGS row.
    public class IndicatorFlagService : IIndicatorFlagService
    {
        private const string DataValueField = "dataValue";

        private readonly IGenericRepository<Indicatorflag> _indicatorFlagRepository;
        private readonly IGenericRepository<Indicator> _indicatorRepository;
        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Casestagehistory> _caseStageHistoryRepository;
        private readonly IGenericRepository<Takeonrequest> _takeOnRequestRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public IndicatorFlagService(
            IGenericRepository<Indicatorflag> indicatorFlagRepository,
            IGenericRepository<Indicator> indicatorRepository,
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Casestagehistory> caseStageHistoryRepository,
            IGenericRepository<Takeonrequest> takeOnRequestRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IAttributeKeyService attributeKeyService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _indicatorFlagRepository = indicatorFlagRepository;
            _indicatorRepository = indicatorRepository;
            _studentDataRepository = studentDataRepository;
            _studentProfileRepository = studentProfileRepository;
            _caseRepository = caseRepository;
            _caseStageHistoryRepository = caseStageHistoryRepository;
            _takeOnRequestRepository = takeOnRequestRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _attributeKeyService = attributeKeyService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Indicator flags by urgency (severity, priority, oldest): every flag for the Programme Head, the open flags of the advisor's support type for an advisor
        public async Task<IEnumerable<GetAllIndicatorFlagResponse>> GetAll(Guid CallerId)
        {
            var view = await GetCallerViewAsync(CallerId);
            var flags = await GetVisibleFlagsAsync(view, f => true);
            await view.LoadTakeOnRequestsAsync(_takeOnRequestRepository, flags);

            return flags.Select(f => new GetAllIndicatorFlagResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                CaseId = f.CaseId,
                FacultyId = f.Student.FacultyId,
                IndicatorId = f.IndicatorId,
                MyTakeOnRequestId = view.MyRequestFor(f)?.TakeOnRequestId,
                ResolvedBy = f.ResolvedBy,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = f.Indicator.SupportCategoryId,
                TermId = f.TermId,
                AttributeName = f.Indicator.DataAttribute.AttributeName,
                CategoryName = f.Indicator.SupportCategory.CategoryName,
                DismissReason = f.DismissReason ?? string.Empty,
                FacultyName = f.Student.Faculty?.FacultyName ?? string.Empty,
                IndicatorDescription = f.Indicator.Description ?? string.Empty,
                IndicatorName = f.Indicator.IndicatorName,
                MyTakeOnRequestStatus = view.MyRequestFor(f)?.Status ?? string.Empty,
                ResolvedByFullName = f.ResolvedByNavigation?.FullName ?? string.Empty,
                StudentCode = f.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = f.Student.FullName,
                SubjectCode = f.Subject?.SubjectCode ?? string.Empty,
                SubjectName = f.Subject?.SubjectName ?? string.Empty,
                TermName = f.Term.TermName,
                ThresholdValue = f.Indicator.ThresholdValue,
                PendingTakeOnRequestCount = view.PendingCountFor(f),
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ComparisonOperator = f.Indicator.ComparisonOperator,
                SupportType = f.Indicator.SupportCategory.SupportType,
                Priority = f.Priority,
                Severity = f.Severity,
                Status = f.Status
            });
        }

        // Get all Indicator flags with pagination
        public async Task<IEnumerable<GetAllIndicatorFlagWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var view = await GetCallerViewAsync(CallerId);
            var flags = (await GetVisibleFlagsAsync(view, f => true))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            await view.LoadTakeOnRequestsAsync(_takeOnRequestRepository, flags);

            return flags.Select(f => new GetAllIndicatorFlagWithPaginationResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                CaseId = f.CaseId,
                FacultyId = f.Student.FacultyId,
                IndicatorId = f.IndicatorId,
                MyTakeOnRequestId = view.MyRequestFor(f)?.TakeOnRequestId,
                ResolvedBy = f.ResolvedBy,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = f.Indicator.SupportCategoryId,
                TermId = f.TermId,
                AttributeName = f.Indicator.DataAttribute.AttributeName,
                CategoryName = f.Indicator.SupportCategory.CategoryName,
                DismissReason = f.DismissReason ?? string.Empty,
                FacultyName = f.Student.Faculty?.FacultyName ?? string.Empty,
                IndicatorDescription = f.Indicator.Description ?? string.Empty,
                IndicatorName = f.Indicator.IndicatorName,
                MyTakeOnRequestStatus = view.MyRequestFor(f)?.Status ?? string.Empty,
                ResolvedByFullName = f.ResolvedByNavigation?.FullName ?? string.Empty,
                StudentCode = f.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = f.Student.FullName,
                SubjectCode = f.Subject?.SubjectCode ?? string.Empty,
                SubjectName = f.Subject?.SubjectName ?? string.Empty,
                TermName = f.Term.TermName,
                ThresholdValue = f.Indicator.ThresholdValue,
                PendingTakeOnRequestCount = view.PendingCountFor(f),
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ComparisonOperator = f.Indicator.ComparisonOperator,
                SupportType = f.Indicator.SupportCategory.SupportType,
                Priority = f.Priority,
                Severity = f.Severity,
                Status = f.Status
            });
        }

        // Get Indicator flag by Id (the Programme Head; an advisor sees an open flag of their support type, or one they asked to take on)
        public async Task<GetIndicatorFlagByIdResponse> GetByIdAsync(Guid CallerId, Guid IndicatorFlagId)
        {
            var view = await GetCallerViewAsync(CallerId);
            var f = await GetFlagAsync(IndicatorFlagId);
            if (!view.IsProgrammeHead &&
                !(f.Status == "open" && view.SupportTypes.Contains(f.Indicator.SupportCategory.SupportType)) &&
                !await _takeOnRequestRepository.ExistsAsync(r => r.IndicatorFlagId == IndicatorFlagId && r.AdvisorId == CallerId))
                throw new NotFoundException("Indicator flag not found.");
            await view.LoadTakeOnRequestsAsync(_takeOnRequestRepository, new List<Indicatorflag> { f });

            return new GetIndicatorFlagByIdResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                CaseId = f.CaseId,
                FacultyId = f.Student.FacultyId,
                IndicatorId = f.IndicatorId,
                MyTakeOnRequestId = view.MyRequestFor(f)?.TakeOnRequestId,
                ResolvedBy = f.ResolvedBy,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = f.Indicator.SupportCategoryId,
                TermId = f.TermId,
                AttributeName = f.Indicator.DataAttribute.AttributeName,
                CategoryName = f.Indicator.SupportCategory.CategoryName,
                DismissReason = f.DismissReason ?? string.Empty,
                FacultyName = f.Student.Faculty?.FacultyName ?? string.Empty,
                IndicatorDescription = f.Indicator.Description ?? string.Empty,
                IndicatorName = f.Indicator.IndicatorName,
                MyTakeOnRequestStatus = view.MyRequestFor(f)?.Status ?? string.Empty,
                ResolvedByFullName = f.ResolvedByNavigation?.FullName ?? string.Empty,
                StudentCode = f.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = f.Student.FullName,
                SubjectCode = f.Subject?.SubjectCode ?? string.Empty,
                SubjectName = f.Subject?.SubjectName ?? string.Empty,
                TermName = f.Term.TermName,
                ThresholdValue = f.Indicator.ThresholdValue,
                PendingTakeOnRequestCount = view.PendingCountFor(f),
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ComparisonOperator = f.Indicator.ComparisonOperator,
                SupportType = f.Indicator.SupportCategory.SupportType,
                Priority = f.Priority,
                Severity = f.Severity,
                Status = f.Status
            };
        }

        // Search Indicator flags by rule, student, subject, term, priority, severity, support type and status, by urgency (same visibility as GetAll)
        public async Task<List<SearchIndicatorFlagResponse>> SearchAsync(Guid CallerId, SearchIndicatorFlagRequest request)
        {
            var view = await GetCallerViewAsync(CallerId);
            var flags = await GetVisibleFlagsAsync(view, f =>
                (request.IndicatorId == null || f.IndicatorId == request.IndicatorId) &&
                (request.StudentId == null || f.StudentId == request.StudentId) &&
                (request.SubjectId == null || f.SubjectId == request.SubjectId) &&
                (request.TermId == null || f.TermId == request.TermId) &&
                (string.IsNullOrWhiteSpace(request.Priority) || f.Priority == request.Priority) &&
                (string.IsNullOrWhiteSpace(request.Severity) || f.Severity == request.Severity) &&
                (string.IsNullOrWhiteSpace(request.SupportType) || f.Indicator.SupportCategory.SupportType == request.SupportType) &&
                (string.IsNullOrWhiteSpace(request.Status) || f.Status == request.Status));
            await view.LoadTakeOnRequestsAsync(_takeOnRequestRepository, flags);

            return flags.Select(f => new SearchIndicatorFlagResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                CaseId = f.CaseId,
                FacultyId = f.Student.FacultyId,
                IndicatorId = f.IndicatorId,
                MyTakeOnRequestId = view.MyRequestFor(f)?.TakeOnRequestId,
                ResolvedBy = f.ResolvedBy,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = f.Indicator.SupportCategoryId,
                TermId = f.TermId,
                AttributeName = f.Indicator.DataAttribute.AttributeName,
                CategoryName = f.Indicator.SupportCategory.CategoryName,
                DismissReason = f.DismissReason ?? string.Empty,
                FacultyName = f.Student.Faculty?.FacultyName ?? string.Empty,
                IndicatorDescription = f.Indicator.Description ?? string.Empty,
                IndicatorName = f.Indicator.IndicatorName,
                MyTakeOnRequestStatus = view.MyRequestFor(f)?.Status ?? string.Empty,
                ResolvedByFullName = f.ResolvedByNavigation?.FullName ?? string.Empty,
                StudentCode = f.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = f.Student.FullName,
                SubjectCode = f.Subject?.SubjectCode ?? string.Empty,
                SubjectName = f.Subject?.SubjectName ?? string.Empty,
                TermName = f.Term.TermName,
                ThresholdValue = f.Indicator.ThresholdValue,
                PendingTakeOnRequestCount = view.PendingCountFor(f),
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ComparisonOperator = f.Indicator.ComparisonOperator,
                SupportType = f.Indicator.SupportCategory.SupportType,
                Priority = f.Priority,
                Severity = f.Severity,
                Status = f.Status
            }).ToList();
        }

        // Check Indicator flags (A7.2): compare every enrolled, active student's current value of each active indicator's data attribute
        // (per term, and per subject for a per-subject attribute) with the rule; raise new open flags and clear open flags that no longer hit
        // (and the open flags of rules that are no longer active), in one save. The Programme Heads are notified of new flags. No ACCESSLOGS row
        public async Task<CheckIndicatorFlagResponse> CheckAsync(Guid CallerId)
        {
            var now = DateTime.UtcNow;
            var indicators = await _indicatorRepository.SearchAsyncInclude(i => i.Status == "active", i => i.DataAttribute);
            var studentIds = (await _studentProfileRepository.SearchAsyncInclude(p => p.AcademicStatus == "enrolled" && p.Student.Status == "active", p => p.Student))
                .Select(p => p.StudentId)
                .ToHashSet();
            var attributeIds = indicators.Select(i => i.DataAttributeId).Distinct().ToList();

            // The value in effect (latest recorded) per student, data attribute, term and subject
            var rows = attributeIds.Count == 0 || studentIds.Count == 0
                ? new List<Studentdata>()
                : await _studentDataRepository.SearchAsync(d => attributeIds.Contains(d.DataAttributeId) && studentIds.Contains(d.StudentId));
            var currentRows = rows
                .GroupBy(d => new { d.StudentId, d.DataAttributeId, d.TermId, d.SubjectId })
                .Select(g => g.OrderByDescending(d => d.TransactionFrom).ThenByDescending(d => d.ValidFrom).First())
                .ToList();
            var readable = currentRows.Where(d => !d.IsMissing && d.ErasedAt == null && d.AttributeKeyId.HasValue && d.DataValue != null).ToList();
            var values = readable.Count == 0
                ? new Dictionary<Guid, string?>()
                : (await _attributeKeyService.DecryptValuesAsync(readable.Select(d => new EncryptedAttributeValue
                    {
                        AttributeKeyId = d.AttributeKeyId!.Value,
                        StudentId = d.StudentId,
                        DataAttributeId = d.DataAttributeId,
                        RowId = d.StudentDataId,
                        FieldName = DataValueField,
                        Payload = d.DataValue
                    }).ToList()))
                    .ToDictionary(v => v.RowId, v => v.IsErased ? null : v.Value);

            var flags = await _indicatorFlagRepository.SearchAsync(f => true);
            var activeIndicatorIds = indicators.Select(i => i.IndicatorId).ToHashSet();
            var raised = new List<Indicatorflag>();
            var cleared = new List<Indicatorflag>();

            foreach (var indicator in indicators)
            {
                foreach (var row in currentRows.Where(d => d.DataAttributeId == indicator.DataAttributeId))
                {
                    var sameKey = flags.Where(f => f.IndicatorId == indicator.IndicatorId && f.StudentId == row.StudentId && f.TermId == row.TermId && f.SubjectId == row.SubjectId).ToList();
                    var openFlag = sameKey.FirstOrDefault(f => f.Status == "open");
                    var isHit = values.TryGetValue(row.StudentDataId, out var value) && value != null &&
                                IsHit(indicator.DataAttribute.DataType, indicator.ComparisonOperator, value, indicator.ThresholdValue);

                    if (isHit && openFlag == null && !sameKey.Any(f => f.Status == "dismissed" || f.Status == "caseOpened"))
                    {
                        var flag = new Indicatorflag
                        {
                            IndicatorFlagId = Guid.NewGuid(),
                            IndicatorId = indicator.IndicatorId,
                            StudentDataId = row.StudentDataId,
                            StudentId = row.StudentId,
                            SubjectId = row.SubjectId,
                            TermId = row.TermId,
                            FlaggedAt = now,
                            Priority = indicator.Priority,
                            Severity = indicator.Severity,
                            Status = "open"
                        };
                        _indicatorFlagRepository.PrepareCreate(flag);
                        flags.Add(flag);
                        raised.Add(flag);
                    }
                    else if (isHit && openFlag != null && openFlag.StudentDataId != row.StudentDataId)
                    {
                        // Still hit by a newer value: the flag points to the value now in effect
                        openFlag.StudentDataId = row.StudentDataId;
                        _indicatorFlagRepository.PrepareUpdate(openFlag);
                    }
                    else if (!isHit && openFlag != null)
                    {
                        cleared.Add(openFlag);
                    }
                }
            }
            cleared.AddRange(flags.Where(f => f.Status == "open" && !activeIndicatorIds.Contains(f.IndicatorId)));

            var cancelled = new List<Takeonrequest>();
            foreach (var flag in cleared)
            {
                flag.Status = "cleared";
                flag.ResolvedAt = now;
                _indicatorFlagRepository.PrepareUpdate(flag);
                cancelled.AddRange(await CancelTakeOnRequestsAsync(flag.IndicatorFlagId, now));
            }
            await _indicatorFlagRepository.SaveAsync();

            await _activityLogService.LogAsync(CallerId, "indicatorFlagsChecked",
                $"Checked {indicators.Count} active indicator(s) against {currentRows.Count} value(s) of {studentIds.Count} student(s): {raised.Count} flag(s) raised, {cleared.Count} cleared");
            if (raised.Count > 0)
            {
                foreach (var programmeHeadId in await GetActiveAccountIdsInRoleAsync("Programme Head"))
                {
                    await _notificationService.NotifyAsync(programmeHeadId, "New students in need",
                        $"The indicator check raised {raised.Count} new flag(s). Review them in Students in need.",
                        "/head/students-in-need");
                }
            }
            await NotifyCancelledAsync(cancelled, "the student no longer meets the indicator's rule");

            return new CheckIndicatorFlagResponse
            {
                CheckedStudentCount = studentIds.Count,
                CheckedValueCount = currentRows.Count,
                ClearedCount = cleared.Count,
                RaisedCount = raised.Count,
                CheckedAt = now
            };
        }

        // Dismiss Indicator flag: the Programme Head closes an open flag with a reason; its pending take-on requests are cancelled
        public async Task<DismissIndicatorFlagResponse> DismissAsync(Guid ProgrammeHeadId, Guid IndicatorFlagId, DismissIndicatorFlagRequest request)
        {
            var f = await GetFlagAsync(IndicatorFlagId);
            if (f.Status != "open")
                throw new ConflictException("Only an open flag can be dismissed.");
            var reason = request.DismissReason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Dismiss reason is required.");

            var now = DateTime.UtcNow;
            f.DismissReason = reason;
            f.ResolvedBy = ProgrammeHeadId;
            f.ResolvedAt = now;
            f.Status = "dismissed";
            _indicatorFlagRepository.PrepareUpdate(f);
            var cancelled = await CancelTakeOnRequestsAsync(IndicatorFlagId, now);
            await _indicatorFlagRepository.SaveAsync();

            f.ResolvedByNavigation = await _accountRepository.GetByIdAsync(ProgrammeHeadId);
            await _activityLogService.LogAsync(ProgrammeHeadId, "indicatorFlagDismissed",
                $"Dismissed the flag '{f.Indicator.IndicatorName}' of {f.Student.FullName} ({f.Term.TermName}): {reason}");
            await NotifyCancelledAsync(cancelled, "the Programme Head dismissed the flag");

            var view = await GetCallerViewAsync(ProgrammeHeadId);
            await view.LoadTakeOnRequestsAsync(_takeOnRequestRepository, new List<Indicatorflag> { f });

            return new DismissIndicatorFlagResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                CaseId = f.CaseId,
                FacultyId = f.Student.FacultyId,
                IndicatorId = f.IndicatorId,
                MyTakeOnRequestId = view.MyRequestFor(f)?.TakeOnRequestId,
                ResolvedBy = f.ResolvedBy,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = f.Indicator.SupportCategoryId,
                TermId = f.TermId,
                AttributeName = f.Indicator.DataAttribute.AttributeName,
                CategoryName = f.Indicator.SupportCategory.CategoryName,
                DismissReason = f.DismissReason ?? string.Empty,
                FacultyName = f.Student.Faculty?.FacultyName ?? string.Empty,
                IndicatorDescription = f.Indicator.Description ?? string.Empty,
                IndicatorName = f.Indicator.IndicatorName,
                MyTakeOnRequestStatus = view.MyRequestFor(f)?.Status ?? string.Empty,
                ResolvedByFullName = f.ResolvedByNavigation?.FullName ?? string.Empty,
                StudentCode = f.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = f.Student.FullName,
                SubjectCode = f.Subject?.SubjectCode ?? string.Empty,
                SubjectName = f.Subject?.SubjectName ?? string.Empty,
                TermName = f.Term.TermName,
                ThresholdValue = f.Indicator.ThresholdValue,
                PendingTakeOnRequestCount = view.PendingCountFor(f),
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ComparisonOperator = f.Indicator.ComparisonOperator,
                SupportType = f.Indicator.SupportCategory.SupportType,
                Priority = f.Priority,
                Severity = f.Severity,
                Status = f.Status
            };
        }

        // Open a Case from an Indicator flag: the Programme Head (directly, or by approving an advisor's take-on request) opens a case of the rule's
        // support category for an advisor of its type: opened by the Programme Head, stage assigned (with its stage history row), the flag's subject,
        // severity and priority, and the rule as the reason. The flag gets the case (status caseOpened); the other pending take-on requests are cancelled.
        // The student is told who will contact them, and the advisor is told about the case
        public async Task<OpenCaseFromIndicatorFlagResponse> OpenCaseAsync(Guid ProgrammeHeadId, Guid IndicatorFlagId, OpenCaseFromIndicatorFlagRequest request)
        {
            var f = await GetFlagAsync(IndicatorFlagId);
            if (f.Status != "open")
                throw new ConflictException("Only an open flag can be turned into a case.");
            if (f.Student.Status != "active")
                throw new ValidationException($"{f.Student.FullName} is not an active account.");

            var category = f.Indicator.SupportCategory;
            var advisor = await GetActiveAdvisorAsync(request.AdvisorId, AdvisorRoleFor(category.SupportType));
            var reason = request.Reason.Trim();
            var now = DateTime.UtcNow;
            var subjectText = f.Subject != null ? $" in {f.Subject.SubjectCode}" : string.Empty;

            var caseRecord = new Case
            {
                CaseId = Guid.NewGuid(),
                AdvisorId = advisor.AccountId,
                OpenedBy = ProgrammeHeadId,
                StudentId = f.StudentId,
                SubjectId = f.SubjectId,
                SupportCategoryId = category.SupportCategoryId,
                RequestReason = $"Indicator flag '{f.Indicator.IndicatorName}'{subjectText} ({f.Term.TermName}): {f.Indicator.Description}",
                OpenedAt = now,
                CurrentStage = "assigned",
                Priority = f.Priority,
                Severity = f.Severity,
                Status = "open"
            };
            _caseRepository.PrepareCreate(caseRecord);
            _caseStageHistoryRepository.PrepareCreate(new Casestagehistory
            {
                CaseStageHistoryId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                ChangedBy = ProgrammeHeadId,
                Reason = reason.Length == 0 ? $"Case opened from the indicator flag '{f.Indicator.IndicatorName}'" : reason,
                ChangedAt = now,
                FromStage = "pending",
                ToStage = "assigned"
            });

            f.CaseId = caseRecord.CaseId;
            f.ResolvedBy = ProgrammeHeadId;
            f.ResolvedAt = now;
            f.Status = "caseOpened";
            _indicatorFlagRepository.PrepareUpdate(f);
            var approved = (await _takeOnRequestRepository.SearchAsync(r => r.IndicatorFlagId == IndicatorFlagId && (r.Status == "pending" || r.Status == "approved")))
                .FirstOrDefault(r => r.Status == "approved");
            var cancelled = await CancelTakeOnRequestsAsync(IndicatorFlagId, now);
            await _indicatorFlagRepository.SaveAsync();

            await _activityLogService.LogAsync(ProgrammeHeadId, "caseOpenedFromFlag",
                $"Opened a {category.CategoryName} case for {f.Student.FullName} from the flag '{f.Indicator.IndicatorName}'{subjectText} ({f.Term.TermName}) and assigned it to {advisor.FullName}");
            await _notificationService.NotifyAsync(f.StudentId, "An advisor would like to meet you",
                $"{advisor.FullName} ({advisor.Email}) will support you with {category.CategoryName}. Please contact them to arrange a meeting.",
                $"/cases/{caseRecord.CaseId}");
            var isApproval = approved != null && approved.AdvisorId == advisor.AccountId;
            await _notificationService.NotifyAsync(advisor.AccountId, isApproval ? "Take-on request approved" : "New case assigned",
                isApproval
                    ? $"Your request was approved: a {category.CategoryName} case for {f.Student.FullName} ({f.Student.Email}) was opened for you from the flag '{f.Indicator.IndicatorName}'."
                    : $"You have been assigned a {category.CategoryName} case for {f.Student.FullName} ({f.Student.Email}), opened from the flag '{f.Indicator.IndicatorName}'.",
                $"/cases/{caseRecord.CaseId}");
            await NotifyCancelledAsync(cancelled, $"a case was opened for {advisor.FullName}");

            return new OpenCaseFromIndicatorFlagResponse
            {
                IndicatorFlagId = f.IndicatorFlagId,
                AdvisorId = advisor.AccountId,
                CaseId = caseRecord.CaseId,
                StudentId = f.StudentId,
                AdvisorEmail = advisor.Email,
                AdvisorFullName = advisor.FullName,
                CategoryName = category.CategoryName,
                IndicatorName = f.Indicator.IndicatorName,
                StudentFullName = f.Student.FullName,
                OpenedAt = caseRecord.OpenedAt,
                CurrentStage = caseRecord.CurrentStage,
                Priority = caseRecord.Priority,
                Severity = caseRecord.Severity,
                Status = f.Status
            };
        }

        // Helper: load Indicator flags with the student (profile, faculty), rule (data attribute, category), term, subject and resolver
        private async Task<List<Indicatorflag>> GetFlagsAsync(Expression<Func<Indicatorflag, bool>> Predicate)
        {
            return await _indicatorFlagRepository.SearchAsyncInclude(Predicate,
                f => f.Student.Studentprofile, f => f.Student.Faculty, f => f.Indicator.DataAttribute, f => f.Indicator.SupportCategory,
                f => f.Term, f => f.Subject, f => f.ResolvedByNavigation);
        }

        // Helper: load one Indicator flag with its details
        private async Task<Indicatorflag> GetFlagAsync(Guid IndicatorFlagId)
        {
            return (await GetFlagsAsync(f => f.IndicatorFlagId == IndicatorFlagId)).FirstOrDefault()
                ?? throw new NotFoundException("Indicator flag not found.");
        }

        // Helper: the flags the caller may see (every flag for the Programme Head, the open flags of the advisor's support type for an advisor),
        // most urgent first: severity, then priority, then oldest
        private async Task<List<Indicatorflag>> GetVisibleFlagsAsync(CallerView View, Expression<Func<Indicatorflag, bool>> Predicate)
        {
            var supportTypes = View.SupportTypes;
            var flags = View.IsProgrammeHead
                ? await GetFlagsAsync(Predicate)
                : (await GetFlagsAsync(Predicate)).Where(f => f.Status == "open" && supportTypes.Contains(f.Indicator.SupportCategory.SupportType)).ToList();

            return flags
                .OrderByDescending(f => SeverityRank(f.Severity))
                .ThenByDescending(f => PriorityRank(f.Priority))
                .ThenBy(f => f.FlaggedAt)
                .ToList();
        }

        // Helper: the caller's role names and support types, used to decide what they see
        private async Task<CallerView> GetCallerViewAsync(Guid CallerId)
        {
            var roleNames = (await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == CallerId, ar => ar.Role))
                .Select(ar => ar.Role.RoleName)
                .ToHashSet();
            var supportTypes = new HashSet<string>();
            if (roleNames.Contains("Academic Advisor"))
                supportTypes.Add("academic");
            if (roleNames.Contains("Wellbeing Advisor"))
                supportTypes.Add("wellbeing");
            return new CallerView(CallerId, roleNames.Contains("Programme Head"), supportTypes);
        }

        // Helper: cancel the pending take-on requests of a flag that is no longer open (staged only; a request already staged as approved keeps
        // that status). Returns the cancelled requests for the notifications
        private async Task<List<Takeonrequest>> CancelTakeOnRequestsAsync(Guid IndicatorFlagId, DateTime DecidedAt)
        {
            var cancelled = (await _takeOnRequestRepository.SearchAsyncInclude(r => r.IndicatorFlagId == IndicatorFlagId && r.Status == "pending",
                    r => r.IndicatorFlag.Student, r => r.IndicatorFlag.Indicator))
                .Where(r => r.Status == "pending")
                .ToList();
            foreach (var request in cancelled)
            {
                request.Status = "cancelled";
                request.DecidedAt = DecidedAt;
                _takeOnRequestRepository.PrepareUpdate(request);
            }
            return cancelled;
        }

        // Helper: tell each advisor whose take-on request was cancelled why
        private async Task NotifyCancelledAsync(List<Takeonrequest> Cancelled, string Why)
        {
            foreach (var request in Cancelled)
            {
                await _notificationService.NotifyAsync(request.AdvisorId, "Take-on request cancelled",
                    $"Your request to take on {request.IndicatorFlag?.Student?.FullName} (flag '{request.IndicatorFlag?.Indicator?.IndicatorName}') was cancelled: {Why}.",
                    "/advisor/students-in-need");
            }
        }

        // Helper: an active account with the given advisor role
        private async Task<Account> GetActiveAdvisorAsync(Guid AdvisorId, string RoleName)
        {
            var advisor = await _accountRepository.GetByIdAsync(AdvisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            if (advisor.Status != "active")
                throw new ValidationException($"{advisor.FullName} is not an active account.");
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == AdvisorId && ar.Role.RoleName == RoleName))
                throw new ValidationException($"{advisor.FullName} does not have the role {RoleName}.");
            return advisor;
        }

        // Helper: the ids of the active accounts that have a role, by name
        private async Task<List<Guid>> GetActiveAccountIdsInRoleAsync(string RoleName)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.Role.RoleName == RoleName && ar.Account.Status == "active", ar => ar.Account))
                .Select(ar => ar.AccountId)
                .Distinct()
                .ToList();
        }

        // Helper: does a value hit a rule? numeric compares as numbers; boolean and categorical support equal and notEqual only (case-insensitive)
        private static bool IsHit(string DataType, string ComparisonOperator, string Value, string Threshold)
        {
            if (DataType == "numeric")
            {
                if (!decimal.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                    !decimal.TryParse(Threshold, NumberStyles.Float, CultureInfo.InvariantCulture, out var limit))
                    return false;
                return ComparisonOperator switch
                {
                    "lessThan" => number < limit,
                    "lessOrEqual" => number <= limit,
                    "greaterThan" => number > limit,
                    "greaterOrEqual" => number >= limit,
                    "equal" => number == limit,
                    "notEqual" => number != limit,
                    _ => false
                };
            }

            var isEqual = string.Equals(Value.Trim(), Threshold.Trim(), StringComparison.OrdinalIgnoreCase);
            return ComparisonOperator switch
            {
                "equal" => isEqual,
                "notEqual" => !isEqual,
                _ => false
            };
        }

        // Helper: which advisor role handles which support type
        private static string AdvisorRoleFor(string SupportType)
        {
            return SupportType == "wellbeing" ? "Wellbeing Advisor" : "Academic Advisor";
        }

        // Helper: critical = 4 ... low = 1, used to order by severity
        private static int SeverityRank(string Severity)
        {
            return Severity switch
            {
                "critical" => 4,
                "high" => 3,
                "moderate" => 2,
                "low" => 1,
                _ => 0
            };
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

        // Helper type: who is asking, and the take-on requests of the flags being shown (the pending count for the Programme Head,
        // the caller's own latest request for an advisor)
        private class CallerView
        {
            private Dictionary<Guid, List<Takeonrequest>> _requests = new();

            public CallerView(Guid CallerId, bool IsProgrammeHead, HashSet<string> SupportTypes)
            {
                this.CallerId = CallerId;
                this.IsProgrammeHead = IsProgrammeHead;
                this.SupportTypes = SupportTypes;
            }

            public Guid CallerId { get; }
            public bool IsProgrammeHead { get; }
            public HashSet<string> SupportTypes { get; }

            public async Task LoadTakeOnRequestsAsync(IGenericRepository<Takeonrequest> Repository, List<Indicatorflag> Flags)
            {
                var flagIds = Flags.Select(f => f.IndicatorFlagId).ToList();
                var callerId = CallerId;
                var isProgrammeHead = IsProgrammeHead;
                _requests = (await Repository.SearchAsync(r => r.IndicatorFlagId != null && flagIds.Contains(r.IndicatorFlagId.Value) &&
                        (isProgrammeHead ? r.Status == "pending" : r.AdvisorId == callerId)))
                    .GroupBy(r => r.IndicatorFlagId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            public int PendingCountFor(Indicatorflag Flag)
            {
                return IsProgrammeHead && _requests.TryGetValue(Flag.IndicatorFlagId, out var requests) ? requests.Count(r => r.Status == "pending") : 0;
            }

            public Takeonrequest? MyRequestFor(Indicatorflag Flag)
            {
                return !IsProgrammeHead && _requests.TryGetValue(Flag.IndicatorFlagId, out var requests) ? requests.OrderByDescending(r => r.CreatedAt).First() : null;
            }
        }
    }
}
