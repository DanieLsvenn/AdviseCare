using AdviseCare.Application.Contracts.Requests.CaseMessage;
using AdviseCare.Application.Contracts.Responses.CaseMessage;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Case messages (CASEMESSAGES): a two-way thread on one case between its student and its current advisor.
    // Messages are append-only (no edit or delete) and cannot be added once the case is closed; each message notifies the other side.
    // The thread is read by the student, the current advisor, the Programme Head and anyone who sent or received a referral of the case.
    // Private advisor records stay in CASENOTES
    public class CaseMessageService : ICaseMessageService
    {
        private const string StudentAuthor = "student";
        private const string AdvisorAuthor = "advisor";

        private readonly IGenericRepository<Casemessage> _caseMessageRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly ICaseAccessService _caseAccessService;
        private readonly IActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public CaseMessageService(
            IGenericRepository<Casemessage> caseMessageRepository,
            IGenericRepository<Case> caseRepository,
            ICaseAccessService caseAccessService,
            IActivityLogService activityLogService,
            INotificationService notificationService)
        {
            _caseMessageRepository = caseMessageRepository;
            _caseRepository = caseRepository;
            _caseAccessService = caseAccessService;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
        }

        // Get all Case messages written by the signed-in account, newest first
        public async Task<IEnumerable<GetAllCaseMessageResponse>> GetAll(Guid AuthorId)
        {
            var messages = await _caseMessageRepository.SearchAsyncIncludeOrderBy(m => m.AuthorId == AuthorId, m => m.CreatedAt, false, m => m.Author, m => m.Case);

            return messages.Select(m => new GetAllCaseMessageResponse
            {
                CaseMessageId = m.CaseMessageId,
                AuthorId = m.AuthorId,
                CaseId = m.CaseId,
                AuthorFullName = m.Author?.FullName ?? string.Empty,
                AuthorRole = AuthorRoleOf(m),
                MessageText = m.MessageText,
                CreatedAt = m.CreatedAt
            });
        }

        // Get all Case messages written by the signed-in account with pagination
        public async Task<IEnumerable<GetAllCaseMessageWithPaginationResponse>> GetAllWithPaginationAsync(Guid AuthorId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var messages = await _caseMessageRepository.SearchAsyncIncludeOrderBy(m => m.AuthorId == AuthorId, m => m.CreatedAt, false, m => m.Author, m => m.Case);

            return messages
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new GetAllCaseMessageWithPaginationResponse
                {
                    CaseMessageId = m.CaseMessageId,
                    AuthorId = m.AuthorId,
                    CaseId = m.CaseId,
                    AuthorFullName = m.Author?.FullName ?? string.Empty,
                    AuthorRole = AuthorRoleOf(m),
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt
                });
        }

        // Get Case message by Id (same readers as the thread)
        public async Task<GetCaseMessageByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseMessageId)
        {
            var message = await _caseMessageRepository.GetByIdAsyncInclude(CaseMessageId, m => m.Author, m => m.Case)
                ?? throw new NotFoundException("Case message not found.");
            if (!await CanReadThreadAsync(message.Case, CallerId))
                throw new NotFoundException("Case message not found.");

            return new GetCaseMessageByIdResponse
            {
                CaseMessageId = message.CaseMessageId,
                AuthorId = message.AuthorId,
                CaseId = message.CaseId,
                AuthorFullName = message.Author?.FullName ?? string.Empty,
                AuthorRole = AuthorRoleOf(message),
                MessageText = message.MessageText,
                CreatedAt = message.CreatedAt
            };
        }

        // Search Case messages of one case: the whole thread, oldest first
        public async Task<List<SearchCaseMessageResponse>> SearchAsync(Guid CallerId, SearchCaseMessageRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (!await CanReadThreadAsync(caseRecord, CallerId))
                throw new NotFoundException("Case not found.");

            var messages = await _caseMessageRepository.SearchAsyncIncludeOrderBy(m => m.CaseId == request.CaseId, m => m.CreatedAt, true, m => m.Author, m => m.Case);

            return messages
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.CaseMessageId)
                .Select(m => new SearchCaseMessageResponse
                {
                    CaseMessageId = m.CaseMessageId,
                    AuthorId = m.AuthorId,
                    CaseId = m.CaseId,
                    AuthorFullName = m.Author?.FullName ?? string.Empty,
                    AuthorRole = AuthorRoleOf(m),
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt
                }).ToList();
        }

        // Create Case message: the case's student or its current advisor writes in the thread of a case that is not closed;
        // the other side is notified
        public async Task<CreateCaseMessageResponse> CreateAsync(Guid CreatorId, CreateCaseMessageRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            var isStudent = caseRecord.StudentId == CreatorId;
            var isAdvisor = caseRecord.AdvisorId == CreatorId;
            if (!isStudent && !isAdvisor)
            {
                if (!await _caseAccessService.CanViewCaseAsStaffAsync(caseRecord, CreatorId))
                    throw new NotFoundException("Case not found.");
                throw new ForbiddenException("Only the student of the case and its current advisor can write messages.");
            }
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed. Messages can be written again if the advisor reopens it.");
            if (isStudent && caseRecord.AdvisorId == null)
                throw new ConflictException("Your case has no advisor yet. You can write once an advisor is assigned.");

            var messageText = request.MessageText.Trim();
            if (messageText.Length == 0)
                throw new ValidationException("Message text is required.");
            if (messageText.Length > 1000)
                throw new ValidationException("Message text cannot exceed 1000 characters.");

            var message = new Casemessage
            {
                CaseMessageId = Guid.NewGuid(),
                AuthorId = CreatorId,
                CaseId = caseRecord.CaseId,
                MessageText = messageText,
                CreatedAt = DateTime.UtcNow
            };
            await _caseMessageRepository.CreateAsync(message);

            await _activityLogService.LogAsync(CreatorId, "caseMessageSent", $"Wrote a message on case {caseRecord.CaseId}");
            if (isStudent)
                await _notificationService.NotifyAsync(caseRecord.AdvisorId!.Value, "New message from your student",
                    "The student of one of your cases wrote a message. Open the case to read it and reply.",
                    $"/cases/{caseRecord.CaseId}");
            else
                await _notificationService.NotifyAsync(caseRecord.StudentId, "New message from your advisor",
                    "Your advisor wrote a message on your support case. Open the case to read it and reply.",
                    $"/cases/{caseRecord.CaseId}");

            var created = await _caseMessageRepository.GetByIdAsyncInclude(message.CaseMessageId, m => m.Author, m => m.Case)
                ?? throw new NotFoundException("Case message not found.");

            return new CreateCaseMessageResponse
            {
                CaseMessageId = created.CaseMessageId,
                AuthorId = created.AuthorId,
                CaseId = created.CaseId,
                AuthorFullName = created.Author?.FullName ?? string.Empty,
                AuthorRole = AuthorRoleOf(created),
                MessageText = created.MessageText,
                CreatedAt = created.CreatedAt
            };
        }

        // Helper: the thread is read by the student of the case and by its staff (current advisor, Programme Head, referral sender or receiver)
        private async Task<bool> CanReadThreadAsync(Case CaseRecord, Guid CallerId)
        {
            return CaseRecord.StudentId == CallerId || await _caseAccessService.CanViewCaseAsStaffAsync(CaseRecord, CallerId);
        }

        // Helper: whether a message was written by the student of the case or by an advisor
        private static string AuthorRoleOf(Casemessage Message)
        {
            return Message.Case != null && Message.AuthorId == Message.Case.StudentId ? StudentAuthor : AdvisorAuthor;
        }
    }
}
