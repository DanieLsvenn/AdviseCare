using AdviseCare.Application.Contracts.Requests.Statistic;
using AdviseCare.Application.Contracts.Responses.Statistic;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Globalization;
using System.Text;

namespace AdviseCare.Application.Services
{
    // Anonymised statistics (QC-024), counts and averages only, generated on the fly and never stored:
    // - welfare statistics (Student Welfare Officer, Programme Head): case activity per support category (and faculty); every cell counted from fewer than 5 students is hidden
    // - cohort data (Programme Head): one data attribute per term (and faculty) from the values consented to under the statistics purpose (CONSENTPURPOSES.isForStatistics);
    //   each value is decrypted only after the access guard allows it, every value read is written to the access log, emergency access never counts,
    //   and every cell with fewer than 10 students is hidden
    public class StatisticService : IStatisticService
    {
        private const int WelfareMinimumStudents = 5;
        private const int CohortMinimumStudents = 10;
        private const string DataValueField = "dataValue";

        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Academicterm> _academicTermRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IAccessGuardService _accessGuardService;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IExcelWriter _excelWriter;
        private readonly IPdfWriter _pdfWriter;
        private readonly IActivityLogService _activityLogService;

        public StatisticService(
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Academicterm> academicTermRepository,
            IGenericRepository<Faculty> facultyRepository,
            IAccessGuardService accessGuardService,
            IAttributeKeyService attributeKeyService,
            IExcelWriter excelWriter,
            IPdfWriter pdfWriter,
            IActivityLogService activityLogService)
        {
            _caseRepository = caseRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _studentDataRepository = studentDataRepository;
            _academicTermRepository = academicTermRepository;
            _facultyRepository = facultyRepository;
            _accessGuardService = accessGuardService;
            _attributeKeyService = attributeKeyService;
            _excelWriter = excelWriter;
            _pdfWriter = pdfWriter;
            _activityLogService = activityLogService;
        }

        // Get the Welfare statistics: per support category (and faculty), the cases opened and the referrals, interventions, support grants,
        // external referrals and escalations dated in the range, and the number of different students behind them
        public async Task<List<GetWelfareStatisticResponse>> GetWelfareStatisticAsync(GetWelfareStatisticRequest request)
        {
            return await GetWelfareCellsAsync(request.From, request.To, request.FacultyId, request.SupportType, request.IsByFaculty);
        }

        // Export the Welfare statistics as xlsx, csv or pdf (suppressed cells are exported as "hidden")
        public async Task<ExportWelfareStatisticResponse> ExportWelfareStatisticAsync(Guid AccessorId, ExportWelfareStatisticRequest request)
        {
            var cells = await GetWelfareCellsAsync(request.From, request.To, request.FacultyId, request.SupportType, request.IsByFaculty);

            var headers = new List<string> { "Support type", "Category", "Faculty", "Students", "Cases opened", "Referred cases", "Interventions", "Support grants", "External referrals", "Escalations" };
            var rows = cells.Select(c => new List<string>
            {
                c.SupportType, c.CategoryName, c.FacultyName, FormatCount(c.StudentCount), FormatCount(c.CaseCount), FormatCount(c.ReferredCaseCount),
                FormatCount(c.InterventionCount), FormatCount(c.SupportGrantCount), FormatCount(c.ExternalReferralCount), FormatCount(c.EscalationCount)
            }).ToList();
            var subtitle = $"Records dated {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd} (UTC); cells from fewer than {WelfareMinimumStudents} students are hidden; generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            var file = BuildFile(request.Format, "Welfare", "AdviseCare - Welfare statistics", subtitle, headers, rows,
                $"welfare_statistics_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}");

            await _activityLogService.LogAsync(AccessorId, "statisticsExported",
                $"Exported the welfare statistics ({rows.Count} cells, {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}) as {request.Format}");

            return new ExportWelfareStatisticResponse
            {
                Content = file.Content,
                ContentType = file.ContentType,
                FileName = file.FileName
            };
        }

        // Get the Cohort data of one data attribute in one term (and per faculty): average, minimum and maximum for a numeric attribute,
        // the number of students per value for a categorical or boolean one
        public async Task<List<GetCohortStatisticResponse>> GetCohortStatisticAsync(Guid AccessorId, GetCohortStatisticRequest request)
        {
            return await GetCohortCellsAsync(AccessorId, request.DataAttributeId, request.TermId, request.FacultyId, request.IsByFaculty);
        }

        // Export the Cohort data as xlsx, csv or pdf (suppressed cells are exported as "hidden")
        public async Task<ExportCohortStatisticResponse> ExportCohortStatisticAsync(Guid AccessorId, ExportCohortStatisticRequest request)
        {
            var cells = await GetCohortCellsAsync(AccessorId, request.DataAttributeId, request.TermId, request.FacultyId, request.IsByFaculty);

            var headers = new List<string> { "Attribute", "Term", "Faculty", "Students", "Average", "Minimum", "Maximum", "Students per value" };
            var rows = cells.Select(c => new List<string>
            {
                c.AttributeName, c.TermName, c.FacultyName, FormatCount(c.StudentCount), FormatNumber(c.AverageValue), FormatNumber(c.MinValue), FormatNumber(c.MaxValue),
                c.IsSuppressed ? "hidden" : string.Join("; ", c.ValueCounts.Select(v => $"{v.Value}: {v.StudentCount}"))
            }).ToList();
            var first = cells.FirstOrDefault();
            var subtitle = $"Values shared for statistics only; cells with fewer than {CohortMinimumStudents} students are hidden; generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            var file = BuildFile(request.Format, "Cohort", $"AdviseCare - Cohort data: {first?.AttributeName} ({first?.TermName})", subtitle, headers, rows,
                $"cohort_{(first?.AttributeName ?? "attribute").Replace(' ', '_')}_{(first?.TermName ?? "term").Replace(' ', '_')}");

            await _activityLogService.LogAsync(AccessorId, "statisticsExported",
                $"Exported the cohort data of {first?.AttributeName} ({first?.TermName}, {rows.Count} cells) as {request.Format}");

            return new ExportCohortStatisticResponse
            {
                Content = file.Content,
                ContentType = file.ContentType,
                FileName = file.FileName
            };
        }

        // Helper: the welfare cells; a case counts when it was opened in the range or has a referral, intervention, support grant,
        // external referral or escalation dated in the range (each record counted on its own date)
        private async Task<List<GetWelfareStatisticResponse>> GetWelfareCellsAsync(DateTime? From, DateTime? To, Guid? FacultyId, string SupportType, bool IsByFaculty)
        {
            var (start, endExclusive) = GetRange(From, To);
            if (FacultyId != null && !await _facultyRepository.ExistsAsync(f => f.FacultyId == FacultyId))
                throw new NotFoundException("Faculty not found.");

            var cases = await _caseRepository.SearchAsyncInclude(c =>
                    (FacultyId == null || c.Student.FacultyId == FacultyId) &&
                    (string.IsNullOrWhiteSpace(SupportType) || c.SupportCategory.SupportType == SupportType) &&
                    ((c.OpenedAt >= start && c.OpenedAt < endExclusive) ||
                     c.Referrals.Any(r => r.CreatedAt >= start && r.CreatedAt < endExclusive) ||
                     c.Interventions.Any(i => i.InterventionDate >= start && i.InterventionDate < endExclusive) ||
                     c.Interventions.Any(i => i.Supportgrants.Any(g => g.IssuedAt >= start && g.IssuedAt < endExclusive)) ||
                     c.Externalreferrals.Any(e => e.ReferredAt >= start && e.ReferredAt < endExclusive) ||
                     c.Escalations.Any(e => e.EscalatedAt >= start && e.EscalatedAt < endExclusive)),
                c => c.SupportCategory, c => c.Student.Faculty, c => c.Referrals, c => c.Interventions.Select(i => i.Supportgrants), c => c.Externalreferrals, c => c.Escalations);

            return cases
                .GroupBy(c => (c.SupportCategoryId, FacultyId: IsByFaculty ? c.Student.FacultyId : null))
                .Select(g =>
                {
                    var category = g.First().SupportCategory;
                    var faculty = IsByFaculty ? g.First().Student.Faculty : null;
                    var studentCount = g.Select(c => c.StudentId).Distinct().Count();
                    var isSuppressed = studentCount < WelfareMinimumStudents;
                    return new GetWelfareStatisticResponse
                    {
                        FacultyId = g.Key.FacultyId,
                        SupportCategoryId = g.Key.SupportCategoryId,
                        CategoryName = category.CategoryName,
                        FacultyName = IsByFaculty ? faculty?.FacultyName ?? "No faculty" : string.Empty,
                        CaseCount = isSuppressed ? null : g.Count(c => c.OpenedAt >= start && c.OpenedAt < endExclusive),
                        EscalationCount = isSuppressed ? null : g.Sum(c => c.Escalations.Count(e => e.EscalatedAt >= start && e.EscalatedAt < endExclusive)),
                        ExternalReferralCount = isSuppressed ? null : g.Sum(c => c.Externalreferrals.Count(e => e.ReferredAt >= start && e.ReferredAt < endExclusive)),
                        InterventionCount = isSuppressed ? null : g.Sum(c => c.Interventions.Count(i => i.InterventionDate >= start && i.InterventionDate < endExclusive)),
                        ReferredCaseCount = isSuppressed ? null : g.Count(c => c.Referrals.Any(r => r.CreatedAt >= start && r.CreatedAt < endExclusive)),
                        StudentCount = isSuppressed ? null : studentCount,
                        SupportGrantCount = isSuppressed ? null : g.SelectMany(c => c.Interventions).Sum(i => i.Supportgrants.Count(s => s.IssuedAt >= start && s.IssuedAt < endExclusive)),
                        IsSuppressed = isSuppressed,
                        SupportType = category.SupportType
                    };
                })
                .OrderBy(c => c.SupportType)
                .ThenBy(c => c.CategoryName)
                .ThenBy(c => c.FacultyName)
                .ToList();
        }

        // Helper: the cohort cells; only the latest value per student of the attribute and term counts, a missing or erased latest value is skipped,
        // and a value counts only when the access guard allows it under the statistics purpose (never through emergency access)
        private async Task<List<GetCohortStatisticResponse>> GetCohortCellsAsync(Guid AccessorId, Guid DataAttributeId, Guid TermId, Guid? FacultyId, bool IsByFaculty)
        {
            var purpose = (await _consentPurposeRepository.SearchAsyncInclude(p => p.IsForStatistics && p.Status == "active", p => p.DataAttributes)).FirstOrDefault()
                ?? throw new ConflictException("There is no active statistics consent purpose, so no cohort data can be shown.");
            var attribute = purpose.DataAttributes.FirstOrDefault(a => a.DataAttributeId == DataAttributeId)
                ?? throw new ValidationException($"This data attribute is not covered by '{purpose.PurposeName}', so it has no cohort data.");
            var term = await _academicTermRepository.GetByIdAsync(TermId)
                ?? throw new NotFoundException("Academic term not found.");
            if (FacultyId != null && !await _facultyRepository.ExistsAsync(f => f.FacultyId == FacultyId))
                throw new NotFoundException("Faculty not found.");

            var latestRows = (await _studentDataRepository.SearchAsyncInclude(d =>
                    d.DataAttributeId == DataAttributeId && d.TermId == TermId &&
                    (FacultyId == null || d.Student.FacultyId == FacultyId),
                    d => d.Student.Faculty))
                .GroupBy(d => d.StudentId)
                .Select(g => g.OrderByDescending(d => d.TransactionFrom).First())
                .Where(d => !d.IsMissing && d.ErasedAt == null && d.DataValue != null && d.AttributeKeyId != null)
                .ToList();

            var decisions = latestRows.Count == 0
                ? new List<AccessDecision>()
                : await _accessGuardService.CheckAccessForStudentsAsync(AccessorId, latestRows.Select(d => d.StudentId).ToList(),
                    new List<Guid> { DataAttributeId }, purpose.ConsentPurposeId);
            var allowedStudentIds = decisions
                .Where(d => d.IsAllowed && !d.IsEmergencyAccess && d.ConsentPurposeId == purpose.ConsentPurposeId)
                .Select(d => d.StudentId)
                .ToHashSet();
            var groups = latestRows
                .Where(d => allowedStudentIds.Contains(d.StudentId))
                .GroupBy(d => IsByFaculty ? d.Student.FacultyId : null)
                .ToList();

            // Decrypt only the cells that will be shown, so a hidden cell reads (and logs) nothing
            var shownRows = groups.Where(g => g.Count() >= CohortMinimumStudents).SelectMany(g => g).ToList();
            var decrypted = (await _attributeKeyService.DecryptValuesAsync(shownRows.Select(d => new EncryptedAttributeValue
                {
                    AttributeKeyId = d.AttributeKeyId!.Value,
                    StudentId = d.StudentId,
                    DataAttributeId = d.DataAttributeId,
                    RowId = d.StudentDataId,
                    FieldName = DataValueField,
                    Payload = d.DataValue
                }).ToList()))
                .Where(v => !v.IsErased && v.Value != null)
                .ToDictionary(v => v.RowId, v => v.Value!);
            await _accessGuardService.LogAccessBatchAsync(AccessorId, shownRows.Where(d => decrypted.ContainsKey(d.StudentDataId)).Select(d => new AccessLogEntry
            {
                StudentId = d.StudentId,
                StudentDataId = d.StudentDataId,
                ConsentPurposeId = purpose.ConsentPurposeId,
                AccessedResource = $"Cohort data: {attribute.AttributeName} ({term.TermName})"
            }).ToList());

            var cells = groups.Select(g =>
            {
                var values = g.Where(d => decrypted.ContainsKey(d.StudentDataId)).Select(d => decrypted[d.StudentDataId]).ToList();
                return BuildCohortCell(attribute, term, g.Key, g.First().Student.Faculty, IsByFaculty, g.Count() < CohortMinimumStudents ? null : values);
            }).ToList();
            if (cells.Count == 0)
                cells.Add(BuildCohortCell(attribute, term, null, null, false, null));

            return cells.OrderBy(c => c.FacultyName).ToList();
        }

        // Helper: one cohort cell; null values = suppressed
        private static GetCohortStatisticResponse BuildCohortCell(Dataattribute Attribute, Academicterm Term, Guid? FacultyId, Faculty? Faculty, bool IsByFaculty, List<string>? Values)
        {
            var cell = new GetCohortStatisticResponse
            {
                DataAttributeId = Attribute.DataAttributeId,
                FacultyId = FacultyId,
                TermId = Term.TermId,
                AttributeName = Attribute.AttributeName,
                FacultyName = IsByFaculty ? Faculty?.FacultyName ?? "No faculty" : string.Empty,
                TermName = Term.TermName,
                AverageValue = null,
                MaxValue = null,
                MinValue = null,
                StudentCount = null,
                ValueCounts = new List<CohortValueCountResponse>(),
                IsSuppressed = Values == null || Values.Count < CohortMinimumStudents,
                DataType = Attribute.DataType
            };
            if (cell.IsSuppressed)
                return cell;

            cell.StudentCount = Values!.Count;
            if (Attribute.DataType == "numeric")
            {
                var numbers = Values.Select(v => double.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture)).ToList();
                cell.AverageValue = Math.Round(numbers.Average(), 2);
                cell.MaxValue = numbers.Max();
                cell.MinValue = numbers.Min();
            }
            else
            {
                cell.ValueCounts = Values
                    .GroupBy(v => Attribute.DataType == "boolean" ? v.Trim().ToLowerInvariant() : v.Trim())
                    .Where(g => g.Count() >= CohortMinimumStudents)
                    .Select(g => new CohortValueCountResponse { Value = g.Key, StudentCount = g.Count() })
                    .OrderByDescending(v => v.StudentCount)
                    .ThenBy(v => v.Value)
                    .ToList();
            }
            return cell;
        }

        // Helper: both dates are required, whole UTC days and inclusive
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

        // Helper: a count as text for an export ("hidden" when suppressed)
        private static string FormatCount(int? Value)
        {
            return Value != null ? Value.Value.ToString(CultureInfo.InvariantCulture) : "hidden";
        }

        // Helper: a number as text for an export
        private static string FormatNumber(double? Value)
        {
            return Value != null ? Value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        // Helper: build the file in the asked format
        private StatisticFile BuildFile(string Format, string SheetName, string Title, string Subtitle, List<string> Headers, List<List<string>> Rows, string BaseName)
        {
            return Format switch
            {
                "csv" => new StatisticFile(ToCsv(Headers, Rows), "text/csv", $"{BaseName}.csv"),
                "pdf" => new StatisticFile(_pdfWriter.WriteTable(Title, Subtitle, Headers, Rows), "application/pdf", $"{BaseName}.pdf"),
                _ => new StatisticFile(_excelWriter.WriteRows(SheetName, Headers, Rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{BaseName}.xlsx")
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

        // Helper type: a generated statistics file
        private record StatisticFile(byte[] Content, string ContentType, string FileName);
    }
}
