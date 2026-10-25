using AdviseCare.Application.Contracts.Requests.AccessLog;
using AdviseCare.Application.Contracts.Responses.AccessLog;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;
using System.Text;

namespace AdviseCare.Application.Services
{
    public class AccessLogService : IAccessLogService
    {
        // Shown instead of a purpose name for reads made through an approved emergency access (consentPurposeId = null)
        private const string EmergencyAccessLabel = "Emergency access";

        private readonly IGenericRepository<Accesslog> _accessLogRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IExcelWriter _excelWriter;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public AccessLogService(
            IGenericRepository<Accesslog> accessLogRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IExcelWriter excelWriter,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _accessLogRepository = accessLogRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _excelWriter = excelWriter;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Access logs
        public async Task<IEnumerable<GetAllAccessLogResponse>> GetAll()
        {
            var logs = await GetLogsAsync(l => true);

            return logs.Select(l => new GetAllAccessLogResponse
            {
                AccessLogId = l.AccessLogId,
                AccessorId = l.AccessorId,
                ConsentPurposeId = l.ConsentPurposeId,
                RecordedUserId = l.RecordedUserId,
                AccessorFullName = l.Accessor.FullName,
                PurposeName = l.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                RecordedUserFullName = l.RecordedUser.FullName,
                AccessedResource = l.AccessedResource,
                AccessedAt = l.AccessedAt,
                IsFlagged = l.IsFlagged,
                Status = l.Status ?? string.Empty
            });
        }

        // Get all Access logs with pagination
        public async Task<IEnumerable<GetAllAccessLogWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var logs = await GetLogsAsync(l => true);

            return logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new GetAllAccessLogWithPaginationResponse
                {
                    AccessLogId = l.AccessLogId,
                    AccessorId = l.AccessorId,
                    ConsentPurposeId = l.ConsentPurposeId,
                    RecordedUserId = l.RecordedUserId,
                    AccessorFullName = l.Accessor.FullName,
                    PurposeName = l.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                    RecordedUserFullName = l.RecordedUser.FullName,
                    AccessedResource = l.AccessedResource,
                    AccessedAt = l.AccessedAt,
                    IsFlagged = l.IsFlagged,
                    Status = l.Status ?? string.Empty
                });
        }

        // Get Access log by Id
        public async Task<GetAccessLogByIdResponse> GetByIdAsync(Guid AccessLogId)
        {
            var log = await _accessLogRepository.GetByIdAsyncInclude(AccessLogId, l => l.Accessor, l => l.ConsentPurpose, l => l.RecordedUser)
                ?? throw new NotFoundException("Access log entry not found.");

            return new GetAccessLogByIdResponse
            {
                AccessLogId = log.AccessLogId,
                AccessorId = log.AccessorId,
                ConsentPurposeId = log.ConsentPurposeId,
                RecordedUserId = log.RecordedUserId,
                AccessorFullName = log.Accessor.FullName,
                PurposeName = log.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                RecordedUserFullName = log.RecordedUser.FullName,
                AccessedResource = log.AccessedResource,
                AccessedAt = log.AccessedAt,
                IsFlagged = log.IsFlagged,
                Status = log.Status ?? string.Empty
            };
        }

        // Search Access logs by accessor, purpose, student, date range, flag or review status
        public async Task<List<SearchAccessLogResponse>> SearchAsync(SearchAccessLogRequest request)
        {
            var logs = await GetLogsAsync(l =>
                (request.AccessorId == null || l.AccessorId == request.AccessorId) &&
                (request.ConsentPurposeId == null || l.ConsentPurposeId == request.ConsentPurposeId) &&
                (request.RecordedUserId == null || l.RecordedUserId == request.RecordedUserId) &&
                (request.FromDate == null || l.AccessedAt >= request.FromDate) &&
                (request.ToDate == null || l.AccessedAt <= request.ToDate) &&
                (request.IsFlagged == null || l.IsFlagged == request.IsFlagged) &&
                (string.IsNullOrWhiteSpace(request.Status) || l.Status == request.Status));

            return logs.Select(l => new SearchAccessLogResponse
            {
                AccessLogId = l.AccessLogId,
                AccessorId = l.AccessorId,
                ConsentPurposeId = l.ConsentPurposeId,
                RecordedUserId = l.RecordedUserId,
                AccessorFullName = l.Accessor.FullName,
                PurposeName = l.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                RecordedUserFullName = l.RecordedUser.FullName,
                AccessedResource = l.AccessedResource,
                AccessedAt = l.AccessedAt,
                IsFlagged = l.IsFlagged,
                Status = l.Status ?? string.Empty
            }).ToList();
        }

        // Review a flagged Access log entry (only the review fields change; the entry itself is never edited)
        public async Task<bool> ReviewAsync(Guid AccessLogId, ReviewAccessLogRequest request)
        {
            var log = await _accessLogRepository.GetByIdAsyncInclude(AccessLogId, l => l.Accessor, l => l.RecordedUser)
                ?? throw new NotFoundException("Access log entry not found.");

            if (!log.IsFlagged)
                throw new ConflictException("Only a flagged entry can be reviewed.");

            log.Status = request.Status;
            await _accessLogRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accessLogReviewed",
                $"Reviewed the access by {log.Accessor.FullName} to {log.RecordedUser.FullName}'s {log.AccessedResource} on {log.AccessedAt:yyyy-MM-dd HH:mm}: {log.Status}");
            return true;
        }

        // Flag an Access log entry for follow-up (Steward)
        public async Task<bool> FlagAsync(Guid AccessLogId)
        {
            var log = await _accessLogRepository.GetByIdAsyncInclude(AccessLogId, l => l.Accessor, l => l.RecordedUser)
                ?? throw new NotFoundException("Access log entry not found.");

            if (log.IsFlagged)
                throw new ConflictException("This entry is already flagged.");

            log.IsFlagged = true;
            log.Status = "unreviewed";
            await _accessLogRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accessLogEntryFlagged",
                $"Flagged the access by {log.Accessor.FullName} to {log.RecordedUser.FullName}'s {log.AccessedResource} on {log.AccessedAt:yyyy-MM-dd HH:mm}");
            return true;
        }

        // Get the access log of a Student's own record, filtered by date range or the accessor's role
        public async Task<List<GetMyAccessLogResponse>> GetMyAccessLogsAsync(Guid StudentId, SearchMyAccessLogRequest request)
        {
            var logs = await GetLogsAsync(l =>
                l.RecordedUserId == StudentId &&
                (request.FromDate == null || l.AccessedAt >= request.FromDate) &&
                (request.ToDate == null || l.AccessedAt <= request.ToDate));

            var accessorIds = logs.Select(l => l.AccessorId).Distinct().ToList();
            var accessorRoles = (await _accountRoleRepository.SearchAsyncInclude(ar => accessorIds.Contains(ar.AccountId), ar => ar.Role))
                .GroupBy(ar => ar.AccountId)
                .ToDictionary(g => g.Key, g => g.Select(ar => ar.Role.RoleName).OrderBy(r => r).ToList());

            return logs
                .Where(l => string.IsNullOrWhiteSpace(request.RoleName) ||
                    (accessorRoles.TryGetValue(l.AccessorId, out var roles) && roles.Contains(request.RoleName)))
                .Select(l => new GetMyAccessLogResponse
                {
                    AccessLogId = l.AccessLogId,
                    ConsentPurposeId = l.ConsentPurposeId,
                    AccessorFullName = l.Accessor.FullName,
                    AccessorRoleNames = accessorRoles.TryGetValue(l.AccessorId, out var roles) ? roles : new List<string>(),
                    PurposeName = l.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                    AccessedResource = l.AccessedResource,
                    AccessedAt = l.AccessedAt,
                    IsFlagged = l.IsFlagged,
                    Status = l.Status ?? string.Empty
                }).ToList();
        }

        // Flag an entry of the Student's own access log as suspicious (every Steward is notified)
        public async Task<bool> FlagMineAsync(Guid StudentId, Guid AccessLogId)
        {
            var log = await _accessLogRepository.GetByIdAsyncInclude(AccessLogId, l => l.Accessor);
            if (log == null || log.RecordedUserId != StudentId)
                throw new NotFoundException("Access log entry not found.");
            if (log.IsFlagged)
                throw new ConflictException("This entry is already flagged.");

            log.IsFlagged = true;
            log.Status = "unreviewed";
            await _accessLogRepository.SaveAsync();

            var stewards = await _accountRoleRepository.SearchAsync(ar => ar.Role.RoleName == "Data Protection Steward" && ar.Account.Status == "active");
            foreach (var steward in stewards)
            {
                await _notificationService.NotifyAsync(steward.AccountId, "Access log entry flagged",
                    $"A student flagged an access by {log.Accessor.FullName} on {log.AccessedAt:yyyy-MM-dd HH:mm} for review.", "/access-logs?isFlagged=true");
            }
            await _activityLogService.LogCurrentAccountAsync("accessLogEntryFlagged",
                $"Flagged the access by {log.Accessor.FullName} on {log.AccessedAt:yyyy-MM-dd HH:mm}");
            return true;
        }

        // Export a processing record (one student and/or a date range) as .xlsx or .csv, generated on the fly
        public async Task<ExportProcessingRecordResponse> ExportProcessingRecordAsync(ExportProcessingRecordRequest request)
        {
            if (request.StudentId == null && (request.FromDate == null || request.ToDate == null))
                throw new ValidationException("Choose a student, or both a start and an end date.");
            if (request.FromDate != null && request.ToDate != null && request.ToDate < request.FromDate)
                throw new ValidationException("The end date must be after the start date.");

            Account? student = null;
            if (request.StudentId != null)
            {
                student = await _accountRepository.GetByIdAsync(request.StudentId.Value)
                    ?? throw new NotFoundException("Student not found.");
            }

            var logs = (await GetLogsAsync(l =>
                    (request.StudentId == null || l.RecordedUserId == request.StudentId) &&
                    (request.FromDate == null || l.AccessedAt >= request.FromDate) &&
                    (request.ToDate == null || l.AccessedAt <= request.ToDate)))
                .OrderBy(l => l.AccessedAt)
                .ToList();

            var headers = new List<string> { "Accessed at (UTC)", "Accessor", "Student", "Purpose", "Accessed resource", "Flagged", "Review status" };
            var rows = logs.Select(l => new List<string>
            {
                l.AccessedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                l.Accessor.FullName,
                l.RecordedUser.FullName,
                l.ConsentPurpose?.PurposeName ?? EmergencyAccessLabel,
                l.AccessedResource,
                l.IsFlagged ? "yes" : "no",
                l.Status ?? string.Empty
            }).ToList();

            var scope = student != null ? student.UserName : "all";
            var range = request.FromDate != null && request.ToDate != null
                ? $"{request.FromDate:yyyyMMdd}-{request.ToDate:yyyyMMdd}"
                : DateTime.UtcNow.ToString("yyyyMMdd");
            var fileName = $"processing_record_{scope}_{range}.{request.Format}";

            await _activityLogService.LogCurrentAccountAsync("processingRecordExported",
                $"Exported a processing record ({(student != null ? student.FullName : "all students")}, {range}, {rows.Count} entries) as {request.Format}");

            return new ExportProcessingRecordResponse
            {
                ContentType = request.Format == "csv"
                    ? "text/csv"
                    : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName,
                Content = request.Format == "csv"
                    ? ToCsv(headers, rows)
                    : _excelWriter.WriteRows("Processing record", headers, rows)
            };
        }

        // Helper: load Access logs with accessor, purpose and student, newest first
        private async Task<List<Accesslog>> GetLogsAsync(Expression<Func<Accesslog, bool>> predicate)
        {
            return await _accessLogRepository.SearchAsyncIncludeOrderBy(predicate, l => l.AccessedAt, false,
                l => l.Accessor, l => l.ConsentPurpose, l => l.RecordedUser);
        }

        // Helper: build a UTF-8 CSV (with BOM so Excel shows Vietnamese names correctly)
        private static byte[] ToCsv(List<string> headers, List<List<string>> rows)
        {
            static string Escape(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;

            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
                builder.AppendLine(string.Join(",", row.Select(Escape)));
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }
    }
}
