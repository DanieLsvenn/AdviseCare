using AdviseCare.Application.Contracts.Requests.CaseAdvisorUpdate;
using AdviseCare.Application.Contracts.Responses.CaseAdvisorUpdate;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class CaseAdvisorUpdateService : ICaseAdvisorUpdateService
    {
        private const int MaxMessageLength = 1000;

        private readonly IGenericRepository<CaseAdvisorUpdate> _updateRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly ICaseAccessService _caseAccessService;
        private readonly IActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public CaseAdvisorUpdateService(
            IGenericRepository<CaseAdvisorUpdate> updateRepository,
            IGenericRepository<Case> caseRepository,
            ICaseAccessService caseAccessService,
            IActivityLogService activityLogService,
            INotificationService notificationService)
        {
            _updateRepository = updateRepository;
            _caseRepository = caseRepository;
            _caseAccessService = caseAccessService;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
        }

        public async Task<List<CaseAdvisorUpdateResponse>> GetForCaseAsync(Guid CallerId, Guid CaseId)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            if (caseRecord.StudentId != CallerId && !await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, CallerId))
                throw new NotFoundException("Case not found.");

            var updates = await _updateRepository.SearchAsyncIncludeOrderBy(
                update => update.CaseId == CaseId,
                update => update.CreatedAt,
                true,
                update => update.Author);

            return updates
                .OrderBy(update => update.CreatedAt)
                .ThenBy(update => update.CaseAdvisorUpdateId)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<CaseAdvisorUpdateResponse> CreateAsync(
            Guid AuthorId,
            Guid CaseId,
            CreateCaseAdvisorUpdateRequest request)
        {
            var caseRecord = await GetCaseAsync(CaseId);
            if (caseRecord.AdvisorId != AuthorId)
            {
                if (!await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, AuthorId))
                    throw new NotFoundException("Case not found.");
                throw new ForbiddenException("Only the case's current advisor can publish student-visible updates.");
            }

            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed. Reopen it to publish an update.");

            var messageText = request.MessageText?.Trim() ?? string.Empty;
            if (messageText.Length == 0)
                throw new ValidationException("Update text is required.");
            if (messageText.Length > MaxMessageLength)
                throw new ValidationException($"Update text cannot exceed {MaxMessageLength} characters.");

            var update = new CaseAdvisorUpdate
            {
                CaseAdvisorUpdateId = Guid.NewGuid(),
                AuthorId = AuthorId,
                CaseId = CaseId,
                MessageText = messageText,
                CreatedAt = DateTime.UtcNow
            };
            await _updateRepository.CreateAsync(update);

            var created = await _updateRepository.GetByIdAsyncInclude(update.CaseAdvisorUpdateId, item => item.Author)
                ?? throw new NotFoundException("Advisor update not found.");

            await _activityLogService.LogAsync(
                AuthorId,
                "caseAdvisorUpdateSent",
                $"Published a student-visible update on case {CaseId}.");
            await _notificationService.NotifyAsync(
                caseRecord.StudentId,
                "Your advisor sent an update",
                "Your advisor posted an update to your support case. Sign in to read it.",
                $"/cases/{CaseId}");

            return ToResponse(created);
        }

        private async Task<Case> GetCaseAsync(Guid CaseId)
        {
            return await _caseRepository.GetByIdAsync(CaseId)
                ?? throw new NotFoundException("Case not found.");
        }

        private static CaseAdvisorUpdateResponse ToResponse(CaseAdvisorUpdate update)
        {
            return new CaseAdvisorUpdateResponse
            {
                CaseAdvisorUpdateId = update.CaseAdvisorUpdateId,
                CaseId = update.CaseId,
                AuthorId = update.AuthorId,
                AuthorFullName = update.Author?.FullName ?? string.Empty,
                MessageText = update.MessageText,
                CreatedAt = update.CreatedAt
            };
        }
    }
}
