using AdviseCare.Application.Contracts.Requests.AcademicResult;
using AdviseCare.Application.Contracts.Responses.AcademicResult;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdviseCare.Application.Services
{
    // Academic results: the per-subject grade and attendance of each student per term (STUDENTSUBJECTRESULTS).
    // Import: the admin uploads transcript PDFs (one student and one type per file) into a batch; every file is read and checked on upload
    // and is valid or invalid on its own; commit writes the valid files in one transaction (an invalid file never blocks the others) and
    // upserts one row per student, subject and term. Reading: the student sees everything; staff see the grade part only when the student
    // shared the "GPA" data attribute with them and the attendance part only for "Attendance rate" (same access guard and access log as STUDENTDATA).
    public class AcademicResultService : IAcademicResultService
    {
        // Data attributes whose consent covers the grade part and the attendance part of the results
        public const string GradeAttributeName = "GPA";
        public const string AttendanceAttributeName = "Attendance rate";
        // Thresholds sent to the web app to mark subjects (FPT rule: at least 80% attendance; pass mark 5/10)
        public const decimal MinAttendanceRate = 80m;
        public const decimal PassingGrade = 5m;

        private const int MaxFilesPerUpload = 50;
        private const int MaxFilesPerBatch = 500;
        private const long MaxFileBytes = 5 * 1024 * 1024;
        private const int MaxRecentBatches = 100;

        private static readonly Regex SemesterPattern = new(@"^(spring|summer|fall)\s*(\d{4})$", RegexOptions.IgnoreCase);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Status texts of the transcript (squashed: lower case, letters only) -> stored value
        private static readonly Dictionary<string, string> Statuses = new()
        {
            ["passed"] = "passed",
            ["pass"] = "passed",
            ["notpassed"] = "notPassed",
            ["failed"] = "notPassed",
            ["fail"] = "notPassed",
            ["attendancefail"] = "attendanceFail",
            ["exempt"] = "exempt",
            ["exempted"] = "exempt",
            ["studying"] = "studying",
            ["notstarted"] = "notStarted"
        };

        private readonly IGenericRepository<Studentsubjectresult> _resultRepository;
        private readonly IGenericRepository<Transcriptimportbatch> _batchRepository;
        private readonly IGenericRepository<Transcriptimportfile> _fileRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Subject> _subjectRepository;
        private readonly IGenericRepository<Academicterm> _termRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly ITranscriptPdfReader _pdfReader;
        private readonly IAccessGuardService _accessGuardService;
        private readonly IActivityLogService _activityLogService;

        public AcademicResultService(
            IGenericRepository<Studentsubjectresult> resultRepository,
            IGenericRepository<Transcriptimportbatch> batchRepository,
            IGenericRepository<Transcriptimportfile> fileRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Subject> subjectRepository,
            IGenericRepository<Academicterm> termRepository,
            IGenericRepository<Faculty> facultyRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IGenericRepository<Case> caseRepository,
            ITranscriptPdfReader pdfReader,
            IAccessGuardService accessGuardService,
            IActivityLogService activityLogService)
        {
            _resultRepository = resultRepository;
            _batchRepository = batchRepository;
            _fileRepository = fileRepository;
            _studentProfileRepository = studentProfileRepository;
            _subjectRepository = subjectRepository;
            _termRepository = termRepository;
            _facultyRepository = facultyRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _caseRepository = caseRepository;
            _pdfReader = pdfReader;
            _accessGuardService = accessGuardService;
            _activityLogService = activityLogService;
        }

        // ================= Import =================

        // Start a batch with the first files
        public async Task<TranscriptImportBatchResponse> CreateBatchAsync(Guid AdminId, UploadTranscriptsRequest request)
        {
            var uploads = CheckUpload(request.Files, 0);
            var batch = new Transcriptimportbatch
            {
                ImportBatchId = Guid.NewGuid(),
                ImportedBy = AdminId,
                CreatedAt = DateTime.UtcNow,
                Status = "previewed"
            };
            _batchRepository.PrepareCreate(batch);
            await ReadFilesAsync(batch.ImportBatchId, new List<Transcriptimportfile>(), uploads);
            await _batchRepository.SaveAsync();
            return await GetBatchAsync(batch.ImportBatchId);
        }

        // Add more files to a batch that is still in preview (the web app sends a large batch in parts)
        public async Task<TranscriptImportBatchResponse> AddFilesAsync(Guid AdminId, Guid ImportBatchId, UploadTranscriptsRequest request)
        {
            var batch = await _batchRepository.GetByIdAsync(ImportBatchId)
                ?? throw new NotFoundException("Import batch not found.");
            if (batch.Status != "previewed")
                throw new ConflictException($"This import batch is already {batch.Status}; start a new one.");

            var existingFiles = await _fileRepository.SearchAsync(f => f.ImportBatchId == ImportBatchId);
            var uploads = CheckUpload(request.Files, existingFiles.Count);
            await ReadFilesAsync(ImportBatchId, existingFiles, uploads);
            await _fileRepository.SaveAsync();
            return await GetBatchAsync(ImportBatchId);
        }

        public async Task<TranscriptImportBatchResponse> GetBatchAsync(Guid ImportBatchId)
        {
            var batch = await _batchRepository.GetByIdAsyncInclude(ImportBatchId, b => b.ImportedByNavigation)
                ?? throw new NotFoundException("Import batch not found.");
            var files = (await _fileRepository.SearchAsyncInclude(f => f.ImportBatchId == ImportBatchId, f => f.Student))
                .OrderBy(f => f.UploadedAt)
                .ThenBy(f => f.FileName)
                .ToList();

            var response = new TranscriptImportBatchResponse
            {
                ImportBatchId = batch.ImportBatchId,
                ImportedById = batch.ImportedBy,
                ImportedByName = batch.ImportedByNavigation?.FullName ?? string.Empty,
                CreatedAt = batch.CreatedAt,
                CommittedAt = batch.CommittedAt,
                Status = batch.Status,
                TotalFiles = files.Count,
                ValidFiles = files.Count(f => f.Status == "valid"),
                InvalidFiles = files.Count(f => f.Status == "invalid"),
                CommittedFiles = files.Count(f => f.Status == "committed"),
                Files = files.Select(ToFileResponse).ToList()
            };

            // What the commit will add to the reference data
            if (batch.Status == "previewed")
            {
                var subjects = SubjectLookup(await _subjectRepository.GetAllAsync());
                var terms = TermLookup(await _termRepository.GetAllAsync());
                var facultyNames = (await _facultyRepository.GetAllAsync()).ToDictionary(f => f.FacultyId, f => f.FacultyName);
                var validFiles = files.Where(f => f.Status == "valid" && f.ParsedRows != null).ToList();
                var validRows = validFiles
                    .SelectMany(f => ReadRows(f.ParsedRows).Select(row => (f.ImportFileId, Row: row)))
                    .ToList();
                response.NewSubjects = NewSubjectsOf(validFiles, subjects)
                    .Select(s => new TranscriptNewSubjectResponse
                    {
                        SubjectCode = s.Code,
                        SubjectName = s.Name,
                        FileCount = s.FileCount,
                        FacultyId = s.FacultyId,
                        FacultyName = s.FacultyId.HasValue && facultyNames.TryGetValue(s.FacultyId.Value, out var facultyName) ? facultyName : string.Empty
                    })
                    .ToList();
                response.NewTerms = validRows
                    .Select(r => r.Row.TermName)
                    .Where(name => !terms.ContainsKey(TermKey(name)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => TermStart(name))
                    .ToList();
            }
            return response;
        }

        public Task<List<TranscriptImportBatchSummaryResponse>> GetBatchesAsync(int Count)
        {
            var take = Math.Clamp(Count, 1, MaxRecentBatches);
            var batches = _batchRepository.GetQueryable()
                .OrderByDescending(b => b.CreatedAt)
                .Take(take)
                .Select(b => new TranscriptImportBatchSummaryResponse
                {
                    ImportBatchId = b.ImportBatchId,
                    ImportedByName = b.ImportedByNavigation.FullName,
                    CreatedAt = b.CreatedAt,
                    CommittedAt = b.CommittedAt,
                    Status = b.Status,
                    TotalFiles = b.Transcriptimportfiles.Count(),
                    ValidFiles = b.Transcriptimportfiles.Count(f => f.Status == "valid"),
                    InvalidFiles = b.Transcriptimportfiles.Count(f => f.Status == "invalid"),
                    CommittedFiles = b.Transcriptimportfiles.Count(f => f.Status == "committed")
                })
                .ToList();
            return Task.FromResult(batches);
        }

        // Write every valid file of the batch in one transaction; invalid files are left out
        public async Task<CommitTranscriptImportResponse> CommitAsync(Guid AdminId, Guid ImportBatchId, CommitTranscriptImportRequest request)
        {
            var batch = await _batchRepository.GetByIdAsync(ImportBatchId)
                ?? throw new NotFoundException("Import batch not found.");
            if (batch.Status != "previewed")
                throw new ConflictException($"This import batch is already {batch.Status}.");

            var files = await _fileRepository.SearchAsyncInclude(f => f.ImportBatchId == ImportBatchId, f => f.Student);
            var validFiles = files
                .Where(f => f.Status == "valid" && f.StudentId.HasValue && f.ImportType != null && f.ParsedRows != null)
                .OrderBy(f => f.UploadedAt)
                .ToList();
            if (validFiles.Count == 0)
                throw new ValidationException("The batch has no valid file to import.");
            var rowsByFile = validFiles.ToDictionary(f => f.ImportFileId, f => ReadRows(f.ParsedRows));
            var allRows = rowsByFile.Values.SelectMany(rows => rows).ToList();

            var now = DateTime.UtcNow;
            var response = new CommitTranscriptImportResponse
            {
                ImportBatchId = ImportBatchId,
                CommittedFiles = validFiles.Count,
                SkippedFiles = files.Count - validFiles.Count
            };

            // Subjects not in AdviseCare yet: created under the faculty of a student who has them,
            // or under the faculty the admin chose when none of those students belongs to a faculty
            var subjects = SubjectLookup(await _subjectRepository.GetAllAsync());
            var newSubjects = NewSubjectsOf(validFiles, subjects);
            var withoutFaculty = newSubjects.Where(s => !s.FacultyId.HasValue).ToList();
            if (withoutFaculty.Count > 0 && !request.FacultyId.HasValue)
            {
                var listed = string.Join(", ", withoutFaculty.Take(10).Select(s => s.Code)) + (withoutFaculty.Count > 10 ? ", ..." : string.Empty);
                throw new ValidationException($"Choose the faculty of the {withoutFaculty.Count} new subject(s) whose students have no faculty: {listed}.");
            }
            if (withoutFaculty.Count > 0 && !await _facultyRepository.ExistsAsync(f => f.FacultyId == request.FacultyId!.Value))
                throw new NotFoundException("Faculty not found.");
            foreach (var (code, name, _, studentFacultyId) in newSubjects)
            {
                var subject = new Subject
                {
                    SubjectId = Guid.NewGuid(),
                    FacultyId = studentFacultyId ?? request.FacultyId!.Value,
                    SubjectCode = code,
                    SubjectName = string.IsNullOrWhiteSpace(name) ? code : Cut(name, 255),
                    Status = "active"
                };
                _subjectRepository.PrepareCreate(subject);
                subjects[code] = subject;
                response.CreatedSubjects.Add(code);
            }

            // Terms not in AdviseCare yet (e.g. Fall 2022): created with the usual season dates
            var terms = TermLookup(await _termRepository.GetAllAsync());
            var hasCurrentTerm = terms.Values.Any(t => t.Status == "current");
            foreach (var termName in allRows.Select(r => r.TermName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(TermStart))
            {
                if (terms.ContainsKey(TermKey(termName))) continue;
                var start = TermStart(termName);
                var end = TermEnd(termName);
                var status = end < now ? "past" : start > now ? "upcoming" : hasCurrentTerm ? "past" : "current";
                if (status == "current") hasCurrentTerm = true;
                var term = new Academicterm
                {
                    TermId = Guid.NewGuid(),
                    TermName = termName,
                    StartDate = start,
                    EndDate = end,
                    Status = status
                };
                _termRepository.PrepareCreate(term);
                terms[TermKey(termName)] = term;
                response.CreatedTerms.Add(termName);
            }

            // Upsert one row per student, subject and term; the grade file and the attendance file each fill their own part
            var studentIds = validFiles.Select(f => f.StudentId!.Value).Distinct().ToList();
            var results = (await _resultRepository.SearchAsync(r => studentIds.Contains(r.StudentId)))
                .ToDictionary(r => (r.StudentId, r.SubjectId, r.TermId));
            var createdIds = new HashSet<Guid>();
            var updatedIds = new HashSet<Guid>();
            foreach (var file in validFiles)
            {
                var studentId = file.StudentId!.Value;
                var isGrade = file.ImportType == "grade";
                foreach (var row in rowsByFile[file.ImportFileId])
                {
                    var subject = subjects[row.SubjectCode];
                    var term = terms[TermKey(row.TermName)];
                    var key = (studentId, subject.SubjectId, term.TermId);
                    if (!results.TryGetValue(key, out var result))
                    {
                        result = new Studentsubjectresult
                        {
                            StudentSubjectResultId = Guid.NewGuid(),
                            StudentId = studentId,
                            SubjectId = subject.SubjectId,
                            TermId = term.TermId
                        };
                        _resultRepository.PrepareCreate(result);
                        results[key] = result;
                        createdIds.Add(result.StudentSubjectResultId);
                    }
                    else if (!createdIds.Contains(result.StudentSubjectResultId))
                    {
                        updatedIds.Add(result.StudentSubjectResultId);
                    }

                    result.IsInCurriculum = row.IsInCurriculum;
                    if (row.CurriculumTerm.HasValue) result.CurriculumTerm = row.CurriculumTerm;
                    // The grade transcript decides the status; an attendance file only fills it while no grade file has
                    if (row.Status != null && (isGrade || result.GradeImportFileId == null)) result.Status = row.Status;
                    if (isGrade)
                    {
                        result.Grade = row.Value;
                        result.GradeImportFileId = file.ImportFileId;
                        result.GradeUpdatedAt = now;
                    }
                    else
                    {
                        result.AttendanceRate = row.Value;
                        result.AttendanceImportFileId = file.ImportFileId;
                        result.AttendanceUpdatedAt = now;
                    }
                }
                file.Status = "committed";
            }

            // The curriculum printed on the transcript goes on the student's profile
            var profiles = await _studentProfileRepository.SearchAsync(p => studentIds.Contains(p.StudentId));
            foreach (var file in validFiles.Where(f => !string.IsNullOrWhiteSpace(f.CurriculumCode)))
            {
                var profile = profiles.FirstOrDefault(p => p.StudentId == file.StudentId);
                if (profile != null) profile.CurriculumCode = file.CurriculumCode;
            }

            batch.Status = "committed";
            batch.CommittedAt = now;
            await _resultRepository.SaveAsync();

            response.CreatedResults = createdIds.Count;
            response.UpdatedResults = updatedIds.Count;
            var gradeFiles = validFiles.Count(f => f.ImportType == "grade");
            await _activityLogService.LogAsync(AdminId, "academicResultsImported",
                $"Imported {validFiles.Count} transcript file(s) ({gradeFiles} grade, {validFiles.Count - gradeFiles} attendance): " +
                $"{response.CreatedResults} subject result(s) created, {response.UpdatedResults} updated, " +
                $"{response.CreatedSubjects.Count} subject(s) and {response.CreatedTerms.Count} term(s) added; {response.SkippedFiles} invalid file(s) left out");
            return response;
        }

        // Drop a batch in preview; nothing was written to the results, and the parsed rows are cleared
        public async Task<TranscriptImportBatchResponse> CancelAsync(Guid AdminId, Guid ImportBatchId)
        {
            var batch = await _batchRepository.GetByIdAsync(ImportBatchId)
                ?? throw new NotFoundException("Import batch not found.");
            if (batch.Status != "previewed")
                throw new ConflictException($"This import batch is already {batch.Status}.");

            var files = await _fileRepository.SearchAsync(f => f.ImportBatchId == ImportBatchId);
            foreach (var file in files) file.ParsedRows = null;
            batch.Status = "cancelled";
            await _batchRepository.SaveAsync();
            await _activityLogService.LogAsync(AdminId, "academicResultsImportCancelled", $"Cancelled a transcript import of {files.Count} file(s)");
            return await GetBatchAsync(ImportBatchId);
        }

        // ================= Reading =================

        // The student's own results: everything, no consent needed, not logged
        public async Task<AcademicResultsResponse> GetMyResultsAsync(Guid StudentId, Guid? TermId)
        {
            var profile = await _studentProfileRepository.FirstOrDefaultAsync(p => p.StudentId == StudentId, p => p.Student)
                ?? throw new NotFoundException("Your student profile has not been set up yet.");
            var rows = await LoadResultsAsync(StudentId, TermId);
            return BuildResults(profile, rows, true, true);
        }

        // A student's results for staff: the grade part needs the "GPA" consent, the attendance part the "Attendance rate" consent
        public async Task<AcademicResultsResponse> GetStudentResultsAsync(Guid AccessorId, Guid StudentId, GetAcademicResultsRequest request)
        {
            var profile = await _studentProfileRepository.FirstOrDefaultAsync(p => p.StudentId == StudentId, p => p.Student)
                ?? throw new NotFoundException("Student not found.");

            Guid? consentPurposeId = null;
            if (request.CaseId.HasValue)
            {
                var caseRecord = await _caseRepository.GetByIdAsyncInclude(request.CaseId.Value, c => c.SupportCategory)
                    ?? throw new NotFoundException("Case not found.");
                if (caseRecord.StudentId != StudentId)
                    throw new NotFoundException("Case not found for this student.");
                if (caseRecord.AdvisorId != AccessorId)
                    throw new ForbiddenException("Only the advisor of this case may read the student's data through it.");
                consentPurposeId = caseRecord.SupportCategory.ConsentPurposeId;
            }

            var attributes = await _dataAttributeRepository.SearchAsync(a =>
                a.AttributeName == GradeAttributeName || a.AttributeName == AttendanceAttributeName);
            var gradeAttribute = attributes.FirstOrDefault(a => string.Equals(a.AttributeName, GradeAttributeName, StringComparison.OrdinalIgnoreCase));
            var attendanceAttribute = attributes.FirstOrDefault(a => string.Equals(a.AttributeName, AttendanceAttributeName, StringComparison.OrdinalIgnoreCase));
            var decisions = attributes.Count == 0
                ? new List<AccessDecision>()
                : await _accessGuardService.CheckAccessAsync(AccessorId, StudentId, attributes.Select(a => a.DataAttributeId).ToList(), consentPurposeId);
            AccessDecision? DecisionOf(Dataattribute? attribute) =>
                attribute == null ? null : decisions.FirstOrDefault(d => d.DataAttributeId == attribute.DataAttributeId && d.IsAllowed);
            var gradeDecision = DecisionOf(gradeAttribute);
            var attendanceDecision = DecisionOf(attendanceAttribute);

            var rows = gradeDecision != null || attendanceDecision != null
                ? await LoadResultsAsync(StudentId, request.TermId)
                : new List<Studentsubjectresult>();
            var response = BuildResults(profile, rows, gradeDecision != null, attendanceDecision != null);

            // One access log row per part shown (null purpose = emergency access)
            var shown = response.Terms.SelectMany(t => t.Results).ToList();
            var entries = new List<AccessLogEntry>();
            var gradeCount = shown.Count(r => r.Grade.HasValue || r.Status.Length > 0);
            if (gradeDecision != null && gradeCount > 0)
                entries.Add(new AccessLogEntry
                {
                    StudentId = StudentId,
                    ConsentPurposeId = gradeDecision.ConsentPurposeId,
                    AccessedResource = $"Academic results: grades of {gradeCount} subject(s)"
                });
            var attendanceCount = shown.Count(r => r.AttendanceRate.HasValue);
            if (attendanceDecision != null && attendanceCount > 0)
                entries.Add(new AccessLogEntry
                {
                    StudentId = StudentId,
                    ConsentPurposeId = attendanceDecision.ConsentPurposeId,
                    AccessedResource = $"Academic results: attendance of {attendanceCount} subject(s)"
                });
            if (entries.Count > 0)
                await _accessGuardService.LogAccessBatchAsync(AccessorId, entries);
            return response;
        }

        // ================= Helpers: upload =================

        private static List<IFormFile> CheckUpload(List<IFormFile>? files, int filesInBatch)
        {
            var uploads = (files ?? new List<IFormFile>()).Where(f => f != null).ToList();
            if (uploads.Count == 0)
                throw new ValidationException("At least one PDF file is required.");
            if (uploads.Count > MaxFilesPerUpload)
                throw new ValidationException($"Send at most {MaxFilesPerUpload} files at a time.");
            if (filesInBatch + uploads.Count > MaxFilesPerBatch)
                throw new ValidationException($"An import batch holds at most {MaxFilesPerBatch} files; commit this one and start another.");
            return uploads;
        }

        // Read and check each uploaded file and stage it as a valid or invalid file of the batch
        private async Task ReadFilesAsync(Guid ImportBatchId, List<Transcriptimportfile> existingFiles, List<IFormFile> uploads)
        {
            var read = new List<(string FileName, string Hash, ParsedTranscript Parsed, List<string> Errors)>();
            foreach (var upload in uploads)
            {
                var fileName = Cut(Path.GetFileName(upload.FileName ?? string.Empty), 255);
                if (string.IsNullOrWhiteSpace(fileName)) fileName = "transcript.pdf";
                var errors = new List<string>();
                var content = Array.Empty<byte>();
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Only PDF files can be imported.");
                else if (upload.Length == 0)
                    errors.Add("The file is empty.");
                else if (upload.Length > MaxFileBytes)
                    errors.Add("The file is larger than 5 MB.");
                else
                {
                    using var stream = new MemoryStream();
                    await upload.CopyToAsync(stream);
                    content = stream.ToArray();
                }

                var parsed = content.Length > 0 ? _pdfReader.Read(content) : new ParsedTranscript();
                errors.AddRange(parsed.Errors);
                read.Add((fileName, Convert.ToHexString(SHA256.HashData(content)), parsed, errors));
            }

            // Everything the checks need, loaded once for all files
            var codes = read.Select(r => r.Parsed.StudentCode).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var profiles = codes.Count == 0
                ? new List<Studentprofile>()
                : await _studentProfileRepository.SearchAsyncInclude(p => codes.Contains(p.StudentCode), p => p.Student);
            var profileByCode = profiles
                .GroupBy(p => p.StudentCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var subjects = SubjectLookup(await _subjectRepository.GetAllAsync());
            var terms = TermLookup(await _termRepository.GetAllAsync());
            var studentIds = profiles.Select(p => p.StudentId).ToList();
            var existingResults = new HashSet<(Guid StudentId, Guid SubjectId, Guid TermId)>();
            if (studentIds.Count > 0)
            {
                var keys = _resultRepository.GetQueryable()
                    .Where(r => studentIds.Contains(r.StudentId))
                    .Select(r => new { r.StudentId, r.SubjectId, r.TermId })
                    .ToList();
                foreach (var key in keys)
                    existingResults.Add((key.StudentId, key.SubjectId, key.TermId));
            }
            var hashes = read.Select(r => r.Hash).Distinct().ToList();
            var committedHashes = _fileRepository.GetQueryable()
                .Where(f => hashes.Contains(f.FileHash) && f.Status == "committed")
                .Select(f => new { f.FileHash, f.UploadedAt })
                .ToList()
                .GroupBy(f => f.FileHash)
                .ToDictionary(g => g.Key, g => g.Max(f => f.UploadedAt));

            // One file per student and type in a batch, and each file once
            var batchHashes = existingFiles.Select(f => f.FileHash).ToHashSet();
            var takenStudentTypes = new Dictionary<(Guid, string), string>();
            foreach (var file in existingFiles.Where(f => f.Status == "valid" && f.StudentId.HasValue && f.ImportType != null))
                takenStudentTypes.TryAdd((file.StudentId!.Value, file.ImportType), file.FileName);

            var uploadedAt = DateTime.UtcNow;
            foreach (var (fileName, hash, parsed, uploadErrors) in read)
            {
                var check = new TranscriptFileCheck();
                check.Errors.AddRange(uploadErrors);
                var file = new Transcriptimportfile
                {
                    ImportFileId = Guid.NewGuid(),
                    ImportBatchId = ImportBatchId,
                    FileName = fileName,
                    FileHash = hash,
                    ImportType = parsed.ImportType.Length > 0 ? parsed.ImportType : null,
                    StudentCodeInFile = NullIfEmpty(Cut(parsed.StudentCode, 50)),
                    FullNameInFile = NullIfEmpty(Cut(parsed.FullName, 255)),
                    CurriculumCode = NullIfEmpty(Cut(parsed.CurriculumCode, 50)),
                    UploadedAt = uploadedAt
                };

                if (check.Errors.Count == 0)
                {
                    if (parsed.StudentCode.Length == 0)
                        check.Errors.Add("No \"Student ID:\" was found at the top of the file.");
                    else if (!profileByCode.TryGetValue(parsed.StudentCode, out var profile))
                        check.Errors.Add($"Student {parsed.StudentCode} has no student profile in AdviseCare.");
                    else
                    {
                        file.StudentId = profile.StudentId;
                        if (parsed.FullName.Length > 0 && !SameName(parsed.FullName, profile.Student?.FullName ?? string.Empty))
                            check.Warnings.Add($"The name in the file ({parsed.FullName}) is not the account's name ({profile.Student?.FullName}).");
                    }
                }

                if (batchHashes.Contains(hash))
                    check.Errors.Add("The same file is already in this batch.");
                else if (committedHashes.TryGetValue(hash, out var importedAt))
                    check.Warnings.Add($"This exact file was already imported on {importedAt:yyyy-MM-dd}; importing it again changes nothing.");
                batchHashes.Add(hash);

                var rows = check.Errors.Count == 0 ? BuildRows(parsed, check) : new List<TranscriptStoredRow>();

                if (check.Errors.Count == 0 && file.StudentId.HasValue && file.ImportType != null)
                {
                    var studentType = (file.StudentId.Value, file.ImportType);
                    if (takenStudentTypes.TryGetValue(studentType, out var otherFile))
                        check.Errors.Add($"{otherFile} in this batch already has the {file.ImportType} transcript of {parsed.StudentCode}.");
                    else
                        takenStudentTypes[studentType] = fileName;
                }

                if (check.Errors.Count == 0)
                {
                    foreach (var row in rows)
                    {
                        var exists = subjects.TryGetValue(row.SubjectCode, out var subject)
                            && terms.TryGetValue(TermKey(row.TermName), out var term)
                            && existingResults.Contains((file.StudentId!.Value, subject.SubjectId, term.TermId));
                        if (exists) check.UpdateCount++;
                        else check.CreateCount++;
                    }
                }

                file.RowCount = rows.Count;
                file.Status = check.Errors.Count == 0 ? "valid" : "invalid";
                file.ParsedRows = check.Errors.Count == 0 ? JsonSerializer.Serialize(rows, JsonOptions) : null;
                file.ValidationResult = JsonSerializer.Serialize(check, JsonOptions);
                _fileRepository.PrepareCreate(file);
            }
        }

        // Turn the PDF's text rows into checked rows; a row without a semester (not started yet) is left out with a warning
        private static List<TranscriptStoredRow> BuildRows(ParsedTranscript parsed, TranscriptFileCheck check)
        {
            var rows = new List<TranscriptStoredRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var isGrade = parsed.ImportType == "grade";
            foreach (var row in parsed.Rows)
            {
                var code = row.SubjectCode.Trim();
                var label = $"Row {row.RowNumber}" + (code.Length > 0 && code != "-" ? $" ({code})" : string.Empty);

                var semester = row.Semester.Trim();
                if (semester.Length == 0 || semester == "-")
                {
                    check.SkippedRowCount++;
                    continue;
                }
                var termName = TermNameOf(semester);
                if (termName == null)
                {
                    check.Errors.Add($"{label}: semester \"{semester}\" is not like Fall2024.");
                    continue;
                }
                if (code.Length == 0 || code == "-")
                {
                    check.Errors.Add($"{label}: the subject code is empty.");
                    continue;
                }
                if (code.Length > 50)
                {
                    check.Errors.Add($"{label}: the subject code is longer than 50 characters.");
                    continue;
                }

                decimal? value = null;
                var valueText = row.Value.Trim();
                if (valueText.Length > 0 && valueText != "-")
                {
                    var number = ParseNumber(valueText.TrimEnd('%').Trim());
                    if (!number.HasValue)
                    {
                        check.Errors.Add($"{label}: \"{valueText}\" is not a number.");
                        continue;
                    }
                    if (number < 0 || number > (isGrade ? 10m : 100m))
                    {
                        check.Errors.Add(isGrade
                            ? $"{label}: grade {valueText} is not between 0 and 10."
                            : $"{label}: attendance {valueText} is not between 0 and 100.");
                        continue;
                    }
                    value = Math.Round(number.Value, 2);
                }

                string? status = null;
                var statusText = row.Status.Trim();
                if (statusText.Length > 0 && statusText != "-")
                {
                    if (!Statuses.TryGetValue(LettersOnly(statusText), out var mapped))
                    {
                        check.Errors.Add($"{label}: status \"{statusText}\" is not one of Passed, Not passed, Attendance fail, Exempt, Studying, Not started.");
                        continue;
                    }
                    status = mapped;
                }

                byte? curriculumTerm = null;
                var termText = row.CurriculumTerm.Trim();
                if (termText.Length > 0 && termText != "-")
                {
                    if (!byte.TryParse(termText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedTerm) || parsedTerm > 20)
                    {
                        check.Errors.Add($"{label}: term \"{termText}\" is not a whole number from 0 to 20.");
                        continue;
                    }
                    curriculumTerm = parsedTerm;
                }

                if (!seen.Add($"{code}|{termName}"))
                {
                    check.Errors.Add($"{label}: {code} in {termName} appears more than once.");
                    continue;
                }

                rows.Add(new TranscriptStoredRow
                {
                    RowNumber = row.RowNumber,
                    IsInCurriculum = row.IsInCurriculum,
                    CurriculumTerm = curriculumTerm,
                    TermName = termName,
                    SubjectCode = code,
                    SubjectName = Cut(row.SubjectName.Trim(), 255),
                    Value = value,
                    Status = status
                });
            }

            if (check.SkippedRowCount > 0)
                check.Warnings.Add($"{check.SkippedRowCount} row(s) have no semester yet (not started) and are left out.");
            if (rows.Count == 0 && check.Errors.Count == 0)
                check.Errors.Add("No subject row with a semester was found in the file.");
            return rows;
        }

        // Subjects the files use that are not in AdviseCare yet, with the faculty of a student who has them (null when none has one);
        // the files must come with their Student loaded
        private static List<(string Code, string Name, int FileCount, Guid? FacultyId)> NewSubjectsOf(
            List<Transcriptimportfile> files, Dictionary<string, Subject> subjects)
        {
            return files
                .SelectMany(f => ReadRows(f.ParsedRows).Select(row => (File: f, Row: row)))
                .Where(x => !subjects.ContainsKey(x.Row.SubjectCode))
                .GroupBy(x => x.Row.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => (
                    Code: g.First().Row.SubjectCode,
                    Name: g.Select(x => x.Row.SubjectName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                    FileCount: g.Select(x => x.File.ImportFileId).Distinct().Count(),
                    FacultyId: g.Select(x => x.File.Student?.FacultyId).FirstOrDefault(id => id.HasValue)))
                .OrderBy(s => s.Code)
                .ToList();
        }

        private static TranscriptImportFileResponse ToFileResponse(Transcriptimportfile file)
        {
            var check = ReadCheck(file.ValidationResult);
            return new TranscriptImportFileResponse
            {
                ImportFileId = file.ImportFileId,
                FileName = file.FileName,
                ImportType = file.ImportType ?? string.Empty,
                StudentId = file.StudentId,
                StudentCode = file.StudentCodeInFile ?? string.Empty,
                StudentName = file.Student?.FullName ?? string.Empty,
                FullNameInFile = file.FullNameInFile ?? string.Empty,
                CurriculumCode = file.CurriculumCode ?? string.Empty,
                RowCount = file.RowCount,
                CreateCount = check.CreateCount,
                UpdateCount = check.UpdateCount,
                SkippedRowCount = check.SkippedRowCount,
                Status = file.Status,
                UploadedAt = file.UploadedAt,
                Errors = check.Errors,
                Warnings = check.Warnings
            };
        }

        // ================= Helpers: reading =================

        private async Task<List<Studentsubjectresult>> LoadResultsAsync(Guid StudentId, Guid? TermId)
        {
            return await _resultRepository.SearchAsyncInclude(
                r => r.StudentId == StudentId && (TermId == null || r.TermId == TermId),
                r => r.Subject,
                r => r.Term);
        }

        // Group by term (newest first) and hide the part that is not shared
        private static AcademicResultsResponse BuildResults(Studentprofile profile, List<Studentsubjectresult> rows, bool gradeShared, bool attendanceShared)
        {
            var items = rows.Select(r => (Term: r.Term, Item: new SubjectResultResponse
            {
                StudentSubjectResultId = r.StudentSubjectResultId,
                SubjectId = r.SubjectId,
                SubjectCode = r.Subject?.SubjectCode ?? string.Empty,
                SubjectName = r.Subject?.SubjectName ?? string.Empty,
                CurriculumTerm = r.CurriculumTerm,
                IsInCurriculum = r.IsInCurriculum,
                Grade = gradeShared ? r.Grade : null,
                AttendanceRate = attendanceShared ? r.AttendanceRate : null,
                AbsenceRate = attendanceShared && r.AttendanceRate.HasValue ? 100m - r.AttendanceRate.Value : null,
                Status = gradeShared ? r.Status ?? string.Empty : string.Empty,
                GradeUpdatedAt = gradeShared ? r.GradeUpdatedAt : null,
                AttendanceUpdatedAt = attendanceShared ? r.AttendanceUpdatedAt : null
            }))
            // A row that has nothing of the shared part shows nothing
            .Where(x => (gradeShared && (x.Item.Grade.HasValue || x.Item.Status.Length > 0 || x.Item.GradeUpdatedAt.HasValue))
                || (attendanceShared && x.Item.AttendanceRate.HasValue))
            .ToList();

            var terms = items
                .Where(x => x.Term != null)
                .GroupBy(x => x.Term.TermId)
                .Select(g => new AcademicTermResultsResponse
                {
                    TermId = g.Key,
                    TermName = g.First().Term.TermName,
                    StartDate = g.First().Term.StartDate,
                    Results = g.Select(x => x.Item)
                        .OrderBy(i => i.IsInCurriculum ? 0 : 1)
                        .ThenBy(i => i.CurriculumTerm ?? 99)
                        .ThenBy(i => i.SubjectCode)
                        .ToList()
                })
                .OrderByDescending(t => t.StartDate)
                .ToList();
            foreach (var term in terms) term.Summary = Summarize(term.Results);

            var shown = terms.SelectMany(t => t.Results).ToList();
            var updates = shown.Select(i => i.GradeUpdatedAt).Concat(shown.Select(i => i.AttendanceUpdatedAt)).Where(d => d.HasValue).ToList();
            return new AcademicResultsResponse
            {
                StudentId = profile.StudentId,
                StudentCode = profile.StudentCode ?? string.Empty,
                FullName = profile.Student?.FullName ?? string.Empty,
                CurriculumCode = profile.CurriculumCode ?? string.Empty,
                IsGradeShared = gradeShared,
                IsAttendanceShared = attendanceShared,
                MinAttendanceRate = MinAttendanceRate,
                PassingGrade = PassingGrade,
                LastUpdatedAt = updates.Count > 0 ? updates.Max() : null,
                Summary = Summarize(shown),
                Terms = terms
            };
        }

        private static AcademicResultSummaryResponse Summarize(List<SubjectResultResponse> items)
        {
            var graded = items.Where(i => i.Grade.HasValue).ToList();
            var attended = items.Where(i => i.AttendanceRate.HasValue).ToList();
            return new AcademicResultSummaryResponse
            {
                SubjectCount = items.Count,
                PassedCount = items.Count(i => i.Status == "passed"),
                GradedCount = graded.Count,
                AverageGrade = graded.Count > 0 ? Math.Round(graded.Average(i => i.Grade!.Value), 2) : null,
                LowGradeCount = graded.Count(i => i.Grade < PassingGrade),
                AttendanceCount = attended.Count,
                AverageAttendanceRate = attended.Count > 0 ? Math.Round(attended.Average(i => i.AttendanceRate!.Value), 2) : null,
                LowAttendanceCount = attended.Count(i => i.AttendanceRate < MinAttendanceRate)
            };
        }

        // ================= Helpers: values =================

        private static Dictionary<string, Subject> SubjectLookup(List<Subject> subjects) =>
            subjects
                .GroupBy(s => s.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, Academicterm> TermLookup(List<Academicterm> terms) =>
            terms
                .GroupBy(t => TermKey(t.TermName))
                .ToDictionary(g => g.Key, g => g.First());

        // "Fall 2026", "Fall2026" and "fall 2026" are the same term
        private static string TermKey(string termName) => LettersAndDigits(termName);

        // "Summer2026" -> "Summer 2026" (the spelling of the ACADEMICTERMS rows); null when it is not a season and a year
        private static string? TermNameOf(string semester)
        {
            var match = SemesterPattern.Match(semester.Trim());
            if (!match.Success) return null;
            var year = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            if (year < 1990 || year > 2100) return null;
            var season = match.Groups[1].Value.ToLowerInvariant();
            return $"{char.ToUpperInvariant(season[0])}{season[1..]} {year}";
        }

        // Season dates used for a term created by the import: Spring January-April, Summer May-August, Fall September-December
        private static DateTime TermStart(string termName)
        {
            var (season, year) = SeasonOf(termName);
            return new DateTime(year, season == "spring" ? 1 : season == "summer" ? 5 : 9, 1);
        }

        private static DateTime TermEnd(string termName)
        {
            var (season, year) = SeasonOf(termName);
            var month = season == "spring" ? 4 : season == "summer" ? 8 : 12;
            return new DateTime(year, month, DateTime.DaysInMonth(year, month));
        }

        private static (string Season, int Year) SeasonOf(string termName)
        {
            var match = SemesterPattern.Match(termName.Trim());
            return match.Success
                ? (match.Groups[1].Value.ToLowerInvariant(), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture))
                : ("fall", 2000);
        }

        // "8.3", "8,3", "85%" -> number; no thousands separators, so "8,3" is never read as 83
        private static decimal? ParseNumber(string text) =>
            decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : null;

        private static bool SameName(string a, string b) =>
            string.Equals(NormalizeName(a), NormalizeName(b), StringComparison.OrdinalIgnoreCase);

        private static string NormalizeName(string name) =>
            Regex.Replace(name.Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");

        private static string LettersOnly(string text) =>
            new string(text.ToLowerInvariant().Where(char.IsLetter).ToArray());

        private static string LettersAndDigits(string text) =>
            new string((text ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string Cut(string text, int length) =>
            text.Length <= length ? text : text[..length];

        private static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

        private static List<TranscriptStoredRow> ReadRows(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<TranscriptStoredRow>();
            try
            {
                return JsonSerializer.Deserialize<List<TranscriptStoredRow>>(json, JsonOptions) ?? new List<TranscriptStoredRow>();
            }
            catch (JsonException)
            {
                return new List<TranscriptStoredRow>();
            }
        }

        private static TranscriptFileCheck ReadCheck(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new TranscriptFileCheck();
            try
            {
                return JsonSerializer.Deserialize<TranscriptFileCheck>(json, JsonOptions) ?? new TranscriptFileCheck();
            }
            catch (JsonException)
            {
                return new TranscriptFileCheck();
            }
        }
    }

    // One checked row of a transcript file, kept as JSON in TRANSCRIPTIMPORTFILES.parsedRows between preview and commit
    public sealed class TranscriptStoredRow
    {
        public int RowNumber { get; set; }
        public bool IsInCurriculum { get; set; } = true;
        public byte? CurriculumTerm { get; set; }
        // As in ACADEMICTERMS, e.g. "Fall 2024"
        public string TermName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        // Grade (0-10) or attendance rate (0-100), by the file's type
        public decimal? Value { get; set; }
        public string? Status { get; set; }
    }

    // What the check of one file found, kept as JSON in TRANSCRIPTIMPORTFILES.validationResult
    public sealed class TranscriptFileCheck
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int SkippedRowCount { get; set; }
    }
}
