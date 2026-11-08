using AdviseCare.Application.Contracts.Requests.CaseMessage;
using AdviseCare.Application.Contracts.Responses.CaseMessage;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;

namespace AdviseCare.Application.Services
{
    // Case messages (CASEMESSAGES): a two-way thread on one case between its student and its current advisor.
    // Messages are append-only (no edit or delete) and cannot be added once the case is closed; each message notifies the other side.
    // The thread is read by the student, the current advisor, the Programme Head and anyone who sent or received a referral of the case.
    // Private advisor records stay in CASENOTES.
    // A message can carry up to 5 files (CASEMESSAGEATTACHMENTS, 10 MB each, documents and images only), stored in the database
    // and downloaded only through the API by the readers of the thread
    public class CaseMessageService : ICaseMessageService
    {
        private const string StudentAuthor = "student";
        private const string AdvisorAuthor = "advisor";
        private const int MaxFilesPerMessage = 5;
        private const int MaxFileBytes = 10 * 1024 * 1024;

        // Allowed extensions and the content type each is served with (the browser's own type is not trusted)
        private static readonly Dictionary<string, string> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        private readonly IGenericRepository<Casemessage> _caseMessageRepository;
        private readonly IGenericRepository<Casemessageattachment> _attachmentRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly ICaseAccessService _caseAccessService;
        private readonly IActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public CaseMessageService(
            IGenericRepository<Casemessage> caseMessageRepository,
            IGenericRepository<Casemessageattachment> attachmentRepository,
            IGenericRepository<Case> caseRepository,
            ICaseAccessService caseAccessService,
            IActivityLogService activityLogService,
            INotificationService notificationService)
        {
            _caseMessageRepository = caseMessageRepository;
            _attachmentRepository = attachmentRepository;
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
                CreatedAt = message.CreatedAt,
                Attachments = GetAttachments(new[] { message.CaseMessageId }).GetValueOrDefault(message.CaseMessageId, new List<CaseMessageAttachmentResponse>())
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
            var attachments = GetAttachments(messages.Select(m => m.CaseMessageId).ToList());

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
                    CreatedAt = m.CreatedAt,
                    Attachments = attachments.GetValueOrDefault(m.CaseMessageId, new List<CaseMessageAttachmentResponse>())
                }).ToList();
        }

        // Create Case message: the case's student or its current advisor writes in the thread of a case that is not closed;
        // the other side is notified
        public async Task<CreateCaseMessageResponse> CreateAsync(Guid CreatorId, CreateCaseMessageRequest request)
        {
            return await CreateMessageAsync(CreatorId, request.CaseId, request.MessageText, new List<IFormFile>());
        }

        // Create Case message with files: same rules as a plain message, plus up to 5 files of 10 MB (documents and images);
        // the message and its files are saved together
        public async Task<CreateCaseMessageResponse> CreateWithFilesAsync(Guid CreatorId, CreateCaseMessageWithFilesRequest request)
        {
            return await CreateMessageAsync(CreatorId, request.CaseId, request.MessageText, request.Files ?? new List<IFormFile>());
        }

        // Download a file of a case message: the readers of the thread only (others get 404, so ids cannot be probed)
        public async Task<DownloadCaseMessageAttachmentResponse> DownloadAttachmentAsync(Guid CallerId, Guid CaseMessageAttachmentId)
        {
            var attachment = await _attachmentRepository.GetByIdAsyncInclude(CaseMessageAttachmentId, a => a.CaseMessage, a => a.CaseMessage.Case)
                ?? throw new NotFoundException("File not found.");
            if (!await CanReadThreadAsync(attachment.CaseMessage.Case, CallerId))
                throw new NotFoundException("File not found.");

            return new DownloadCaseMessageAttachmentResponse
            {
                Content = attachment.FileContent,
                ContentType = attachment.ContentType,
                FileName = attachment.FileName
            };
        }

        // Helper: write a message (and its files) on a case
        private async Task<CreateCaseMessageResponse> CreateMessageAsync(Guid CreatorId, Guid CaseId, string MessageText, List<IFormFile> Files)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(CaseId)
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

            var messageText = (MessageText ?? string.Empty).Trim();
            if (messageText.Length == 0)
                throw new ValidationException("Message text is required.");
            if (messageText.Length > 1000)
                throw new ValidationException("Message text cannot exceed 1000 characters.");

            var files = await ReadFilesAsync(Files);

            var message = new Casemessage
            {
                CaseMessageId = Guid.NewGuid(),
                AuthorId = CreatorId,
                CaseId = caseRecord.CaseId,
                MessageText = messageText,
                CreatedAt = DateTime.UtcNow
            };
            // Staged so the message and its files commit together
            _caseMessageRepository.PrepareCreate(message);
            foreach (var file in files)
            {
                _attachmentRepository.PrepareCreate(new Casemessageattachment
                {
                    CaseMessageAttachmentId = Guid.NewGuid(),
                    CaseMessageId = message.CaseMessageId,
                    UploadedBy = CreatorId,
                    ContentType = file.ContentType,
                    FileContent = file.Content,
                    FileName = file.FileName,
                    FileSize = file.Content.Length,
                    CreatedAt = message.CreatedAt
                });
            }
            await _caseMessageRepository.SaveAsync();

            await _activityLogService.LogAsync(CreatorId, "caseMessageSent",
                files.Count == 0
                    ? $"Wrote a message on case {caseRecord.CaseId}"
                    : $"Wrote a message with {files.Count} file(s) on case {caseRecord.CaseId}: {string.Join(", ", files.Select(f => f.FileName))}");
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
                CreatedAt = created.CreatedAt,
                Attachments = GetAttachments(new[] { created.CaseMessageId }).GetValueOrDefault(created.CaseMessageId, new List<CaseMessageAttachmentResponse>())
            };
        }

        // Helper: check and read the uploaded files (count, size, allowed type, file name)
        private static async Task<List<UploadedFile>> ReadFilesAsync(List<IFormFile> Files)
        {
            var uploads = Files.Where(f => f != null && f.Length > 0).ToList();
            if (Files.Any(f => f != null && f.Length == 0))
                throw new ValidationException("One of the files is empty.");
            if (uploads.Count > MaxFilesPerMessage)
                throw new ValidationException($"A message can carry at most {MaxFilesPerMessage} files.");

            var result = new List<UploadedFile>();
            foreach (var upload in uploads)
            {
                var fileName = Path.GetFileName(upload.FileName ?? string.Empty).Trim();
                if (fileName.Length == 0)
                    throw new ValidationException("Every file needs a name.");
                if (fileName.Length > 255)
                    throw new ValidationException($"The file name '{fileName[..40]}...' is longer than 255 characters.");
                if (upload.Length > MaxFileBytes)
                    throw new ValidationException($"'{fileName}' is larger than 10 MB.");
                var extension = Path.GetExtension(fileName);
                if (!AllowedFileTypes.TryGetValue(extension, out var contentType))
                    throw new ValidationException($"'{fileName}' cannot be attached. Allowed: PDF, Word, Excel, PowerPoint, text, CSV and images (PNG, JPG, GIF, WEBP).");

                using var stream = new MemoryStream();
                await upload.CopyToAsync(stream);
                result.Add(new UploadedFile(fileName, contentType, stream.ToArray()));
            }
            return result;
        }

        // Helper: the files of some messages, without their content (listing a thread never loads the file bytes)
        private Dictionary<Guid, List<CaseMessageAttachmentResponse>> GetAttachments(IEnumerable<Guid> CaseMessageIds)
        {
            var ids = CaseMessageIds.ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, List<CaseMessageAttachmentResponse>>();

            return _attachmentRepository.GetQueryable()
                .Where(a => ids.Contains(a.CaseMessageId))
                .OrderBy(a => a.CreatedAt)
                .ThenBy(a => a.FileName)
                .Select(a => new
                {
                    a.CaseMessageId,
                    a.CaseMessageAttachmentId,
                    a.ContentType,
                    a.FileName,
                    a.FileSize
                })
                .ToList()
                .GroupBy(a => a.CaseMessageId)
                .ToDictionary(g => g.Key, g => g.Select(a => new CaseMessageAttachmentResponse
                {
                    CaseMessageAttachmentId = a.CaseMessageAttachmentId,
                    ContentType = a.ContentType,
                    FileName = a.FileName,
                    FileSize = a.FileSize
                }).ToList());
        }

        // Helper type: an uploaded file that passed the checks
        private record UploadedFile(string FileName, string ContentType, byte[] Content);

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
