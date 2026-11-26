using AdviseCare.Application.Contracts.Requests.AccessLog;
using AdviseCare.Application.Contracts.Requests.StudentData;
using AdviseCare.Application.Contracts.Responses.StudentData;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdviseCare.Application.Services
{
    // Student data (STUDENTDATA, the bitemporal ledger of data attribute values): import (Excel/CSV, or transcript PDFs for the per-subject Subject mark and
    // Attendance rate), consent-guarded reads, point-in-time views and the student's own export
    public class StudentDataService : IStudentDataService
    {
        private const string DataValueField = "dataValue";
        // Transcript PDFs: a grade transcript fills "Subject mark", an attendance file "Attendance rate"
        private const string TranscriptGradeAttribute = "Subject mark";
        private const string TranscriptAttendanceAttribute = "Attendance rate";
        private const int MaxTranscriptFiles = 200;
        private const long MaxTranscriptBytes = 5 * 1024 * 1024;
        private static readonly string EmptyHash = Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()));
        private static readonly Regex TranscriptSemesterPattern = new(@"^(spring|summer|fall)(\d{4})$", RegexOptions.IgnoreCase);
        private static readonly JsonSerializerOptions MappingJsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IGenericRepository<Academicterm> _academicTermRepository;
        private readonly IGenericRepository<Subject> _subjectRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IExcelReader _excelReader;
        private readonly ICsvReader _csvReader;
        private readonly ITranscriptPdfReader _transcriptPdfReader;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IAccessGuardService _accessGuardService;
        private readonly IAccessLogService _accessLogService;
        private readonly IStudentProfileService _studentProfileService;
        private readonly IActivityLogService _activityLogService;
        private readonly IIndicatorFlagService _indicatorFlagService;

        public StudentDataService(
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IGenericRepository<Academicterm> academicTermRepository,
            IGenericRepository<Subject> subjectRepository,
            IGenericRepository<Case> caseRepository,
            IExcelReader excelReader,
            ICsvReader csvReader,
            ITranscriptPdfReader transcriptPdfReader,
            IAttributeKeyService attributeKeyService,
            IAccessGuardService accessGuardService,
            IAccessLogService accessLogService,
            IStudentProfileService studentProfileService,
            IActivityLogService activityLogService,
            IIndicatorFlagService indicatorFlagService)
        {
            _studentDataRepository = studentDataRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _studentProfileRepository = studentProfileRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _academicTermRepository = academicTermRepository;
            _subjectRepository = subjectRepository;
            _caseRepository = caseRepository;
            _excelReader = excelReader;
            _csvReader = csvReader;
            _transcriptPdfReader = transcriptPdfReader;
            _attributeKeyService = attributeKeyService;
            _accessGuardService = accessGuardService;
            _accessLogService = accessLogService;
            _studentProfileService = studentProfileService;
            _activityLogService = activityLogService;
            _indicatorFlagService = indicatorFlagService;
        }

        // Get all Student data rows (what was imported and when; never the values)
        public async Task<IEnumerable<GetAllStudentDataResponse>> GetAll()
        {
            var rows = await GetRowsAsync(d => true);

            return rows.Select(d => new GetAllStudentDataResponse
            {
                StudentDataId = d.StudentDataId,
                DataAttributeId = d.DataAttributeId,
                DataSourceId = d.DataSourceId,
                StudentId = d.StudentId,
                SubjectId = d.SubjectId,
                TermId = d.TermId,
                AttributeName = d.DataAttribute.AttributeName,
                ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                StudentFullName = d.Student.FullName,
                SubjectCode = d.Subject?.SubjectCode ?? string.Empty,
                SubjectName = d.Subject?.SubjectName ?? string.Empty,
                TermName = d.Term.TermName,
                ErasedAt = d.ErasedAt,
                TransactionFrom = d.TransactionFrom,
                ValidFrom = d.ValidFrom,
                IsMissing = d.IsMissing,
                Status = d.Status
            });
        }

        // Get all Student data rows with pagination (never the values)
        public async Task<IEnumerable<GetAllStudentDataWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var rows = await GetRowsAsync(d => true);

            return rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new GetAllStudentDataWithPaginationResponse
                {
                    StudentDataId = d.StudentDataId,
                    DataAttributeId = d.DataAttributeId,
                    DataSourceId = d.DataSourceId,
                    StudentId = d.StudentId,
                    SubjectId = d.SubjectId,
                    TermId = d.TermId,
                    AttributeName = d.DataAttribute.AttributeName,
                    ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                    StudentFullName = d.Student.FullName,
                    SubjectCode = d.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = d.Subject?.SubjectName ?? string.Empty,
                    TermName = d.Term.TermName,
                    ErasedAt = d.ErasedAt,
                    TransactionFrom = d.TransactionFrom,
                    ValidFrom = d.ValidFrom,
                    IsMissing = d.IsMissing,
                    Status = d.Status
                });
        }

        // Get Student data row by Id (never the value)
        public async Task<GetStudentDataByIdResponse> GetByIdAsync(Guid StudentDataId)
        {
            var row = (await GetRowsAsync(d => d.StudentDataId == StudentDataId)).FirstOrDefault()
                ?? throw new NotFoundException("Student data row not found.");

            return new GetStudentDataByIdResponse
            {
                StudentDataId = row.StudentDataId,
                DataAttributeId = row.DataAttributeId,
                DataSourceId = row.DataSourceId,
                StudentId = row.StudentId,
                SubjectId = row.SubjectId,
                TermId = row.TermId,
                AttributeName = row.DataAttribute.AttributeName,
                ProvenanceNote = row.ProvenanceNote ?? string.Empty,
                StudentFullName = row.Student.FullName,
                SubjectCode = row.Subject?.SubjectCode ?? string.Empty,
                SubjectName = row.Subject?.SubjectName ?? string.Empty,
                TermName = row.Term.TermName,
                ErasedAt = row.ErasedAt,
                TransactionFrom = row.TransactionFrom,
                ValidFrom = row.ValidFrom,
                IsMissing = row.IsMissing,
                Status = row.Status
            };
        }

        // Search Student data rows by attribute, student, subject, term, missing flag or status (e.g. every missing value of a term; never the values)
        public async Task<List<SearchStudentDataResponse>> SearchAsync(SearchStudentDataRequest request)
        {
            var rows = await GetRowsAsync(d =>
                (request.DataAttributeId == null || d.DataAttributeId == request.DataAttributeId) &&
                (request.StudentId == null || d.StudentId == request.StudentId) &&
                (request.SubjectId == null || d.SubjectId == request.SubjectId) &&
                (request.TermId == null || d.TermId == request.TermId) &&
                (request.IsMissing == null || d.IsMissing == request.IsMissing) &&
                (string.IsNullOrWhiteSpace(request.Status) || d.Status == request.Status));

            return rows.Select(d => new SearchStudentDataResponse
            {
                StudentDataId = d.StudentDataId,
                DataAttributeId = d.DataAttributeId,
                DataSourceId = d.DataSourceId,
                StudentId = d.StudentId,
                SubjectId = d.SubjectId,
                TermId = d.TermId,
                AttributeName = d.DataAttribute.AttributeName,
                ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                StudentFullName = d.Student.FullName,
                SubjectCode = d.Subject?.SubjectCode ?? string.Empty,
                SubjectName = d.Subject?.SubjectName ?? string.Empty,
                TermName = d.Term.TermName,
                ErasedAt = d.ErasedAt,
                TransactionFrom = d.TransactionFrom,
                ValidFrom = d.ValidFrom,
                IsMissing = d.IsMissing,
                Status = d.Status
            }).ToList();
        }

        // Create Student data by importing an .xlsx/.csv file: every valid row is encrypted and inserted as a new row (never an update), every enrolled student
        // without a value for an imported per-term attribute/term gets a missing row (per-subject attributes get none), all in one save. Any invalid row fails the whole import; rows of archived
        // data attributes are not imported but listed in RejectedRows, and the other rows still go in
        public async Task<CreateStudentDataResponse> CreateAsync(Guid CreatorId, CreateStudentDataRequest request)
        {
            var fileName = Path.GetFileName(request.File?.FileName ?? string.Empty);
            var import = await ReadImportAsync(request);
            if (import.Errors.Count > 0)
            {
                await _activityLogService.LogAsync(CreatorId, "studentDataImportFailed", $"Refused {fileName}: {import.Errors.Count} invalid row(s)");
                return new CreateStudentDataResponse { Errors = import.Errors, RejectedRows = import.RejectedRows, IsSuccess = false };
            }

            var transactionAt = DateTime.UtcNow;
            var newRows = import.Rows.Select(r => new Studentdata
            {
                StudentDataId = Guid.NewGuid(),
                DataSourceId = null,
                DataAttributeId = r.DataAttribute.DataAttributeId,
                StudentId = r.Student.AccountId,
                SubjectId = r.Subject?.SubjectId,
                TermId = r.Term.TermId,
                ProvenanceNote = $"Imported from {fileName} on {transactionAt:yyyy-MM-dd}.",
                TransactionFrom = transactionAt,
                ValidFrom = r.ValidFrom,
                IsMissing = false,
                Status = "original"
            }).ToList();

            // Encrypt every value with the student's key for its attribute (new keys are staged and saved below with the rows)
            var encrypted = await _attributeKeyService.EncryptValuesAsync(import.Rows.Select((r, index) => new PlainAttributeValue
            {
                StudentId = r.Student.AccountId,
                DataAttributeId = r.DataAttribute.DataAttributeId,
                RowId = newRows[index].StudentDataId,
                FieldName = DataValueField,
                Value = r.Value
            }).ToList());
            for (var index = 0; index < newRows.Count; index++)
            {
                newRows[index].AttributeKeyId = encrypted[index].AttributeKeyId;
                newRows[index].DataValue = encrypted[index].Payload;
                _studentDataRepository.PrepareCreate(newRows[index]);
            }

            // Missing rows: every enrolled student without a value for an imported attribute/term pair; only for per-term attributes, since
            // AdviseCare does not know which subjects a student takes and cannot tell a missing per-subject value from a subject not taken
            var enrolledStudentIds = (await _studentProfileRepository.SearchAsyncInclude(p =>
                    p.AcademicStatus == "enrolled" && p.Student.Status == "active", p => p.Student))
                .Select(p => p.StudentId)
                .ToList();
            var importedTriples = import.Rows.Select(r => (r.Student.AccountId, r.DataAttribute.DataAttributeId, r.Term.TermId)).ToHashSet();
            var importedPairs = import.Rows.Where(r => !r.DataAttribute.IsSubjectSpecific).Select(r => (r.DataAttribute, r.Term)).DistinctBy(p => (p.DataAttribute.DataAttributeId, p.Term.TermId)).ToList();
            var missingCount = 0;
            foreach (var studentId in enrolledStudentIds)
            {
                foreach (var (dataAttribute, term) in importedPairs)
                {
                    if (importedTriples.Contains((studentId, dataAttribute.DataAttributeId, term.TermId)))
                        continue;

                    _studentDataRepository.PrepareCreate(new Studentdata
                    {
                        StudentDataId = Guid.NewGuid(),
                        DataAttributeId = dataAttribute.DataAttributeId,
                        StudentId = studentId,
                        TermId = term.TermId,
                        ProvenanceNote = $"Expected in {fileName}; no value was supplied. Recorded {transactionAt:yyyy-MM-dd}.",
                        TransactionFrom = transactionAt,
                        ValidFrom = term.StartDate,
                        IsMissing = true,
                        Status = "original"
                    });
                    missingCount++;
                }
            }
            await _studentDataRepository.SaveAsync();

            await _activityLogService.LogAsync(CreatorId, "studentDataImported",
                $"Imported {newRows.Count} value(s) from {fileName}; recorded {missingCount} missing value(s); left out {import.RejectedRows.Count} row(s) of archived data attributes");
            // Compare the new values with the active indicators (A7.2): raise and clear flags
            var flagCheck = await _indicatorFlagService.CheckAsync(CreatorId);

            return new CreateStudentDataResponse
            {
                InsertedCount = newRows.Count,
                MissingCount = missingCount,
                ClearedFlagCount = flagCheck.ClearedCount,
                RaisedFlagCount = flagCheck.RaisedCount,
                RejectedRows = import.RejectedRows,
                Errors = new List<StudentDataRowErrorResponse>(),
                IsSuccess = true
            };
        }

        private const int MaxPreviewRows = 2000;

        // Preview an import file: the header row, five sample rows and every row (for the editable preview); nothing is written
        public Task<PreviewStudentDataResponse> PreviewAsync(PreviewStudentDataRequest request)
        {
            var rows = ReadRows(request.File);
            if (rows.Count == 0)
                throw new ValidationException("The file has no data rows under the header row.");

            return Task.FromResult(new PreviewStudentDataResponse
            {
                Headers = rows[0].Values.Keys.ToList(),
                SampleRows = rows.Take(5).Select(r => new StudentDataPreviewRowResponse
                {
                    RowNumber = r.RowNumber,
                    Values = r.Values
                }).ToList(),
                // Every row (up to 2000) for the editable preview; the edited rows come back as a .csv file
                Rows = rows.Take(MaxPreviewRows).Select(r => new StudentDataPreviewRowResponse
                {
                    RowNumber = r.RowNumber,
                    Values = r.Values
                }).ToList(),
                TotalRowCount = rows.Count
            });
        }

        // Validate an import file and its column mapping without writing anything
        public async Task<ValidateStudentDataResponse> ValidateAsync(CreateStudentDataRequest request)
        {
            var import = await ReadImportAsync(request);

            return new ValidateStudentDataResponse
            {
                ValidRowCount = import.Errors.Count == 0 ? import.Rows.Count : 0,
                RejectedRows = import.RejectedRows,
                Errors = import.Errors,
                IsValid = import.Errors.Count == 0
            };
        }

        // Preview transcript PDFs (grade transcripts -> "Subject mark", attendance files -> "Attendance rate", per subject and term): every file is read
        // and checked like an import row (student, term, subject, value range); nothing is written and nothing is kept
        public async Task<PreviewTranscriptsResponse> PreviewTranscriptsAsync(PreviewTranscriptsRequest request)
        {
            var check = await ReadTranscriptsAsync(request.Files);

            return new PreviewTranscriptsResponse
            {
                InsertCount = check.Files.Where(f => f.IsValid).Sum(f => f.InsertCount),
                ReplaceCount = check.Files.Where(f => f.IsValid).Sum(f => f.ReplaceCount),
                Files = check.Files,
                IsValid = check.Files.All(f => f.IsValid)
            };
        }

        // Import transcript PDFs: the files are read and checked again; only when every file is valid, every row is encrypted and inserted as a new
        // STUDENTDATA row in one save (insert-only, no missing rows for per-subject attributes); any invalid file fails the whole import
        public async Task<ImportTranscriptsResponse> ImportTranscriptsAsync(Guid CreatorId, ImportTranscriptsRequest request)
        {
            var check = await ReadTranscriptsAsync(request.Files);
            if (check.Files.Any(f => !f.IsValid))
            {
                await _activityLogService.LogAsync(CreatorId, "transcriptImportFailed",
                    $"Refused {check.Files.Count} transcript file(s): {check.Files.Count(f => !f.IsValid)} invalid");
                return new ImportTranscriptsResponse { Files = check.Files, IsSuccess = false };
            }

            var transactionAt = DateTime.UtcNow;
            var newRows = check.Rows.Select(r => new Studentdata
            {
                StudentDataId = Guid.NewGuid(),
                DataSourceId = null,
                DataAttributeId = r.Row.DataAttribute.DataAttributeId,
                StudentId = r.Row.Student.AccountId,
                SubjectId = r.Row.Subject?.SubjectId,
                TermId = r.Row.Term.TermId,
                ProvenanceNote = $"Imported from transcript {r.FileName} on {transactionAt:yyyy-MM-dd}.",
                TransactionFrom = transactionAt,
                ValidFrom = r.Row.ValidFrom,
                IsMissing = false,
                Status = "original"
            }).ToList();

            // Encrypt every value with the student's key for its attribute (new keys are staged and saved below with the rows)
            var encrypted = await _attributeKeyService.EncryptValuesAsync(check.Rows.Select((r, index) => new PlainAttributeValue
            {
                StudentId = r.Row.Student.AccountId,
                DataAttributeId = r.Row.DataAttribute.DataAttributeId,
                RowId = newRows[index].StudentDataId,
                FieldName = DataValueField,
                Value = r.Row.Value
            }).ToList());
            for (var index = 0; index < newRows.Count; index++)
            {
                newRows[index].AttributeKeyId = encrypted[index].AttributeKeyId;
                newRows[index].DataValue = encrypted[index].Payload;
                _studentDataRepository.PrepareCreate(newRows[index]);
            }
            await _studentDataRepository.SaveAsync();

            var gradeFiles = check.Files.Count(f => f.ImportType == "grade");
            var replacedCount = check.Files.Sum(f => f.ReplaceCount);
            await _activityLogService.LogAsync(CreatorId, "transcriptImported",
                $"Imported {check.Files.Count} transcript file(s) ({gradeFiles} grade, {check.Files.Count - gradeFiles} attendance): {newRows.Count} value(s), {replacedCount} replacing a current value");
            // Compare the new values with the active indicators (A7.2): raise and clear flags
            var flagCheck = await _indicatorFlagService.CheckAsync(CreatorId);

            return new ImportTranscriptsResponse
            {
                ImportedFileCount = check.Files.Count,
                InsertedCount = newRows.Count,
                ReplacedCount = replacedCount,
                ClearedFlagCount = flagCheck.ClearedCount,
                RaisedFlagCount = flagCheck.RaisedCount,
                Files = check.Files,
                IsSuccess = true
            };
        }

        // Get a student's current data for a staff member: inside a case only the case category's purpose counts (and only the case's advisor may read);
        // outside a case any purpose the staff member's role has a policy for counts, or an approved emergency access. Every value shown is logged
        public async Task<GetStudentDataOfStudentResponse> GetStudentDataAsync(Guid AccessorId, Guid StudentId, Guid? CaseId, Guid? TermId)
        {
            var values = await BuildSnapshotAsync(AccessorId, StudentId, CaseId, TermId, null, "system");

            return new GetStudentDataOfStudentResponse
            {
                StudentId = StudentId,
                CaseId = CaseId,
                TermId = TermId,
                Values = values
            };
        }

        // Get a student's data as of a date: TimeAxis = system ("what AdviseCare knew on that date") or valid ("what was true on that date by today's knowledge")
        public async Task<GetStudentDataAsOfResponse> GetAsOfAsync(Guid AccessorId, Guid StudentId, GetStudentDataAsOfRequest request)
        {
            var values = await BuildSnapshotAsync(AccessorId, StudentId, request.CaseId, request.TermId, request.Date, request.TimeAxis);

            return new GetStudentDataAsOfResponse
            {
                StudentId = StudentId,
                CaseId = request.CaseId,
                TermId = request.TermId,
                Values = values,
                Date = request.Date,
                TimeAxis = request.TimeAxis
            };
        }

        // Compare a student's data on two dates on the same time axis; each date is authorized and logged on its own
        public async Task<CompareStudentDataResponse> CompareAsync(Guid AccessorId, Guid StudentId, CompareStudentDataRequest request)
        {
            var valuesA = (await BuildSnapshotAsync(AccessorId, StudentId, request.CaseId, request.TermId, request.DateA, request.TimeAxis))
                .ToDictionary(v => (v.DataAttributeId, v.TermId, v.SubjectId));
            var valuesB = (await BuildSnapshotAsync(AccessorId, StudentId, request.CaseId, request.TermId, request.DateB, request.TimeAxis))
                .ToDictionary(v => (v.DataAttributeId, v.TermId, v.SubjectId));

            return new CompareStudentDataResponse
            {
                StudentId = StudentId,
                CaseId = request.CaseId,
                TermId = request.TermId,
                DateA = request.DateA,
                DateB = request.DateB,
                Values = valuesA.Keys.Union(valuesB.Keys)
                    .Select(k =>
                    {
                        valuesA.TryGetValue(k, out var valueA);
                        valuesB.TryGetValue(k, out var valueB);
                        var any = valueA ?? valueB!;
                        return new CompareStudentDataValueResponse
                        {
                            DataAttributeId = k.DataAttributeId,
                            SubjectId = k.SubjectId,
                            TermId = k.TermId,
                            AttributeName = any.AttributeName,
                            SubjectCode = any.SubjectCode,
                            SubjectName = any.SubjectName,
                            TermName = any.TermName,
                            DateAValue = valueA,
                            DateBValue = valueB
                        };
                    })
                    .OrderBy(v => v.TermName)
                    .ThenBy(v => v.AttributeName)
                    .ThenBy(v => v.SubjectCode)
                    .ToList(),
                TimeAxis = request.TimeAxis
            };
        }

        // Get the signed-in student's own current data (no consent needed, not logged)
        public async Task<GetMyStudentDataResponse> GetMyDataAsync(Guid StudentId, Guid? TermId)
        {
            var values = await BuildSnapshotAsync(StudentId, StudentId, null, TermId, null, "system");

            return new GetMyStudentDataResponse
            {
                StudentId = StudentId,
                CaseId = null,
                TermId = TermId,
                Values = values
            };
        }

        // Export the signed-in student's own record as a JSON file: profile, current data, own cases and own access log (generated on the fly, not stored)
        public async Task<ExportMyStudentDataResponse> ExportMyDataAsync(Guid StudentId)
        {
            var profile = await _studentProfileService.GetMyProfileAsync(StudentId);
            var values = await BuildSnapshotAsync(StudentId, StudentId, null, null, null, "system");
            var cases = await _caseRepository.SearchAsyncIncludeOrderBy(c => c.StudentId == StudentId, c => c.OpenedAt, false,
                c => c.SupportCategory, c => c.Subject);
            var accessLogs = await _accessLogService.GetMyAccessLogsAsync(StudentId, new SearchMyAccessLogRequest());

            var document = new StudentDataExportDocumentResponse
            {
                Profile = profile,
                Values = values,
                Cases = cases.Select(c => new StudentDataExportCaseResponse
                {
                    CaseId = c.CaseId,
                    CategoryName = c.SupportCategory.CategoryName,
                    RequestReason = c.RequestReason,
                    SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = c.Subject?.SubjectName ?? string.Empty,
                    ClosedAt = c.ClosedAt,
                    OpenedAt = c.OpenedAt,
                    CurrentStage = c.CurrentStage,
                    Priority = c.Priority,
                    Status = c.Status
                }).ToList(),
                AccessLogs = accessLogs,
                ExportedAt = DateTime.UtcNow
            };

            await _activityLogService.LogAsync(StudentId, "dataExported",
                $"Exported own profile, {values.Count} value(s), {cases.Count} case(s) and {accessLogs.Count} access log entries as JSON");

            return new ExportMyStudentDataResponse
            {
                ContentType = "application/json",
                FileName = $"advisecare-my-data-{DateTime.UtcNow:yyyyMMdd}.json",
                Content = JsonSerializer.SerializeToUtf8Bytes(document, ExportJsonOptions)
            };
        }

        // Helper: one snapshot of a student's data (current, or as of a date on one time axis), passed through the access guard;
        // allowed values are decrypted and every value shown to staff is written to the access log in one save
        private async Task<List<StudentDataValueResponse>> BuildSnapshotAsync(Guid AccessorId, Guid StudentId, Guid? CaseId, Guid? TermId, DateOnly? Date, string TimeAxis)
        {
            if (!await _accountRepository.ExistsAsync(a => a.AccountId == StudentId))
                throw new NotFoundException("Student not found.");

            Guid? consentPurposeId = null;
            if (CaseId.HasValue)
            {
                var caseRecord = await _caseRepository.GetByIdAsyncInclude(CaseId.Value, c => c.SupportCategory)
                    ?? throw new NotFoundException("Case not found.");
                if (caseRecord.StudentId != StudentId)
                    throw new NotFoundException("Case not found for this student.");
                if (caseRecord.AdvisorId != AccessorId)
                    throw new ForbiddenException("Only the advisor of this case may read the student's data through it.");
                consentPurposeId = caseRecord.SupportCategory.ConsentPurposeId;
            }

            // End of the chosen UTC date
            DateTime? cutoff = Date.HasValue ? Date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
            var useValidTime = TimeAxis == "valid" && cutoff.HasValue;
            var rows = await _studentDataRepository.SearchAsyncInclude(d =>
                    d.StudentId == StudentId &&
                    (TermId == null || d.TermId == TermId) &&
                    (cutoff == null || (useValidTime ? d.ValidFrom < cutoff : d.TransactionFrom < cutoff)),
                d => d.DataAttribute, d => d.Subject, d => d.Term);

            // The row in effect for each attribute, term and subject (no subject for a per-term attribute): system time = latest recorded; valid time = latest valid, a later recording winning on ties
            var currentRows = rows
                .GroupBy(d => new { d.DataAttributeId, d.TermId, d.SubjectId })
                .Select(g => useValidTime
                    ? g.OrderByDescending(d => d.ValidFrom).ThenByDescending(d => d.TransactionFrom).First()
                    : g.OrderByDescending(d => d.TransactionFrom).ThenByDescending(d => d.ValidFrom).First())
                .OrderBy(d => d.Term.StartDate)
                .ThenBy(d => d.DataAttribute.AttributeName)
                .ThenBy(d => d.Subject?.SubjectCode)
                .ToList();
            if (currentRows.Count == 0)
                return new List<StudentDataValueResponse>();

            var decisions = (await _accessGuardService.CheckAccessAsync(AccessorId, StudentId,
                    currentRows.Select(d => d.DataAttributeId).Distinct().ToList(), consentPurposeId))
                .ToDictionary(d => d.DataAttributeId);

            var readable = currentRows
                .Where(d => decisions[d.DataAttributeId].IsAllowed && !d.IsMissing && d.AttributeKeyId.HasValue)
                .ToList();
            var decrypted = (await _attributeKeyService.DecryptValuesAsync(readable.Select(d => new EncryptedAttributeValue
                {
                    AttributeKeyId = d.AttributeKeyId!.Value,
                    StudentId = d.StudentId,
                    DataAttributeId = d.DataAttributeId,
                    RowId = d.StudentDataId,
                    FieldName = DataValueField,
                    Payload = d.DataValue
                }).ToList()))
                .ToDictionary(v => v.RowId);

            // One access log row per value shown (null purpose = emergency access; the student's own reads are not logged)
            await _accessGuardService.LogAccessBatchAsync(AccessorId, readable.Select(d => new AccessLogEntry
            {
                StudentId = StudentId,
                StudentDataId = d.StudentDataId,
                ConsentPurposeId = decisions[d.DataAttributeId].ConsentPurposeId,
                AccessedResource = d.Subject == null
                    ? $"Student data: {d.DataAttribute.AttributeName} ({d.Term.TermName})"
                    : $"Student data: {d.DataAttribute.AttributeName} - {d.Subject.SubjectCode} ({d.Term.TermName})"
            }).ToList());

            return currentRows.Select(d =>
            {
                var isShared = decisions[d.DataAttributeId].IsAllowed;
                decrypted.TryGetValue(d.StudentDataId, out var value);
                return new StudentDataValueResponse
                {
                    DataAttributeId = d.DataAttributeId,
                    DataSourceId = isShared ? d.DataSourceId : null,
                    StudentDataId = isShared ? d.StudentDataId : null,
                    SubjectId = d.SubjectId,
                    TermId = d.TermId,
                    AttributeName = d.DataAttribute.AttributeName,
                    ProvenanceNote = isShared ? d.ProvenanceNote ?? string.Empty : string.Empty,
                    SubjectCode = d.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = d.Subject?.SubjectName ?? string.Empty,
                    TermName = d.Term.TermName,
                    Value = value?.Value,
                    TransactionFrom = isShared ? d.TransactionFrom : null,
                    ValidFrom = isShared ? d.ValidFrom : null,
                    HasValue = !d.IsMissing && d.ErasedAt == null,
                    IsErased = isShared && (d.ErasedAt != null || (value?.IsErased ?? false)),
                    IsMissing = isShared && d.IsMissing,
                    IsShared = isShared,
                    DataType = d.DataAttribute.DataType,
                    SummaryMethod = d.DataAttribute.SummaryMethod,
                    Status = isShared ? d.Status : string.Empty
                };
            }).ToList();
        }

        // Helper: read the file and the column mapping, then check every row against the students, data attributes, terms and subjects;
        // rows of archived data attributes are set aside in RejectedRows instead of failing the import
        private async Task<ImportCheck> ReadImportAsync(CreateStudentDataRequest request)
        {
            var check = new ImportCheck();
            var rows = ReadRows(request.File);
            if (rows.Count == 0)
            {
                check.Errors.Add(new StudentDataRowErrorResponse { RowNumber = 1, Messages = { "The file has no data rows under the header row." } });
                return check;
            }

            ColumnMapping mapping;
            try
            {
                mapping = JsonSerializer.Deserialize<ColumnMapping>(request.ColumnMapping, MappingJsonOptions)
                    ?? throw new ValidationException("Column mapping must be a JSON object.");
            }
            catch (JsonException)
            {
                throw new ValidationException("Column mapping must be valid JSON.");
            }

            var mappedHeaders = new[] { mapping.StudentIdentifier, mapping.DataAttribute, mapping.Value, mapping.Term, mapping.ValidFrom }
                .Select(h => h?.Trim() ?? string.Empty).ToArray();
            // The subject column is optional: it only has to be mapped when the file holds values of per-subject data attributes
            var subjectHeader = mapping.Subject?.Trim() ?? string.Empty;
            if (subjectHeader.Length > 0)
                mappedHeaders = mappedHeaders.Append(subjectHeader).ToArray();
            var mappingError =
                mappedHeaders.Take(5).Any(string.IsNullOrWhiteSpace) ? "Every field (studentIdentifier, dataAttribute, value, term, validFrom) must be mapped to a column." :
                mappedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mappedHeaders.Length ? "Each field must map to a different column." :
                mappedHeaders.Where(h => !rows[0].Values.ContainsKey(h)).ToList() is { Count: > 0 } notFound ? $"Mapped column(s) not found in the file: {string.Join(", ", notFound)}." :
                null;
            if (mappingError != null)
            {
                check.Errors.Add(new StudentDataRowErrorResponse { RowNumber = 1, Messages = { mappingError } });
                return check;
            }

            // Everything the rows are checked against, loaded once
            var studentIds = (await _accountRoleRepository.SearchAsync(ar => ar.Role.RoleName == "Student")).Select(ar => ar.AccountId).ToHashSet();
            var accounts = await _accountRepository.SearchAsync(a => studentIds.Contains(a.AccountId));
            var profiles = await _studentProfileRepository.GetAllAsync();
            var studentsByIdentifier = BuildStudentLookup(accounts, profiles);
            var dataAttributes = (await _dataAttributeRepository.GetAllAsync()).ToDictionary(a => a.AttributeName, StringComparer.OrdinalIgnoreCase);
            var terms = (await _academicTermRepository.GetAllAsync()).GroupBy(t => t.TermName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var subjects = (await _subjectRepository.GetAllAsync()).ToDictionary(s => s.SubjectCode, StringComparer.OrdinalIgnoreCase);

            // One value per student, data attribute, term and subject in a file: a second row for the same key is reported, not silently stacked
            var seenKeys = new Dictionary<(Guid, Guid, Guid, Guid?), int>();

            foreach (var row in rows)
            {
                var identifier = GetValue(row, mapping.StudentIdentifier);
                var attributeName = GetValue(row, mapping.DataAttribute);
                var value = GetValue(row, mapping.Value);
                var termName = GetValue(row, mapping.Term);
                var validFromText = GetValue(row, mapping.ValidFrom);
                var subjectCode = GetValue(row, mapping.Subject);
                var messages = new List<string>();

                List<Account>? matches = null;
                if (string.IsNullOrEmpty(identifier)) messages.Add("student identifier is empty");
                else if (!studentsByIdentifier.TryGetValue(identifier, out matches)) messages.Add($"student '{identifier}' does not exist or is not a student");
                else if (matches.Count != 1) messages.Add($"student identifier '{identifier}' matches more than one student");

                Dataattribute? dataAttribute = null;
                if (string.IsNullOrEmpty(attributeName)) messages.Add("data attribute is empty");
                else if (!dataAttributes.TryGetValue(attributeName, out dataAttribute)) messages.Add($"data attribute '{attributeName}' does not exist");
                else if (string.IsNullOrEmpty(value)) messages.Add("value is empty");
                else if (!DataValueValidator.IsValid(dataAttribute.DataType, value)) messages.Add($"value '{value}' is not valid for the {dataAttribute.DataType} data attribute '{attributeName}'");

                Subject? subject = null;
                if (dataAttribute != null && dataAttribute.IsSubjectSpecific)
                {
                    if (string.IsNullOrEmpty(subjectCode)) messages.Add($"subject is empty, but '{dataAttribute.AttributeName}' is recorded per subject");
                    else if (!subjects.TryGetValue(subjectCode, out subject)) messages.Add($"subject '{subjectCode}' does not exist");
                }
                else if (dataAttribute != null && !string.IsNullOrEmpty(subjectCode))
                    messages.Add($"'{dataAttribute.AttributeName}' is recorded per term, so its subject must be empty");

                List<Academicterm>? termMatches = null;
                if (string.IsNullOrEmpty(termName)) messages.Add("term is empty");
                else if (!terms.TryGetValue(termName, out termMatches)) messages.Add($"term '{termName}' does not exist");
                else if (termMatches.Count != 1) messages.Add($"term name '{termName}' matches more than one term");

                var validFrom = DateTimeOffset.MinValue;
                if (string.IsNullOrEmpty(validFromText)) messages.Add("valid-from date is empty");
                else if (!DateTimeOffset.TryParse(validFromText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out validFrom))
                    messages.Add($"valid-from date '{validFromText}' is not a valid date");

                if (messages.Count > 0)
                {
                    check.Errors.Add(new StudentDataRowErrorResponse { RowNumber = row.RowNumber, Messages = messages });
                    continue;
                }
                if (dataAttribute!.Status == "archived")
                {
                    check.RejectedRows.Add(new StudentDataRowErrorResponse { RowNumber = row.RowNumber, Messages = { $"data attribute '{dataAttribute.AttributeName}' is archived, so this row was not imported" } });
                    continue;
                }

                var key = (matches![0].AccountId, dataAttribute.DataAttributeId, termMatches![0].TermId, subject?.SubjectId);
                if (seenKeys.TryGetValue(key, out var firstRow))
                {
                    var subjectText = subject == null ? string.Empty : $" in subject '{subject.SubjectCode}'";
                    check.Errors.Add(new StudentDataRowErrorResponse { RowNumber = row.RowNumber, Messages = { $"row {firstRow} already gives '{dataAttribute.AttributeName}' of student '{identifier}' for term '{termName}'{subjectText}; keep one of the two rows" } });
                    continue;
                }
                seenKeys[key] = row.RowNumber;

                check.Rows.Add(new ImportRow(matches![0], dataAttribute, termMatches![0], subject, value, validFrom.UtcDateTime));
            }

            return check;
        }

        // Helper: read an .xlsx or .csv file into rows (header row = column names)
        private List<ExcelRow> ReadRows(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new ValidationException("A non-empty .xlsx or .csv file is required.");

            var extension = Path.GetExtension(file.FileName);
            var isCsv = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
            if (!isCsv && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Only .xlsx and .csv files can be imported.");

            try
            {
                using var stream = file.OpenReadStream();
                return isCsv ? _csvReader.ReadRows(stream) : _excelReader.ReadRows(stream);
            }
            catch (InvalidDataException exception)
            {
                throw new ValidationException(exception.Message);
            }
            catch (Exception)
            {
                throw new ValidationException($"The file could not be read as {extension} data.");
            }
        }

        // Helper: a trimmed cell value by its mapped column name
        private static string GetValue(ExcelRow row, string? header)
        {
            return !string.IsNullOrWhiteSpace(header) && row.Values.TryGetValue(header.Trim(), out var value) ? value.Trim() : string.Empty;
        }

        // Helper: Student accounts by email, username and student code (an identifier shared by two students is reported as ambiguous)
        private static Dictionary<string, List<Account>> BuildStudentLookup(List<Account> students, List<Studentprofile> profiles)
        {
            var lookup = new Dictionary<string, List<Account>>(StringComparer.OrdinalIgnoreCase);
            void Add(string? identifier, Account student)
            {
                if (string.IsNullOrWhiteSpace(identifier))
                    return;
                if (!lookup.TryGetValue(identifier, out var matches))
                    lookup[identifier] = matches = new List<Account>();
                if (matches.All(m => m.AccountId != student.AccountId))
                    matches.Add(student);
            }

            var studentsById = students.ToDictionary(s => s.AccountId);
            foreach (var student in students)
            {
                Add(student.Email, student);
                Add(student.UserName, student);
            }
            foreach (var profile in profiles)
            {
                if (studentsById.TryGetValue(profile.StudentId, out var student))
                    Add(profile.StudentCode, student);
            }
            return lookup;
        }

        // Helper: read and check transcript PDFs (one student and one type per file); a problem makes that file invalid, the others are still read
        private async Task<TranscriptCheck> ReadTranscriptsAsync(List<IFormFile>? Files)
        {
            var uploads = (Files ?? new List<IFormFile>()).Where(f => f != null).ToList();
            if (uploads.Count == 0)
                throw new ValidationException("At least one PDF file is required.");
            if (uploads.Count > MaxTranscriptFiles)
                throw new ValidationException($"Send at most {MaxTranscriptFiles} transcript files at a time.");

            // Read every file first, so everything the checks need can be loaded once
            var read = new List<(string FileName, string Hash, ParsedTranscript Parsed, List<string> Errors)>();
            foreach (var upload in uploads)
            {
                var fileName = Path.GetFileName(upload.FileName ?? string.Empty);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "transcript.pdf";
                var errors = new List<string>();
                var content = Array.Empty<byte>();
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Only PDF files can be imported.");
                else if (upload.Length == 0)
                    errors.Add("The file is empty.");
                else if (upload.Length > MaxTranscriptBytes)
                    errors.Add("The file is larger than 5 MB.");
                else
                {
                    using var stream = new MemoryStream();
                    await upload.CopyToAsync(stream);
                    content = stream.ToArray();
                }

                var parsed = content.Length > 0 ? _transcriptPdfReader.Read(content) : new ParsedTranscript();
                errors.AddRange(parsed.Errors);
                read.Add((fileName, Convert.ToHexString(SHA256.HashData(content)), parsed, errors));
            }

            var codes = read.Select(r => r.Parsed.StudentCode).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var profiles = codes.Count == 0
                ? new List<Studentprofile>()
                : await _studentProfileRepository.SearchAsyncInclude(p => codes.Contains(p.StudentCode), p => p.Student);
            var profileByCode = profiles
                .GroupBy(p => p.StudentCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var attributes = await _dataAttributeRepository.SearchAsync(a => a.AttributeName == TranscriptGradeAttribute || a.AttributeName == TranscriptAttendanceAttribute);
            var gradeAttribute = TranscriptAttribute(attributes, TranscriptGradeAttribute);
            var attendanceAttribute = TranscriptAttribute(attributes, TranscriptAttendanceAttribute);
            var subjects = (await _subjectRepository.GetAllAsync())
                .GroupBy(s => s.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var terms = (await _academicTermRepository.GetAllAsync())
                .GroupBy(t => TermKey(t.TermName))
                .ToDictionary(g => g.Key, g => g.First());

            // (student, attribute, term, subject) of the values the students already have, to count the ones an import replaces
            var studentIds = profiles.Select(p => p.StudentId).ToList();
            var attributeIds = new[] { gradeAttribute.DataAttributeId, attendanceAttribute.DataAttributeId };
            var currentKeys = studentIds.Count == 0
                ? new HashSet<(Guid, Guid, Guid, Guid)>()
                : (await _studentDataRepository.SearchAsync(d => studentIds.Contains(d.StudentId) && attributeIds.Contains(d.DataAttributeId)
                        && d.SubjectId != null && !d.IsMissing && d.ErasedAt == null))
                    .Select(d => (d.StudentId, d.DataAttributeId, d.TermId, d.SubjectId!.Value))
                    .ToHashSet();

            // One file per student and type, and each file once
            var check = new TranscriptCheck();
            var seenHashes = new Dictionary<string, string>();
            var seenStudentTypes = new Dictionary<(Guid, string), string>();
            foreach (var (fileName, hash, parsed, uploadErrors) in read)
            {
                var file = new TranscriptFileResponse
                {
                    CurriculumCode = parsed.CurriculumCode,
                    FileName = fileName,
                    FullNameInFile = parsed.FullName,
                    StudentCode = parsed.StudentCode,
                    ImportType = parsed.ImportType
                };
                file.Errors.AddRange(uploadErrors);

                Account? student = null;
                if (file.Errors.Count == 0)
                {
                    if (parsed.StudentCode.Length == 0)
                        file.Errors.Add("No \"Student ID:\" was found at the top of the file.");
                    else if (!profileByCode.TryGetValue(parsed.StudentCode, out var profile))
                        file.Errors.Add($"Student {parsed.StudentCode} has no student profile in AdviseCare.");
                    else if (profile.Student?.Status != "active")
                        file.Errors.Add($"The account of student {parsed.StudentCode} is not active.");
                    else
                    {
                        student = profile.Student;
                        file.StudentId = student.AccountId;
                        file.StudentName = student.FullName ?? string.Empty;
                        if (parsed.FullName.Length > 0 && NormalizeName(parsed.FullName) != NormalizeName(student.FullName ?? string.Empty))
                            file.Warnings.Add($"The name in the file ({parsed.FullName}) is not the account's name ({student.FullName}).");
                    }
                }

                if (HasContent(hash) && seenHashes.TryGetValue(hash, out var sameFile))
                    file.Errors.Add($"The same file as {sameFile} is uploaded twice.");
                else if (HasContent(hash))
                    seenHashes[hash] = fileName;

                var rows = new List<ImportRow>();
                if (file.Errors.Count == 0 && student != null)
                {
                    var dataAttribute = parsed.ImportType == "grade" ? gradeAttribute : attendanceAttribute;
                    rows = BuildTranscriptRows(parsed, student, dataAttribute, subjects, terms, file);

                    // An invalid file does not claim its student and type, so the fixed file can be uploaded next to it
                    var studentType = (student.AccountId, parsed.ImportType);
                    if (file.Errors.Count == 0 && seenStudentTypes.TryGetValue(studentType, out var otherFile))
                        file.Errors.Add($"{otherFile} already has the {parsed.ImportType} file of {parsed.StudentCode}.");
                    else if (file.Errors.Count == 0)
                        seenStudentTypes[studentType] = fileName;
                }

                file.IsValid = file.Errors.Count == 0;
                file.RowCount = rows.Count;
                if (file.IsValid)
                {
                    file.InsertCount = rows.Count;
                    file.ReplaceCount = rows.Count(r => currentKeys.Contains((r.Student.AccountId, r.DataAttribute.DataAttributeId, r.Term.TermId, r.Subject!.SubjectId)));
                    check.Rows.AddRange(rows.Select(r => (r, fileName)));
                }
                check.Files.Add(file);
            }
            return check;

            // An empty upload has the hash of no content and is reported by its own error, not as a duplicate
            static bool HasContent(string Hash) => Hash != EmptyHash;
        }

        // Helper: turn the PDF's text rows into import rows. A row without a semester (not started yet) or without a value ("-", e.g. exempt or
        // studying) is left out with a warning; a semester or subject code that is not in AdviseCare is an error, like in the Excel/CSV import
        private static List<ImportRow> BuildTranscriptRows(ParsedTranscript Parsed, Account Student, Dataattribute DataAttribute,
            Dictionary<string, Subject> Subjects, Dictionary<string, Academicterm> Terms, TranscriptFileResponse File)
        {
            var rows = new List<ImportRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var isGrade = Parsed.ImportType == "grade";
            foreach (var row in Parsed.Rows)
            {
                var code = row.SubjectCode.Trim();
                var label = $"Row {row.RowNumber}" + (code.Length > 0 && code != "-" ? $" ({code})" : string.Empty);
                var semester = row.Semester.Trim();
                var valueText = row.Value.Trim();
                if (semester.Length == 0 || semester == "-" || valueText.Length == 0 || valueText == "-")
                {
                    File.SkippedRowCount++;
                    continue;
                }

                var season = TranscriptSemesterPattern.Match(semester.Replace(" ", string.Empty));
                if (!season.Success)
                {
                    File.Errors.Add($"{label}: semester \"{semester}\" is not like Fall2024.");
                    continue;
                }
                var termName = $"{char.ToUpperInvariant(season.Groups[1].Value[0])}{season.Groups[1].Value[1..].ToLowerInvariant()} {season.Groups[2].Value}";
                if (!Terms.TryGetValue(TermKey(termName), out var term))
                {
                    File.Errors.Add($"{label}: term '{termName}' does not exist.");
                    continue;
                }
                if (code.Length == 0 || code == "-")
                {
                    File.Errors.Add($"{label}: the subject code is empty.");
                    continue;
                }
                if (!Subjects.TryGetValue(code, out var subject))
                {
                    File.Errors.Add($"{label}: subject '{code}' does not exist.");
                    continue;
                }

                // "8.3", "8,3", "85%" -> number; no thousands separators, so "8,3" is never read as 83
                if (!decimal.TryParse(valueText.TrimEnd('%').Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
                {
                    File.Errors.Add($"{label}: \"{valueText}\" is not a number.");
                    continue;
                }
                if (number < 0 || number > (isGrade ? 10m : 100m))
                {
                    File.Errors.Add(isGrade
                        ? $"{label}: grade {valueText} is not between 0 and 10."
                        : $"{label}: attendance {valueText} is not between 0 and 100.");
                    continue;
                }
                if (!seen.Add($"{subject.SubjectCode}|{term.TermName}"))
                {
                    File.Errors.Add($"{label}: {subject.SubjectCode} in {term.TermName} appears more than once.");
                    continue;
                }

                // Stored like every numeric STUDENTDATA value: invariant decimal text (DataValueValidator)
                rows.Add(new ImportRow(Student, DataAttribute, term, subject, Math.Round(number, 2).ToString(CultureInfo.InvariantCulture), TranscriptValidFrom(term, DateTime.UtcNow)));
            }

            if (File.SkippedRowCount > 0)
                File.Warnings.Add($"{File.SkippedRowCount} row(s) have no semester or no value yet (not started, studying, exempt) and are left out.");
            if (rows.Count == 0 && File.Errors.Count == 0)
                File.Errors.Add("No subject row with a semester and a value was found in the file.");
            return rows;
        }

        // Helper: one of the two per-subject data attributes the transcripts fill; it must exist, be active and be recorded per subject
        private static Dataattribute TranscriptAttribute(List<Dataattribute> Attributes, string AttributeName)
        {
            var attribute = Attributes.FirstOrDefault(a => a.AttributeName == AttributeName)
                ?? throw new ValidationException($"Data attribute '{AttributeName}' is not in AdviseCare, so transcripts cannot be imported.");
            if (attribute.Status != "active")
                throw new ValidationException($"Data attribute '{AttributeName}' is archived, so transcripts cannot be imported.");
            if (!attribute.IsSubjectSpecific)
                throw new ValidationException($"Data attribute '{AttributeName}' is not recorded per subject, so transcripts cannot be imported.");
            return attribute;
        }

        // Helper: valid from the import day, kept inside the term (the term's end once it is over, its start before it begins)
        private static DateTime TranscriptValidFrom(Academicterm Term, DateTime TransactionAt)
        {
            var date = TransactionAt.Date;
            if (date > Term.EndDate.Date)
                return Term.EndDate.Date;
            if (date < Term.StartDate.Date)
                return Term.StartDate.Date;
            return date;
        }

        // Helper: "Fall 2026", "Fall2026" and "fall 2026" are the same term
        private static string TermKey(string TermName) => new string(TermName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        // Helper: the same person's name, ignoring case, accents and spacing
        private static string NormalizeName(string Name) =>
            new string(Name.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD).Where(char.IsLetter).ToArray()).ToLowerInvariant();

        // Helper: load Student data rows with their attribute, student, subject and term, newest first
        private async Task<List<Studentdata>> GetRowsAsync(Expression<Func<Studentdata, bool>> predicate)
        {
            return await _studentDataRepository.SearchAsyncIncludeOrderBy(predicate, d => d.TransactionFrom, false,
                d => d.DataAttribute, d => d.Student, d => d.Subject, d => d.Term);
        }

        // Helper types for the import: the column mapping JSON, one checked row, and the result of the check
        private class ColumnMapping
        {
            public string? StudentIdentifier { get; set; }
            public string? DataAttribute { get; set; }
            public string? Value { get; set; }
            public string? Term { get; set; }
            public string? ValidFrom { get; set; }
            public string? Subject { get; set; }
        }

        private record ImportRow(Account Student, Dataattribute DataAttribute, Academicterm Term, Subject? Subject, string Value, DateTime ValidFrom);

        private class ImportCheck
        {
            public List<ImportRow> Rows { get; } = new();
            public List<StudentDataRowErrorResponse> RejectedRows { get; } = new();
            public List<StudentDataRowErrorResponse> Errors { get; } = new();
        }

        // The transcript files as checked, and the rows of the valid ones with the file each came from (for its provenance note)
        private class TranscriptCheck
        {
            public List<TranscriptFileResponse> Files { get; } = new();
            public List<(ImportRow Row, string FileName)> Rows { get; } = new();
        }
    }
}
