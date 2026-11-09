using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Contracts.Responses.Report;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace AdviseCare.Application.Services
{
    // Reports (WF4, Programme Head) over the cases opened in a date range: the case list, and the caseload and closure time per advisor.
    // Generated on the fly and downloaded as xlsx, csv or pdf; nothing is stored (REPORTS stays for the stored report history, extra scope ES2)
    public class ReportService : IReportService
    {
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IExcelWriter _excelWriter;
        private readonly IPdfWriter _pdfWriter;
        private readonly IActivityLogService _activityLogService;

        public ReportService(
            IGenericRepository<Case> caseRepository,
            IExcelWriter excelWriter,
            IPdfWriter pdfWriter,
            IActivityLogService activityLogService)
        {
            _caseRepository = caseRepository;
            _excelWriter = excelWriter;
            _pdfWriter = pdfWriter;
            _activityLogService = activityLogService;
        }

        // Get the Case report: the cases opened in the range, oldest first, filtered by advisor, support type, category, subject, stage and status
        public async Task<List<GetCaseReportResponse>> GetCaseReportAsync(GetCaseReportRequest request)
        {
            var cases = await GetReportCasesAsync(request.From, request.To, request.AdvisorId, request.SupportType, request.SupportCategoryId, request.SubjectId, request.CurrentStage, request.Status);

            return cases.Select(c => new GetCaseReportResponse
            {
                CaseId = c.CaseId,
                AdvisorId = c.AdvisorId,
                AdvisorFullName = c.Advisor?.FullName ?? string.Empty,
                CategoryName = c.SupportCategory.CategoryName,
                StudentCode = c.Student.Studentprofile?.StudentCode ?? string.Empty,
                StudentFullName = c.Student.FullName,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                DaysToClose = DaysToClose(c),
                ClosedAt = c.ClosedAt,
                OpenedAt = c.OpenedAt,
                SupportType = c.SupportCategory.SupportType,
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();
        }

        // Export the Case report as xlsx, csv or pdf
        public async Task<ExportCaseReportResponse> ExportCaseReportAsync(Guid ProgrammeHeadId, ExportCaseReportRequest request)
        {
            var cases = await GetReportCasesAsync(request.From, request.To, request.AdvisorId, request.SupportType, request.SupportCategoryId, request.SubjectId, request.CurrentStage, request.Status);

            var headers = new List<string> { "Opened at (UTC)", "Closed at (UTC)", "Student", "Student code", "Advisor", "Type", "Category", "Subject", "Stage", "Priority", "Status", "Days to close" };
            var rows = cases.Select(c => new List<string>
            {
                FormatDate(c.OpenedAt), FormatDate(c.ClosedAt), c.Student.FullName, c.Student.Studentprofile?.StudentCode ?? string.Empty,
                c.Advisor?.FullName ?? string.Empty, c.SupportCategory.SupportType, c.SupportCategory.CategoryName, c.Subject?.SubjectCode ?? string.Empty,
                c.CurrentStage, c.Priority, c.Status, FormatNumber(DaysToClose(c))
            }).ToList();
            var file = BuildFile(request.Format, "Cases", "AdviseCare - Cases report", RangeText(request.From!.Value, request.To!.Value, rows.Count),
                headers, rows, $"cases_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}");

            await _activityLogService.LogAsync(ProgrammeHeadId, "reportExported",
                $"Exported the cases report ({rows.Count} cases, {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}) as {request.Format}");

            return new ExportCaseReportResponse
            {
                Content = file.Content,
                ContentType = file.ContentType,
                FileName = file.FileName
            };
        }

        // Get the Caseload and closure report: per current advisor, the total, open and closed cases opened in the range,
        // and the average days to close (closed cases only), busiest advisor first
        public async Task<List<GetCaseloadClosureReportResponse>> GetCaseloadClosureReportAsync(GetCaseloadClosureReportRequest request)
        {
            var advisors = await GetCaseloadAsync(request.From, request.To, request.AdvisorId, request.SupportType);

            return advisors.Select(a => new GetCaseloadClosureReportResponse
            {
                AdvisorId = a.AdvisorId,
                AdvisorEmail = a.AdvisorEmail,
                AdvisorFullName = a.AdvisorFullName,
                AverageDaysToClose = a.AverageDaysToClose,
                ClosedCases = a.ClosedCases,
                OpenCases = a.OpenCases,
                TotalCases = a.TotalCases
            }).ToList();
        }

        // Export the Caseload and closure report as xlsx, csv or pdf
        public async Task<ExportCaseloadClosureReportResponse> ExportCaseloadClosureReportAsync(Guid ProgrammeHeadId, ExportCaseloadClosureReportRequest request)
        {
            var advisors = await GetCaseloadAsync(request.From, request.To, request.AdvisorId, request.SupportType);

            var headers = new List<string> { "Advisor", "Email", "Total cases", "Open", "Closed", "Average days to close" };
            var rows = advisors.Select(a => new List<string>
            {
                a.AdvisorFullName, a.AdvisorEmail, a.TotalCases.ToString(CultureInfo.InvariantCulture), a.OpenCases.ToString(CultureInfo.InvariantCulture),
                a.ClosedCases.ToString(CultureInfo.InvariantCulture), FormatNumber(a.AverageDaysToClose)
            }).ToList();
            var file = BuildFile(request.Format, "Caseload", "AdviseCare - Caseload and closure time per advisor", RangeText(request.From!.Value, request.To!.Value, rows.Count),
                headers, rows, $"caseload_closure_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}");

            await _activityLogService.LogAsync(ProgrammeHeadId, "reportExported",
                $"Exported the caseload and closure report ({rows.Count} advisors, {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}) as {request.Format}");

            return new ExportCaseloadClosureReportResponse
            {
                Content = file.Content,
                ContentType = file.ContentType,
                FileName = file.FileName
            };
        }

        // Helper: the cases opened in the range (both dates inclusive, whole UTC days) that match the filters, oldest first
        private async Task<List<Case>> GetReportCasesAsync(DateTime? From, DateTime? To, Guid? AdvisorId, string SupportType, Guid? SupportCategoryId, Guid? SubjectId, string CurrentStage, string Status)
        {
            var (start, endExclusive) = GetRange(From, To);
            Expression<Func<Case, bool>> predicate = c =>
                c.OpenedAt >= start && c.OpenedAt < endExclusive &&
                (AdvisorId == null || c.AdvisorId == AdvisorId) &&
                (string.IsNullOrWhiteSpace(SupportType) || c.SupportCategory.SupportType == SupportType) &&
                (SupportCategoryId == null || c.SupportCategoryId == SupportCategoryId) &&
                (SubjectId == null || c.SubjectId == SubjectId) &&
                (string.IsNullOrWhiteSpace(CurrentStage) || c.CurrentStage == CurrentStage) &&
                (string.IsNullOrWhiteSpace(Status) || c.Status == Status);

            return await _caseRepository.SearchAsyncIncludeOrderBy(predicate, c => c.OpenedAt, true,
                c => c.Student.Studentprofile, c => c.Advisor, c => c.Subject, c => c.SupportCategory);
        }

        // Helper: the caseload rows, grouped by the case's current advisor (a reassigned or referred case counts for whoever holds it now)
        private async Task<List<CaseloadRow>> GetCaseloadAsync(DateTime? From, DateTime? To, Guid? AdvisorId, string SupportType)
        {
            var (start, endExclusive) = GetRange(From, To);
            var cases = await _caseRepository.SearchAsyncInclude(c =>
                c.AdvisorId != null &&
                c.OpenedAt >= start && c.OpenedAt < endExclusive &&
                (AdvisorId == null || c.AdvisorId == AdvisorId) &&
                (string.IsNullOrWhiteSpace(SupportType) || c.SupportCategory.SupportType == SupportType),
                c => c.Advisor, c => c.SupportCategory);

            return cases
                .GroupBy(c => c.AdvisorId!.Value)
                .Select(g =>
                {
                    var closedDays = g.Select(DaysToClose).Where(d => d != null).Select(d => d!.Value).ToList();
                    var advisor = g.First().Advisor;
                    return new CaseloadRow(g.Key, advisor?.Email ?? string.Empty, advisor?.FullName ?? string.Empty,
                        closedDays.Count == 0 ? null : Math.Round(closedDays.Average(), 2),
                        g.Count(c => c.Status == "closed"), g.Count(c => c.Status != "closed"), g.Count());
                })
                .OrderByDescending(r => r.TotalCases)
                .ThenBy(r => r.AdvisorFullName)
                .ToList();
        }

        // Helper: both dates are required, whole UTC days and inclusive (To = 2026-12-27 includes cases opened on the 27th)
        private static (DateTime Start, DateTime EndExclusive) GetRange(DateTime? From, DateTime? To)
        {
            if (From == null || To == null)
                throw new ValidationException("Both from and to dates are required.");
            var start = DateTime.SpecifyKind(From.Value.Date, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(To.Value.Date, DateTimeKind.Utc);
            if (end < start)
                throw new ValidationException("The to date must not be before the from date.");
            return (start, end.AddDays(1));
        }

        // Helper: days from opening to closing, closed cases only
        private static double? DaysToClose(Case CaseRecord)
        {
            return CaseRecord.Status == "closed" && CaseRecord.ClosedAt != null
                ? Math.Round((CaseRecord.ClosedAt.Value - CaseRecord.OpenedAt).TotalDays, 2)
                : null;
        }

        // Helper: the subtitle of an exported report
        private static string RangeText(DateTime From, DateTime To, int Count)
        {
            return $"Cases opened {From:yyyy-MM-dd} to {To:yyyy-MM-dd} (UTC), {Count} row(s), generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        }

        // Helper: a date as text for an exported report
        private static string FormatDate(DateTime? Value)
        {
            return Value != null ? Value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : string.Empty;
        }

        // Helper: a number as text for an exported report
        private static string FormatNumber(double? Value)
        {
            return Value != null ? Value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        // Helper: build the file in the asked format
        private ReportFile BuildFile(string Format, string SheetName, string Title, string Subtitle, List<string> Headers, List<List<string>> Rows, string BaseName)
        {
            return Format switch
            {
                "csv" => new ReportFile(ToCsv(Headers, Rows), "text/csv", $"{BaseName}.csv"),
                "pdf" => new ReportFile(_pdfWriter.WriteTable(Title, Subtitle, Headers, Rows), "application/pdf", $"{BaseName}.pdf"),
                _ => new ReportFile(_excelWriter.WriteRows(SheetName, Headers, Rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{BaseName}.xlsx")
            };
        }

        // Helper: CSV in UTF-8 with BOM, so Excel shows Vietnamese names correctly
        private static byte[] ToCsv(List<string> Headers, List<List<string>> Rows)
        {
            static string Escape(string Value) =>
                Value.Contains(',') || Value.Contains('"') || Value.Contains('\n') || Value.Contains('\r')
                    ? $"\"{Value.Replace("\"", "\"\"")}\""
                    : Value;

            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", Headers.Select(Escape)));
            foreach (var row in Rows)
                builder.AppendLine(string.Join(",", row.Select(Escape)));
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }

        // Helper type: one advisor's row of the caseload report
        private record CaseloadRow(Guid AdvisorId, string AdvisorEmail, string AdvisorFullName, double? AverageDaysToClose, int ClosedCases, int OpenCases, int TotalCases);

        // Helper type: a generated report file
        private record ReportFile(byte[] Content, string ContentType, string FileName);
    }
}
