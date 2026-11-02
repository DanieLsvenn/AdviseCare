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
using System.Text.Json;

namespace AdviseCare.Application.Services
{
    // Student data (STUDENTDATA, the bitemporal ledger of data attribute values): import, consent-guarded reads, point-in-time views and the student's own export
    public class StudentDataService : IStudentDataService
    {
        private const string DataValueField = "dataValue";
        private static readonly JsonSerializerOptions MappingJsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IGenericRepository<Academicterm> _academicTermRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IExcelReader _excelReader;
        private readonly ICsvReader _csvReader;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IAccessGuardService _accessGuardService;
        private readonly IAccessLogService _accessLogService;
        private readonly IStudentProfileService _studentProfileService;
        private readonly IActivityLogService _activityLogService;

        public StudentDataService(
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IGenericRepository<Academicterm> academicTermRepository,
            IGenericRepository<Case> caseRepository,
            IExcelReader excelReader,
            ICsvReader csvReader,
            IAttributeKeyService attributeKeyService,
            IAccessGuardService accessGuardService,
            IAccessLogService accessLogService,
            IStudentProfileService studentProfileService,
            IActivityLogService activityLogService)
        {
            _studentDataRepository = studentDataRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _studentProfileRepository = studentProfileRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _academicTermRepository = academicTermRepository;
            _caseRepository = caseRepository;
            _excelReader = excelReader;
            _csvReader = csvReader;
            _attributeKeyService = attributeKeyService;
            _accessGuardService = accessGuardService;
            _accessLogService = accessLogService;
            _studentProfileService = studentProfileService;
            _activityLogService = activityLogService;
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
                TermId = d.TermId,
                AttributeName = d.DataAttribute.AttributeName,
                ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                StudentFullName = d.Student.FullName,
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
                    TermId = d.TermId,
                    AttributeName = d.DataAttribute.AttributeName,
                    ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                    StudentFullName = d.Student.FullName,
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
                TermId = row.TermId,
                AttributeName = row.DataAttribute.AttributeName,
                ProvenanceNote = row.ProvenanceNote ?? string.Empty,
                StudentFullName = row.Student.FullName,
                TermName = row.Term.TermName,
                ErasedAt = row.ErasedAt,
                TransactionFrom = row.TransactionFrom,
                ValidFrom = row.ValidFrom,
                IsMissing = row.IsMissing,
                Status = row.Status
            };
        }

        // Search Student data rows by attribute, student, term, missing flag or status (e.g. every missing value of a term; never the values)
        public async Task<List<SearchStudentDataResponse>> SearchAsync(SearchStudentDataRequest request)
        {
            var rows = await GetRowsAsync(d =>
                (request.DataAttributeId == null || d.DataAttributeId == request.DataAttributeId) &&
                (request.StudentId == null || d.StudentId == request.StudentId) &&
                (request.TermId == null || d.TermId == request.TermId) &&
                (request.IsMissing == null || d.IsMissing == request.IsMissing) &&
                (string.IsNullOrWhiteSpace(request.Status) || d.Status == request.Status));

            return rows.Select(d => new SearchStudentDataResponse
            {
                StudentDataId = d.StudentDataId,
                DataAttributeId = d.DataAttributeId,
                DataSourceId = d.DataSourceId,
                StudentId = d.StudentId,
                TermId = d.TermId,
                AttributeName = d.DataAttribute.AttributeName,
                ProvenanceNote = d.ProvenanceNote ?? string.Empty,
                StudentFullName = d.Student.FullName,
                TermName = d.Term.TermName,
                ErasedAt = d.ErasedAt,
                TransactionFrom = d.TransactionFrom,
                ValidFrom = d.ValidFrom,
                IsMissing = d.IsMissing,
                Status = d.Status
            }).ToList();
        }

        // Create Student data by importing an .xlsx/.csv file: every valid row is encrypted and inserted as a new row (never an update), every enrolled student
        // without a value for an imported attribute/term gets a missing row, all in one save. Any invalid row fails the whole import; rows of archived
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

            // Missing rows: every enrolled student without a value for an imported attribute/term pair
            var enrolledStudentIds = (await _studentProfileRepository.SearchAsyncInclude(p =>
                    p.AcademicStatus == "enrolled" && p.Student.Status == "active", p => p.Student))
                .Select(p => p.StudentId)
                .ToList();
            var importedTriples = import.Rows.Select(r => (r.Student.AccountId, r.DataAttribute.DataAttributeId, r.Term.TermId)).ToHashSet();
            var importedPairs = import.Rows.Select(r => (r.DataAttribute, r.Term)).DistinctBy(p => (p.DataAttribute.DataAttributeId, p.Term.TermId)).ToList();
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

            return new CreateStudentDataResponse
            {
                InsertedCount = newRows.Count,
                MissingCount = missingCount,
                RejectedRows = import.RejectedRows,
                Errors = new List<StudentDataRowErrorResponse>(),
                IsSuccess = true
            };
        }

        // Preview an import file: the header row and up to five sample rows, nothing is written
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
                }).ToList()
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
                .ToDictionary(v => (v.DataAttributeId, v.TermId));
            var valuesB = (await BuildSnapshotAsync(AccessorId, StudentId, request.CaseId, request.TermId, request.DateB, request.TimeAxis))
                .ToDictionary(v => (v.DataAttributeId, v.TermId));

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
                            TermId = k.TermId,
                            AttributeName = any.AttributeName,
                            TermName = any.TermName,
                            DateAValue = valueA,
                            DateBValue = valueB
                        };
                    })
                    .OrderBy(v => v.TermName)
                    .ThenBy(v => v.AttributeName)
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
                d => d.DataAttribute, d => d.Term);

            // The row in effect for each attribute and term: system time = latest recorded; valid time = latest valid, a later recording winning on ties
            var currentRows = rows
                .GroupBy(d => new { d.DataAttributeId, d.TermId })
                .Select(g => useValidTime
                    ? g.OrderByDescending(d => d.ValidFrom).ThenByDescending(d => d.TransactionFrom).First()
                    : g.OrderByDescending(d => d.TransactionFrom).ThenByDescending(d => d.ValidFrom).First())
                .OrderBy(d => d.Term.StartDate)
                .ThenBy(d => d.DataAttribute.AttributeName)
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
                AccessedResource = $"Student data: {d.DataAttribute.AttributeName} ({d.Term.TermName})"
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
                    TermId = d.TermId,
                    AttributeName = d.DataAttribute.AttributeName,
                    ProvenanceNote = isShared ? d.ProvenanceNote ?? string.Empty : string.Empty,
                    TermName = d.Term.TermName,
                    Value = value?.Value,
                    TransactionFrom = isShared ? d.TransactionFrom : null,
                    ValidFrom = isShared ? d.ValidFrom : null,
                    HasValue = !d.IsMissing && d.ErasedAt == null,
                    IsErased = isShared && (d.ErasedAt != null || (value?.IsErased ?? false)),
                    IsMissing = isShared && d.IsMissing,
                    IsShared = isShared,
                    Status = isShared ? d.Status : string.Empty
                };
            }).ToList();
        }

        // Helper: read the file and the column mapping, then check every row against the students, data attributes and terms;
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
            var mappingError =
                mappedHeaders.Any(string.IsNullOrWhiteSpace) ? "Every field (studentIdentifier, dataAttribute, value, term, validFrom) must be mapped to a column." :
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

            foreach (var row in rows)
            {
                var identifier = GetValue(row, mapping.StudentIdentifier);
                var attributeName = GetValue(row, mapping.DataAttribute);
                var value = GetValue(row, mapping.Value);
                var termName = GetValue(row, mapping.Term);
                var validFromText = GetValue(row, mapping.ValidFrom);
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

                check.Rows.Add(new ImportRow(matches![0], dataAttribute, termMatches![0], value, validFrom.UtcDateTime));
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

        // Helper: load Student data rows with their attribute, student and term, newest first
        private async Task<List<Studentdata>> GetRowsAsync(Expression<Func<Studentdata, bool>> predicate)
        {
            return await _studentDataRepository.SearchAsyncIncludeOrderBy(predicate, d => d.TransactionFrom, false,
                d => d.DataAttribute, d => d.Student, d => d.Term);
        }

        // Helper types for the import: the column mapping JSON, one checked row, and the result of the check
        private class ColumnMapping
        {
            public string? StudentIdentifier { get; set; }
            public string? DataAttribute { get; set; }
            public string? Value { get; set; }
            public string? Term { get; set; }
            public string? ValidFrom { get; set; }
        }

        private record ImportRow(Account Student, Dataattribute DataAttribute, Academicterm Term, string Value, DateTime ValidFrom);

        private class ImportCheck
        {
            public List<ImportRow> Rows { get; } = new();
            public List<StudentDataRowErrorResponse> RejectedRows { get; } = new();
            public List<StudentDataRowErrorResponse> Errors { get; } = new();
        }
    }
}
