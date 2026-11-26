using AdviseCare.Application.Contracts.Requests.MeetupRequest;
using AdviseCare.Application.Contracts.Responses.MeetupRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Globalization;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Meet-ups (MEETUPREQUESTS, MEETUPSCHEDULES): while a case is not closed and is in stage contacted or followUp, its student or its
    // current advisor requests a meet-up (date, start time, duration, location, room, reason; one pending request per case).
    // Only the side that did not propose the current date answers: accept, change the date (the same request goes back to the other side)
    // or decline with a reason; the side that proposed the current date can cancel it. Accepting creates the MEETUPSCHEDULES row and moves
    // a contacted case to followUp (with its stage history row) in one save.
    // Times are Vietnam time (UTC+7, stored in UTC): Monday to Friday, 08:30-11:30 and 13:30-18:00, 15-minute steps, inside one window,
    // in the future, and never overlapping a scheduled meet-up of the same advisor or student.
    // Either side cancels a scheduled meet-up with a reason before it starts; the advisor marks it completed or missed once it has started.
    // Closing a case or changing its advisor cancels its pending requests and its meet-ups that have not started (CancelOpenMeetupsForCaseAsync).
    public class MeetupRequestService : IMeetupRequestService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private const int SlotMinutes = 15;

        // The bookable time windows (Vietnam time); a meet-up must start and end inside one of them
        private static readonly (TimeOnly Start, TimeOnly End)[] TimeWindows =
        {
            (new TimeOnly(8, 30), new TimeOnly(11, 30)),
            (new TimeOnly(13, 30), new TimeOnly(18, 0))
        };

        private readonly IGenericRepository<Meetuprequest> _meetupRequestRepository;
        private readonly IGenericRepository<Meetupschedule> _meetupScheduleRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Casestagehistory> _caseStageHistoryRepository;
        private readonly ICaseAccessService _caseAccessService;
        private readonly IActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public MeetupRequestService(
            IGenericRepository<Meetuprequest> meetupRequestRepository,
            IGenericRepository<Meetupschedule> meetupScheduleRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Casestagehistory> caseStageHistoryRepository,
            ICaseAccessService caseAccessService,
            IActivityLogService activityLogService,
            INotificationService notificationService)
        {
            _meetupRequestRepository = meetupRequestRepository;
            _meetupScheduleRepository = meetupScheduleRepository;
            _caseRepository = caseRepository;
            _caseStageHistoryRepository = caseStageHistoryRepository;
            _caseAccessService = caseAccessService;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
        }

        // Get all Meetup requests of the signed-in student or advisor, newest first
        public async Task<IEnumerable<GetAllMeetupRequestResponse>> GetAll(Guid CallerId)
        {
            var requests = await GetRequestsAsync(r => r.AdvisorId == CallerId || r.Case.StudentId == CallerId);

            return requests.Select(r => new GetAllMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            });
        }

        // Get all Meetup requests of the signed-in student or advisor with pagination
        public async Task<IEnumerable<GetAllMeetupRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = await GetRequestsAsync(r => r.AdvisorId == CallerId || r.Case.StudentId == CallerId);

            return requests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllMeetupRequestWithPaginationResponse
                {
                    MeetupRequestId = r.MeetupRequestId,
                    AdvisorId = r.AdvisorId,
                    AwaitingResponseFrom = AwaitingResponseFrom(r),
                    CaseId = r.CaseId,
                    ProposedBy = r.ProposedBy,
                    RequestedBy = r.RequestedBy,
                    RespondedBy = r.RespondedBy,
                    StudentId = r.Case.StudentId,
                    AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                    CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                    DeclineReason = r.DeclineReason,
                    LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                    LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                    Location = r.Location,
                    ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                    Reason = r.Reason,
                    RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                    Room = r.Room,
                    StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                    DurationMinutes = r.DurationMinutes,
                    RescheduleCount = r.RescheduleCount,
                    CreatedAt = r.CreatedAt,
                    ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                    ProposedStartAt = r.ProposedStartAt,
                    RespondedAt = r.RespondedAt,
                    UpdatedAt = r.UpdatedAt,
                    Status = r.Status
                });
        }

        // Get Meetup request by Id (the student of the case, the advisor of the request, and the staff who can see the case)
        public async Task<GetMeetupRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid MeetupRequestId)
        {
            var r = await GetRequestAsync(MeetupRequestId);
            if (!await CanReadAsync(r, CallerId))
                throw new NotFoundException("Meet-up request not found.");

            return new GetMeetupRequestByIdResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Search Meetup requests: of one case (its readers), or else the signed-in student's or advisor's own; by status and date range, newest first
        public async Task<List<SearchMeetupRequestResponse>> SearchAsync(Guid CallerId, SearchMeetupRequestRequest request)
        {
            if (request.CaseId != null)
            {
                var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId.Value)
                    ?? throw new NotFoundException("Case not found.");
                if (caseRecord.StudentId != CallerId && !await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, CallerId))
                    throw new NotFoundException("Case not found.");
            }
            var (fromUtc, toUtc) = ToUtcRange(request.FromDate, request.ToDate);

            var requests = await GetRequestsAsync(r =>
                (request.CaseId != null ? r.CaseId == request.CaseId : (r.AdvisorId == CallerId || r.Case.StudentId == CallerId)) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status) &&
                (fromUtc == null || r.ProposedStartAt >= fromUtc) &&
                (toUtc == null || r.ProposedStartAt < toUtc));

            return requests.Select(r => new SearchMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            }).ToList();
        }

        // Create Meetup request: the student of the case or its current advisor asks to meet (case not closed, stage contacted or followUp,
        // no other pending request on the case); the time must be bookable and free for both sides; the other side is notified
        public async Task<CreateMeetupRequestResponse> CreateAsync(Guid CreatorId, CreateMeetupRequestRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsyncInclude(request.CaseId, c => c.Student, c => c.Advisor, c => c.SupportCategory)
                ?? throw new NotFoundException("Case not found.");
            if (caseRecord.StudentId != CreatorId && caseRecord.AdvisorId != CreatorId)
            {
                if (!await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, CreatorId))
                    throw new NotFoundException("Case not found.");
                throw new ForbiddenException("Only the student of the case and its current advisor can request a meet-up.");
            }
            EnsureCaseAllowsMeetups(caseRecord);
            if (await _meetupRequestRepository.ExistsAsync(r => r.CaseId == caseRecord.CaseId && r.Status == "pending"))
                throw new ConflictException("This case already has a meet-up request waiting for an answer. Answer or cancel it first.");

            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");
            var location = request.Location.Trim();
            if (location.Length == 0)
                throw new ValidationException("Location is required.");
            var startUtc = ToBookableStartUtc(request.MeetupDate, request.StartTime, request.DurationMinutes);
            await EnsureNoOverlapAsync(caseRecord.AdvisorId!.Value, caseRecord.StudentId, startUtc, request.DurationMinutes);

            var meetupRequest = new Meetuprequest
            {
                MeetupRequestId = Guid.NewGuid(),
                AdvisorId = caseRecord.AdvisorId.Value,
                CaseId = caseRecord.CaseId,
                ProposedBy = CreatorId,
                RequestedBy = CreatorId,
                Location = location,
                Reason = reason,
                Room = string.IsNullOrWhiteSpace(request.Room) ? null : request.Room.Trim(),
                DurationMinutes = request.DurationMinutes,
                RescheduleCount = 0,
                CreatedAt = DateTime.UtcNow,
                ProposedStartAt = startUtc,
                Status = "pending"
            };
            await _meetupRequestRepository.CreateAsync(meetupRequest);

            var when = DescribeTime(startUtc, request.DurationMinutes);
            await _activityLogService.LogAsync(CreatorId, "meetupRequested",
                $"Requested a meet-up on {when} for the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}");
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CreatorId, "New meet-up request",
                $"{(CreatorId == caseRecord.StudentId ? caseRecord.Student.FullName : caseRecord.Advisor.FullName)} asked to meet on {when}. Accept it, change the date or decline it.");

            var r = await GetRequestAsync(meetupRequest.MeetupRequestId);
            return new CreateMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Update Meetup request (change the date): the side that must answer proposes another date, time, duration, location or room;
        // the same request goes back to the other side (proposedBy changes, rescheduleCount + 1)
        public async Task<UpdateMeetupRequestResponse> UpdateAsync(Guid CallerId, Guid MeetupRequestId, UpdateMeetupRequestRequest request)
        {
            var meetupRequest = await GetPendingForAnswerAsync(CallerId, MeetupRequestId);
            var caseRecord = meetupRequest.Case;

            var location = request.Location.Trim();
            if (location.Length == 0)
                throw new ValidationException("Location is required.");
            var startUtc = ToBookableStartUtc(request.MeetupDate, request.StartTime, request.DurationMinutes);
            await EnsureNoOverlapAsync(meetupRequest.AdvisorId, caseRecord.StudentId, startUtc, request.DurationMinutes);

            meetupRequest.ProposedBy = CallerId;
            meetupRequest.Location = location;
            meetupRequest.Room = string.IsNullOrWhiteSpace(request.Room) ? null : request.Room.Trim();
            meetupRequest.DurationMinutes = request.DurationMinutes;
            meetupRequest.RescheduleCount += 1;
            meetupRequest.ProposedStartAt = startUtc;
            meetupRequest.UpdatedAt = DateTime.UtcNow;
            _meetupRequestRepository.PrepareUpdate(meetupRequest);
            await _meetupRequestRepository.SaveAsync();

            var when = DescribeTime(startUtc, request.DurationMinutes);
            await _activityLogService.LogAsync(CallerId, "meetupRescheduled",
                $"Proposed another meet-up time ({when}) for the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}");
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CallerId, "Meet-up date changed",
                $"Another time was proposed for your meet-up: {when}. Accept it, change the date again or decline it.");

            var r = await GetRequestAsync(MeetupRequestId);
            return new UpdateMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Accept Meetup request: the side that must answer accepts; the meet-up is scheduled (MEETUPSCHEDULES) and a contacted case moves
        // to followUp with a stage history row, all in one save; the other side is notified
        public async Task<AcceptMeetupRequestResponse> AcceptAsync(Guid CallerId, Guid MeetupRequestId)
        {
            var meetupRequest = await GetPendingForAnswerAsync(CallerId, MeetupRequestId);
            var caseRecord = meetupRequest.Case;
            if (meetupRequest.ProposedStartAt <= DateTime.UtcNow)
                throw new ConflictException("The proposed time has already passed. Change the date instead.");
            await EnsureNoOverlapAsync(meetupRequest.AdvisorId, caseRecord.StudentId, meetupRequest.ProposedStartAt, meetupRequest.DurationMinutes);

            var now = DateTime.UtcNow;
            var when = DescribeTime(meetupRequest.ProposedStartAt, meetupRequest.DurationMinutes);
            meetupRequest.RespondedBy = CallerId;
            meetupRequest.RespondedAt = now;
            meetupRequest.Status = "accepted";
            _meetupRequestRepository.PrepareUpdate(meetupRequest);
            _meetupScheduleRepository.PrepareCreate(new Meetupschedule
            {
                MeetupScheduleId = Guid.NewGuid(),
                MeetupRequestId = meetupRequest.MeetupRequestId,
                Location = meetupRequest.Location,
                Room = meetupRequest.Room,
                DurationMinutes = meetupRequest.DurationMinutes,
                CreatedAt = now,
                ScheduledStartAt = meetupRequest.ProposedStartAt,
                Status = "scheduled"
            });
            var movedToFollowUp = caseRecord.CurrentStage == "contacted";
            if (movedToFollowUp)
            {
                _caseStageHistoryRepository.PrepareCreate(new Casestagehistory
                {
                    CaseStageHistoryId = Guid.NewGuid(),
                    CaseId = caseRecord.CaseId,
                    ChangedBy = CallerId,
                    Reason = $"Meet-up agreed for {when}",
                    ChangedAt = now,
                    FromStage = caseRecord.CurrentStage,
                    ToStage = "followUp"
                });
                caseRecord.CurrentStage = "followUp";
                _caseRepository.PrepareUpdate(caseRecord);
            }
            await _meetupRequestRepository.SaveAsync();

            await _activityLogService.LogAsync(CallerId, "meetupAccepted",
                $"Accepted the meet-up on {when} for the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}" +
                (movedToFollowUp ? " (case moved to follow-up)" : string.Empty));
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CallerId, "Meet-up accepted",
                $"Your meet-up on {when} is scheduled ({DescribePlace(meetupRequest.Location, meetupRequest.Room)}).");

            var r = await GetRequestAsync(MeetupRequestId);
            return new AcceptMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Decline Meetup request: the side that must answer declines with a reason; the other side is notified
        public async Task<DeclineMeetupRequestResponse> DeclineAsync(Guid CallerId, Guid MeetupRequestId, DeclineMeetupRequestRequest request)
        {
            var meetupRequest = await GetPendingForAnswerAsync(CallerId, MeetupRequestId);
            var caseRecord = meetupRequest.Case;
            var declineReason = request.DeclineReason.Trim();
            if (declineReason.Length == 0)
                throw new ValidationException("Decline reason is required.");

            meetupRequest.RespondedBy = CallerId;
            meetupRequest.DeclineReason = declineReason;
            meetupRequest.RespondedAt = DateTime.UtcNow;
            meetupRequest.Status = "declined";
            _meetupRequestRepository.PrepareUpdate(meetupRequest);
            await _meetupRequestRepository.SaveAsync();

            var when = DescribeTime(meetupRequest.ProposedStartAt, meetupRequest.DurationMinutes);
            await _activityLogService.LogAsync(CallerId, "meetupDeclined",
                $"Declined the meet-up on {when} for the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}: {declineReason}");
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CallerId, "Meet-up declined",
                $"The meet-up on {when} was declined: {declineReason}");

            var r = await GetRequestAsync(MeetupRequestId);
            return new DeclineMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Cancel Meetup request: the side that proposed the current date withdraws it while it is pending; the other side is notified
        public async Task<CancelMeetupRequestResponse> CancelAsync(Guid CallerId, Guid MeetupRequestId)
        {
            var meetupRequest = await GetRequestAsync(MeetupRequestId);
            if (!await CanReadAsync(meetupRequest, CallerId))
                throw new NotFoundException("Meet-up request not found.");
            if (meetupRequest.Status != "pending")
                throw new ConflictException("Only a pending meet-up request can be cancelled.");
            if (meetupRequest.ProposedBy != CallerId)
                throw new ForbiddenException("Only the side that proposed the current date can cancel the request. You can decline it instead.");
            var caseRecord = meetupRequest.Case;

            meetupRequest.UpdatedAt = DateTime.UtcNow;
            meetupRequest.Status = "cancelled";
            _meetupRequestRepository.PrepareUpdate(meetupRequest);
            await _meetupRequestRepository.SaveAsync();

            var when = DescribeTime(meetupRequest.ProposedStartAt, meetupRequest.DurationMinutes);
            await _activityLogService.LogAsync(CallerId, "meetupRequestCancelled",
                $"Cancelled the meet-up request for {when} on the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}");
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CallerId, "Meet-up request cancelled",
                $"The meet-up request for {when} was cancelled.");

            var r = await GetRequestAsync(MeetupRequestId);
            return new CancelMeetupRequestResponse
            {
                MeetupRequestId = r.MeetupRequestId,
                AdvisorId = r.AdvisorId,
                AwaitingResponseFrom = AwaitingResponseFrom(r),
                CaseId = r.CaseId,
                ProposedBy = r.ProposedBy,
                RequestedBy = r.RequestedBy,
                RespondedBy = r.RespondedBy,
                StudentId = r.Case.StudentId,
                AdvisorFullName = r.Advisor?.FullName ?? string.Empty,
                CategoryName = r.Case.SupportCategory?.CategoryName ?? string.Empty,
                DeclineReason = r.DeclineReason,
                LocalDate = ToVietnamTime(r.ProposedStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(r.ProposedStartAt).AddMinutes(r.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(r.ProposedStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = r.Location,
                ProposedByFullName = r.ProposedByNavigation?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                Room = r.Room,
                StudentFullName = r.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = r.DurationMinutes,
                RescheduleCount = r.RescheduleCount,
                CreatedAt = r.CreatedAt,
                ProposedEndAt = r.ProposedStartAt.AddMinutes(r.DurationMinutes),
                ProposedStartAt = r.ProposedStartAt,
                RespondedAt = r.RespondedAt,
                UpdatedAt = r.UpdatedAt,
                Status = r.Status
            };
        }

        // Get all Meetup schedules of the signed-in student or advisor, soonest first
        public async Task<IEnumerable<GetAllMeetupScheduleResponse>> GetAllScheduleAsync(Guid CallerId)
        {
            var schedules = await GetSchedulesAsync(s => s.MeetupRequest.AdvisorId == CallerId || s.MeetupRequest.Case.StudentId == CallerId);

            return schedules.Select(s => new GetAllMeetupScheduleResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            });
        }

        // Get all Meetup schedules of the signed-in student or advisor with pagination
        public async Task<IEnumerable<GetAllMeetupScheduleWithPaginationResponse>> GetAllScheduleWithPaginationAsync(Guid CallerId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var schedules = await GetSchedulesAsync(s => s.MeetupRequest.AdvisorId == CallerId || s.MeetupRequest.Case.StudentId == CallerId);

            return schedules
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new GetAllMeetupScheduleWithPaginationResponse
                {
                    MeetupScheduleId = s.MeetupScheduleId,
                    AdvisorId = s.MeetupRequest.AdvisorId,
                    CaseId = s.MeetupRequest.CaseId,
                    CancelledBy = s.CancelledBy,
                    MeetupRequestId = s.MeetupRequestId,
                    StudentId = s.MeetupRequest.Case.StudentId,
                    AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                    CancelReason = s.CancelReason,
                    CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                    CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                    LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                    LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                    Location = s.Location,
                    Reason = s.MeetupRequest.Reason,
                    Room = s.Room,
                    StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                    DurationMinutes = s.DurationMinutes,
                    CancelledAt = s.CancelledAt,
                    CreatedAt = s.CreatedAt,
                    ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                    ScheduledStartAt = s.ScheduledStartAt,
                    Status = s.Status
                });
        }

        // Get Meetup schedule by Id (same readers as its request)
        public async Task<GetMeetupScheduleByIdResponse> GetScheduleByIdAsync(Guid CallerId, Guid MeetupScheduleId)
        {
            var s = await GetScheduleAsync(MeetupScheduleId);
            if (!await CanReadAsync(s.MeetupRequest, CallerId))
                throw new NotFoundException("Meet-up not found.");

            return new GetMeetupScheduleByIdResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            };
        }

        // Search Meetup schedules: of one case (its readers), or else the signed-in student's or advisor's own (the advisor's schedule screen);
        // by status and date range (Vietnam dates), soonest first
        public async Task<List<SearchMeetupScheduleResponse>> SearchScheduleAsync(Guid CallerId, SearchMeetupScheduleRequest request)
        {
            if (request.CaseId != null)
            {
                var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId.Value)
                    ?? throw new NotFoundException("Case not found.");
                if (caseRecord.StudentId != CallerId && !await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, CallerId))
                    throw new NotFoundException("Case not found.");
            }
            var (fromUtc, toUtc) = ToUtcRange(request.FromDate, request.ToDate);

            var schedules = await GetSchedulesAsync(s =>
                (request.CaseId != null
                    ? s.MeetupRequest.CaseId == request.CaseId
                    : (s.MeetupRequest.AdvisorId == CallerId || s.MeetupRequest.Case.StudentId == CallerId)) &&
                (string.IsNullOrWhiteSpace(request.Status) || s.Status == request.Status) &&
                (fromUtc == null || s.ScheduledStartAt >= fromUtc) &&
                (toUtc == null || s.ScheduledStartAt < toUtc));

            return schedules.Select(s => new SearchMeetupScheduleResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            }).ToList();
        }

        // Cancel Meetup schedule: the student or the advisor cancels a scheduled meet-up with a reason before it starts; the other side is notified
        public async Task<CancelMeetupScheduleResponse> CancelScheduleAsync(Guid CallerId, Guid MeetupScheduleId, CancelMeetupScheduleRequest request)
        {
            var schedule = await GetScheduleAsync(MeetupScheduleId);
            var meetupRequest = schedule.MeetupRequest;
            if (meetupRequest.AdvisorId != CallerId && meetupRequest.Case.StudentId != CallerId)
            {
                if (!await CanReadAsync(meetupRequest, CallerId))
                    throw new NotFoundException("Meet-up not found.");
                throw new ForbiddenException("Only the student and the advisor of the meet-up can cancel it.");
            }
            if (schedule.Status != "scheduled")
                throw new ConflictException("Only a scheduled meet-up can be cancelled.");
            if (schedule.ScheduledStartAt <= DateTime.UtcNow)
                throw new ConflictException("The meet-up has already started. The advisor marks it as completed or missed instead.");
            var cancelReason = request.CancelReason.Trim();
            if (cancelReason.Length == 0)
                throw new ValidationException("Cancel reason is required.");

            schedule.CancelledBy = CallerId;
            schedule.CancelReason = cancelReason;
            schedule.CancelledAt = DateTime.UtcNow;
            schedule.Status = "cancelled";
            _meetupScheduleRepository.PrepareUpdate(schedule);
            await _meetupScheduleRepository.SaveAsync();

            var caseRecord = meetupRequest.Case;
            var when = DescribeTime(schedule.ScheduledStartAt, schedule.DurationMinutes);
            await _activityLogService.LogAsync(CallerId, "meetupCancelled",
                $"Cancelled the meet-up on {when} for the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}: {cancelReason}");
            await NotifyOtherSideAsync(meetupRequest, caseRecord, CallerId, "Meet-up cancelled",
                $"The meet-up on {when} was cancelled: {cancelReason}");

            var s = await GetScheduleAsync(MeetupScheduleId);
            return new CancelMeetupScheduleResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            };
        }

        // Complete Meetup schedule: the advisor of the meet-up marks it as held once it has started
        public async Task<CompleteMeetupScheduleResponse> CompleteScheduleAsync(Guid AdvisorId, Guid MeetupScheduleId)
        {
            var schedule = await GetScheduleForOutcomeAsync(AdvisorId, MeetupScheduleId);
            schedule.Status = "completed";
            _meetupScheduleRepository.PrepareUpdate(schedule);
            await _meetupScheduleRepository.SaveAsync();

            await _activityLogService.LogAsync(AdvisorId, "meetupCompleted",
                $"Marked the meet-up on {DescribeTime(schedule.ScheduledStartAt, schedule.DurationMinutes)} with {schedule.MeetupRequest.Case.Student.FullName} as completed");

            var s = await GetScheduleAsync(MeetupScheduleId);
            return new CompleteMeetupScheduleResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            };
        }

        // Mark Meetup schedule as missed: the advisor of the meet-up marks it as not held once it has started; the student is notified
        public async Task<MarkMissedMeetupScheduleResponse> MarkMissedScheduleAsync(Guid AdvisorId, Guid MeetupScheduleId)
        {
            var schedule = await GetScheduleForOutcomeAsync(AdvisorId, MeetupScheduleId);
            schedule.Status = "missed";
            _meetupScheduleRepository.PrepareUpdate(schedule);
            await _meetupScheduleRepository.SaveAsync();

            var caseRecord = schedule.MeetupRequest.Case;
            var when = DescribeTime(schedule.ScheduledStartAt, schedule.DurationMinutes);
            await _activityLogService.LogAsync(AdvisorId, "meetupMissed",
                $"Marked the meet-up on {when} with {caseRecord.Student.FullName} as missed");
            await _notificationService.NotifyAsync(caseRecord.StudentId, "Meet-up marked as missed",
                $"Your advisor marked the meet-up on {when} as missed. You can request a new meet-up on the case.",
                $"/cases/{caseRecord.CaseId}");

            var s = await GetScheduleAsync(MeetupScheduleId);
            return new MarkMissedMeetupScheduleResponse
            {
                MeetupScheduleId = s.MeetupScheduleId,
                AdvisorId = s.MeetupRequest.AdvisorId,
                CaseId = s.MeetupRequest.CaseId,
                CancelledBy = s.CancelledBy,
                MeetupRequestId = s.MeetupRequestId,
                StudentId = s.MeetupRequest.Case.StudentId,
                AdvisorFullName = s.MeetupRequest.Advisor?.FullName ?? string.Empty,
                CancelReason = s.CancelReason,
                CancelledByFullName = s.CancelledByNavigation?.FullName ?? string.Empty,
                CategoryName = s.MeetupRequest.Case.SupportCategory?.CategoryName ?? string.Empty,
                LocalDate = ToVietnamTime(s.ScheduledStartAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LocalEndTime = ToVietnamTime(s.ScheduledStartAt).AddMinutes(s.DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture),
                LocalStartTime = ToVietnamTime(s.ScheduledStartAt).ToString("HH:mm", CultureInfo.InvariantCulture),
                Location = s.Location,
                Reason = s.MeetupRequest.Reason,
                Room = s.Room,
                StudentFullName = s.MeetupRequest.Case.Student?.FullName ?? string.Empty,
                DurationMinutes = s.DurationMinutes,
                CancelledAt = s.CancelledAt,
                CreatedAt = s.CreatedAt,
                ScheduledEndAt = s.ScheduledStartAt.AddMinutes(s.DurationMinutes),
                ScheduledStartAt = s.ScheduledStartAt,
                Status = s.Status
            };
        }

        // Cancel the open meet-ups of a case: called after a case is closed or its advisor changes (reassignment, approved case change
        // request, accepted referral to another advisor); pending requests and scheduled meet-ups that have not started are cancelled
        // and both sides are notified
        public async Task CancelOpenMeetupsForCaseAsync(Guid CaseId, Guid ChangedBy, string Reason)
        {
            var now = DateTime.UtcNow;
            var pending = await _meetupRequestRepository.SearchAsync(r => r.CaseId == CaseId && r.Status == "pending");
            var scheduled = await _meetupScheduleRepository.SearchAsyncInclude(
                s => s.MeetupRequest.CaseId == CaseId && s.Status == "scheduled" && s.ScheduledStartAt > now,
                s => s.MeetupRequest);
            if (pending.Count == 0 && scheduled.Count == 0)
                return;

            foreach (var meetupRequest in pending)
            {
                meetupRequest.UpdatedAt = now;
                meetupRequest.Status = "cancelled";
                _meetupRequestRepository.PrepareUpdate(meetupRequest);
            }
            foreach (var schedule in scheduled)
            {
                schedule.CancelledBy = ChangedBy;
                schedule.CancelReason = Reason;
                schedule.CancelledAt = now;
                schedule.Status = "cancelled";
                _meetupScheduleRepository.PrepareUpdate(schedule);
            }
            await _meetupRequestRepository.SaveAsync();

            var caseRecord = await _caseRepository.GetByIdAsync(CaseId)
                ?? throw new NotFoundException("Case not found.");
            var advisorIds = pending.Select(r => r.AdvisorId).Concat(scheduled.Select(s => s.MeetupRequest.AdvisorId)).Distinct().ToList();
            var message = $"{pending.Count + scheduled.Count} meet-up(s) of this case were cancelled: {Reason}";
            await _notificationService.NotifyAsync(caseRecord.StudentId, "Meet-ups cancelled", message, $"/cases/{CaseId}");
            foreach (var advisorId in advisorIds)
                await _notificationService.NotifyAsync(advisorId, "Meet-ups cancelled", message, $"/cases/{CaseId}");
        }

        // Helper: load Meetup requests with their case (student, category), advisor and the people involved, newest first
        private async Task<List<Meetuprequest>> GetRequestsAsync(Expression<Func<Meetuprequest, bool>> Predicate)
        {
            return await _meetupRequestRepository.SearchAsyncIncludeOrderBy(Predicate, r => r.CreatedAt, false,
                r => r.Case, r => r.Case.Student, r => r.Case.SupportCategory, r => r.Advisor,
                r => r.ProposedByNavigation, r => r.RequestedByNavigation);
        }

        // Helper: load one Meetup request with its details
        private async Task<Meetuprequest> GetRequestAsync(Guid MeetupRequestId)
        {
            return (await GetRequestsAsync(r => r.MeetupRequestId == MeetupRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Meet-up request not found.");
        }

        // Helper: load Meetup schedules with their request, case (student, category), advisor and canceller, soonest first
        private async Task<List<Meetupschedule>> GetSchedulesAsync(Expression<Func<Meetupschedule, bool>> Predicate)
        {
            return await _meetupScheduleRepository.SearchAsyncIncludeOrderBy(Predicate, s => s.ScheduledStartAt, true,
                s => s.MeetupRequest, s => s.MeetupRequest.Case, s => s.MeetupRequest.Case.Student,
                s => s.MeetupRequest.Case.SupportCategory, s => s.MeetupRequest.Advisor, s => s.CancelledByNavigation);
        }

        // Helper: load one Meetup schedule with its details
        private async Task<Meetupschedule> GetScheduleAsync(Guid MeetupScheduleId)
        {
            return (await GetSchedulesAsync(s => s.MeetupScheduleId == MeetupScheduleId)).FirstOrDefault()
                ?? throw new NotFoundException("Meet-up not found.");
        }

        // Helper: a pending request that the caller must answer (the side that did not propose the current date), on a case that still allows meet-ups
        private async Task<Meetuprequest> GetPendingForAnswerAsync(Guid CallerId, Guid MeetupRequestId)
        {
            var meetupRequest = await GetRequestAsync(MeetupRequestId);
            if (!await CanReadAsync(meetupRequest, CallerId))
                throw new NotFoundException("Meet-up request not found.");
            if (meetupRequest.Status != "pending")
                throw new ConflictException("This meet-up request has already been answered or cancelled.");
            if (AwaitingResponseFrom(meetupRequest) != CallerId)
                throw new ForbiddenException(meetupRequest.ProposedBy == CallerId
                    ? "You proposed this date; the other side must answer. You can cancel the request instead."
                    : "Only the student of the case and the advisor of the request can answer it.");
            EnsureCaseAllowsMeetups(meetupRequest.Case);
            return meetupRequest;
        }

        // Helper: a scheduled meet-up of the calling advisor that has already started, for marking it completed or missed
        private async Task<Meetupschedule> GetScheduleForOutcomeAsync(Guid AdvisorId, Guid MeetupScheduleId)
        {
            var schedule = await GetScheduleAsync(MeetupScheduleId);
            if (schedule.MeetupRequest.AdvisorId != AdvisorId)
            {
                if (!await CanReadAsync(schedule.MeetupRequest, AdvisorId))
                    throw new NotFoundException("Meet-up not found.");
                throw new ForbiddenException("Only the advisor of the meet-up can mark it as completed or missed.");
            }
            if (schedule.Status != "scheduled")
                throw new ConflictException("Only a scheduled meet-up can be marked as completed or missed.");
            if (schedule.ScheduledStartAt > DateTime.UtcNow)
                throw new ConflictException("The meet-up has not started yet.");
            return schedule;
        }

        // Helper: who may read a request or its meet-up: the student of the case, the advisor of the request, and the staff who can see the case
        private async Task<bool> CanReadAsync(Meetuprequest MeetupRequest, Guid CallerId)
        {
            return MeetupRequest.Case.StudentId == CallerId || MeetupRequest.AdvisorId == CallerId ||
                   await _caseAccessService.CanViewCaseAsStaffAsync(MeetupRequest.Case, CallerId);
        }

        // Helper: meet-ups need a case that is not closed, has an advisor and is in stage contacted or followUp
        private static void EnsureCaseAllowsMeetups(Case CaseRecord)
        {
            if (CaseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (CaseRecord.AdvisorId == null)
                throw new ConflictException("The case has no advisor yet.");
            if (CaseRecord.CurrentStage != "contacted" && CaseRecord.CurrentStage != "followUp")
                throw new ConflictException("Meet-ups can be arranged once the case is in stage contacted or followUp.");
        }

        // Helper: the side that must answer a request: the student or the advisor, whichever did not propose the current date
        private static Guid AwaitingResponseFrom(Meetuprequest MeetupRequest)
        {
            return MeetupRequest.ProposedBy == MeetupRequest.Case.StudentId ? MeetupRequest.AdvisorId : MeetupRequest.Case.StudentId;
        }

        // Helper: check a date, start time and duration (Vietnam time) against the bookable windows and return the start in UTC
        private static DateTime ToBookableStartUtc(DateOnly MeetupDate, string StartTime, int DurationMinutes)
        {
            if (!TimeOnly.TryParseExact(StartTime, "HH:mm", out var start))
                throw new ValidationException("Start time must be in HH:mm format, e.g. 14:00.");
            if (MeetupDate.DayOfWeek == DayOfWeek.Saturday || MeetupDate.DayOfWeek == DayOfWeek.Sunday)
                throw new ValidationException("Meet-ups can only be booked from Monday to Friday.");
            if (start.Minute % SlotMinutes != 0)
                throw new ValidationException("The start time must be on a 15-minute step (e.g. 14:00, 14:15, 14:30).");
            if (DurationMinutes < SlotMinutes || DurationMinutes % SlotMinutes != 0)
                throw new ValidationException("The duration must be a multiple of 15 minutes, at least 15 minutes.");
            var startMinute = start.Hour * 60 + start.Minute;
            var endMinute = startMinute + DurationMinutes;
            var fitsWindow = TimeWindows.Any(w =>
                startMinute >= w.Start.Hour * 60 + w.Start.Minute && endMinute <= w.End.Hour * 60 + w.End.Minute);
            if (!fitsWindow)
                throw new ValidationException("A meet-up must start and end inside 08:30-11:30 or 13:30-18:00 (Vietnam time).");

            var startUtc = DateTime.SpecifyKind(MeetupDate.ToDateTime(start) - VietnamOffset, DateTimeKind.Utc);
            if (startUtc <= DateTime.UtcNow)
                throw new ValidationException("The meet-up time must be in the future.");
            return startUtc;
        }

        // Helper: refuse a time that overlaps a scheduled meet-up of the advisor or of the student
        private async Task EnsureNoOverlapAsync(Guid AdvisorId, Guid StudentId, DateTime StartUtc, int DurationMinutes)
        {
            var endUtc = StartUtc.AddMinutes(DurationMinutes);
            var earliest = StartUtc.AddMinutes(-270);
            var candidates = await _meetupScheduleRepository.SearchAsyncInclude(
                s => s.Status == "scheduled" && s.ScheduledStartAt < endUtc && s.ScheduledStartAt > earliest &&
                     (s.MeetupRequest.AdvisorId == AdvisorId || s.MeetupRequest.Case.StudentId == StudentId),
                s => s.MeetupRequest, s => s.MeetupRequest.Case);
            var clash = candidates.FirstOrDefault(s => s.ScheduledStartAt.AddMinutes(s.DurationMinutes) > StartUtc);
            if (clash != null)
                throw new ConflictException(clash.MeetupRequest.AdvisorId == AdvisorId
                    ? $"The advisor already has a meet-up at {DescribeTime(clash.ScheduledStartAt, clash.DurationMinutes)}. Choose another time."
                    : $"The student already has a meet-up at {DescribeTime(clash.ScheduledStartAt, clash.DurationMinutes)}. Choose another time.");
        }

        // Helper: a date range of Vietnam dates (inclusive) as a UTC range
        private static (DateTime? FromUtc, DateTime? ToUtc) ToUtcRange(DateOnly? FromDate, DateOnly? ToDate)
        {
            if (FromDate != null && ToDate != null && ToDate < FromDate)
                throw new ValidationException("The end date cannot be before the start date.");
            DateTime? fromUtc = FromDate == null ? null : FromDate.Value.ToDateTime(TimeOnly.MinValue) - VietnamOffset;
            DateTime? toUtc = ToDate == null ? null : ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - VietnamOffset;
            return (fromUtc, toUtc);
        }

        // Helper: a UTC time as Vietnam time
        private static DateTime ToVietnamTime(DateTime Utc)
        {
            return Utc + VietnamOffset;
        }

        // Helper: "Mon 01/12/2026 14:00-15:00" (Vietnam time) for messages and logs
        private static string DescribeTime(DateTime StartUtc, int DurationMinutes)
        {
            var start = ToVietnamTime(StartUtc);
            return start.ToString("ddd dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + "-" +
                start.AddMinutes(DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        // Helper: "Campus Beta building, Room 305"
        private static string DescribePlace(string Location, string? Room)
        {
            return string.IsNullOrWhiteSpace(Room) ? Location : $"{Location}, {Room}";
        }

        // Helper: notify the student or the advisor of the request, whichever did not act
        private async Task NotifyOtherSideAsync(Meetuprequest MeetupRequest, Case CaseRecord, Guid ActorId, string Title, string Message)
        {
            var otherSide = ActorId == CaseRecord.StudentId ? MeetupRequest.AdvisorId : CaseRecord.StudentId;
            await _notificationService.NotifyAsync(otherSide, Title, Message, $"/cases/{CaseRecord.CaseId}");
        }
    }
}
