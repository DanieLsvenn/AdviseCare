using System.Globalization;
using System.Text;
using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Contracts.Responses.Report;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D6 / WF4: Programme Head reports over the cases opened in a date range. Generated on the fly; nothing is stored
    // (REPORTS stays unused until the "stored report history" future item).
    public class CaseReportService(
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Studentprofile> studentProfileRepository,
        IExcelWriter excelWriter,
        IPdfWriter pdfWriter,
        IActivityLogService activityLogService) : ICaseReportService
    {
        private static readonly string[] Formats = { "xlsx", "csv", "pdf" };

        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository = studentProfileRepository;
        private readonly IExcelWriter _excelWriter = excelWriter;
        private readonly IPdfWriter _pdfWriter = pdfWriter;
        private readonly IActivityLogService _activityLogService = activityLogService;

        public async Task<List<CaseReportRowResponse>> GetCasesAsync(CaseReportRequest request)
        {
            var (from, toExclusive) = ValidateRange(request.From, request.To);
            var supportType = CaseWorkflowHelper.OptionalFilter(request.SupportType, CaseRules.SupportTypes, "Support type");
            var stage = CaseWorkflowHelper.OptionalFilter(request.CurrentStage, CaseRules.Stages, "Current stage");
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, CaseRules.CaseStatuses, "Status");

            var cases = await _caseRepository.SearchAsyncIncludeOrderBy(
                c => c.OpenedAt >= from && c.OpenedAt < toExclusive &&
                     (!request.AdvisorId.HasValue || c.AdvisorId == request.AdvisorId.Value) &&
                     (supportType == null || c.SupportCategory.SupportType == supportType) &&
                     (!request.SupportCategoryId.HasValue || c.SupportCategoryId == request.SupportCategoryId.Value) &&
                     (!request.SubjectId.HasValue || c.SubjectId == request.SubjectId.Value) &&
                     (stage == null || c.CurrentStage == stage) &&
                     (status == null || c.Status == status),
                c => c.OpenedAt,
                true,
                c => c.Student, c => c.Advisor, c => c.Subject, c => c.SupportCategory);

            var studentIds = cases.Select(c => c.StudentId).Distinct().ToList();
            var codes = (await _studentProfileRepository.SearchAsync(p => studentIds.Contains(p.StudentId)))
                .ToDictionary(p => p.StudentId, p => p.StudentCode);

            return cases.Select(c => new CaseReportRowResponse
            {
                CaseId = c.CaseId,
                OpenedAt = CaseRules.AsUtc(c.OpenedAt),
                ClosedAt = CaseRules.AsUtc(c.ClosedAt),
                StudentName = c.Student?.FullName ?? string.Empty,
                StudentCode = codes.GetValueOrDefault(c.StudentId) ?? string.Empty,
                AdvisorId = c.AdvisorId,
                AdvisorName = c.Advisor?.FullName ?? string.Empty,
                SupportType = c.SupportCategory?.SupportType ?? string.Empty,
                CategoryName = c.SupportCategory?.CategoryName ?? string.Empty,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status,
                DaysToClose = DaysToClose(c)
            }).ToList();
        }

        public async Task<List<CaseloadClosureRowResponse>> GetCaseloadClosureAsync(CaseloadClosureRequest request)
        {
            var (from, toExclusive) = ValidateRange(request.From, request.To);
            var supportType = CaseWorkflowHelper.OptionalFilter(request.SupportType, CaseRules.SupportTypes, "Support type");

            var cases = await _caseRepository.SearchAsyncInclude(
                c => c.AdvisorId != null &&
                     c.OpenedAt >= from && c.OpenedAt < toExclusive &&
                     (!request.AdvisorId.HasValue || c.AdvisorId == request.AdvisorId.Value) &&
                     (supportType == null || c.SupportCategory.SupportType == supportType),
                c => c.Advisor, c => c.SupportCategory);

            // Grouped by the case's current advisor: a reassigned or referred case counts for whoever holds it now
            return cases
                .GroupBy(c => c.AdvisorId!.Value)
                .Select(group =>
                {
                    var closedDays = group.Select(DaysToClose).Where(d => d.HasValue).Select(d => d!.Value).ToList();
                    var advisor = group.First().Advisor;
                    return new CaseloadClosureRowResponse
                    {
                        AdvisorId = group.Key,
                        AdvisorName = advisor?.FullName ?? string.Empty,
                        AdvisorEmail = advisor?.Email ?? string.Empty,
                        TotalCases = group.Count(),
                        OpenCases = group.Count(c => c.Status != CaseRules.StatusClosed),
                        ClosedCases = group.Count(c => c.Status == CaseRules.StatusClosed),
                        AverageDaysToClose = closedDays.Count == 0 ? null : Math.Round(closedDays.Average(), 2)
                    };
                })
                .OrderByDescending(r => r.TotalCases)
                .ThenBy(r => r.AdvisorName)
                .ToList();
        }

        public async Task<ReportFileResponse> ExportCasesAsync(Guid programmeHeadId, CaseReportExportRequest request)
        {
            var format = ValidateFormat(request.Format);
            var rows = await GetCasesAsync(request);

            var headers = new List<string> { "Opened at (UTC)", "Closed at (UTC)", "Student", "Student code", "Advisor", "Type", "Category", "Subject", "Stage", "Priority", "Status", "Days to close" };
            var cells = rows.Select(r => new List<string>
            {
                FormatDate(r.OpenedAt), FormatDate(r.ClosedAt), r.StudentName, r.StudentCode, r.AdvisorName, r.SupportType,
                r.CategoryName, r.SubjectCode, r.CurrentStage, r.Priority, r.Status, FormatNumber(r.DaysToClose)
            }).ToList();

            var title = "AdviseCare - Cases report";
            var file = BuildFile(format, "Cases", title, RangeText(request.From!.Value, request.To!.Value, rows.Count), headers, cells,
                $"cases_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}");
            await _activityLogService.LogAsync(programmeHeadId, "reportExported",
                $"Exported the cases report ({rows.Count} cases, {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}) as {format}");
            return file;
        }

        public async Task<ReportFileResponse> ExportCaseloadClosureAsync(Guid programmeHeadId, CaseloadClosureExportRequest request)
        {
            var format = ValidateFormat(request.Format);
            var rows = await GetCaseloadClosureAsync(request);

            var headers = new List<string> { "Advisor", "Email", "Total cases", "Open", "Closed", "Average days to close" };
            var cells = rows.Select(r => new List<string>
            {
                r.AdvisorName, r.AdvisorEmail, r.TotalCases.ToString(CultureInfo.InvariantCulture),
                r.OpenCases.ToString(CultureInfo.InvariantCulture), r.ClosedCases.ToString(CultureInfo.InvariantCulture),
                FormatNumber(r.AverageDaysToClose)
            }).ToList();

            var title = "AdviseCare - Caseload and closure time per advisor";
            var file = BuildFile(format, "Caseload", title, RangeText(request.From!.Value, request.To!.Value, rows.Count), headers, cells,
                $"caseload_closure_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}");
            await _activityLogService.LogAsync(programmeHeadId, "reportExported",
                $"Exported the caseload and closure report ({rows.Count} advisors, {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}) as {format}");
            return file;
        }

        // ---------- helpers ----------

        // Both dates are whole UTC days and inclusive: to=2026-12-27 includes cases opened on the 27th
        private static (DateTime From, DateTime ToExclusive) ValidateRange(DateTime? from, DateTime? to)
        {
            if (!from.HasValue || !to.HasValue)
                throw new ValidationException("Both from and to dates are required.");
            var start = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(to.Value.Date, DateTimeKind.Utc);
            if (end < start)
                throw new ValidationException("The to date must not be before the from date.");
            return (start, end.AddDays(1));
        }

        private static string ValidateFormat(string? format)
        {
            var value = string.IsNullOrWhiteSpace(format) ? "xlsx" : format.Trim().ToLowerInvariant();
            if (!Formats.Contains(value))
                throw new ValidationException("Format must be xlsx, csv or pdf.");
            return value;
        }

        private static double? DaysToClose(Case c) =>
            c.Status == CaseRules.StatusClosed && c.ClosedAt.HasValue
                ? Math.Round((c.ClosedAt.Value - c.OpenedAt).TotalDays, 2)
                : null;

        private static string RangeText(DateTime from, DateTime to, int count) =>
            $"Cases opened {from:yyyy-MM-dd} to {to:yyyy-MM-dd} (UTC), {count} row(s), generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

        private static string FormatDate(DateTime? value) =>
            value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatNumber(double? value) =>
            value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;

        private ReportFileResponse BuildFile(string format, string sheetName, string title, string subtitle,
            List<string> headers, List<List<string>> rows, string baseName)
        {
            return format switch
            {
                "csv" => new ReportFileResponse
                {
                    FileName = $"{baseName}.csv",
                    ContentType = "text/csv",
                    Content = ToCsv(headers, rows)
                },
                "pdf" => new ReportFileResponse
                {
                    FileName = $"{baseName}.pdf",
                    ContentType = "application/pdf",
                    Content = _pdfWriter.WriteTable(title, subtitle, headers, rows)
                },
                _ => new ReportFileResponse
                {
                    FileName = $"{baseName}.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Content = _excelWriter.WriteRows(sheetName, headers, rows)
                }
            };
        }

        // UTF-8 with BOM so Excel shows Vietnamese names correctly
        private static byte[] ToCsv(List<string> headers, List<List<string>> rows)
        {
            static string Escape(string value) =>
                value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
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
