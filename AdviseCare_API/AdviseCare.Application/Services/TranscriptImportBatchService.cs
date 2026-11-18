using AdviseCare.Application.Contracts.Requests.TranscriptImportBatch;
using AdviseCare.Application.Contracts.Responses.TranscriptImportBatch;
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
    // Transcript import batches (TRANSCRIPTIMPORTBATCHES / TRANSCRIPTIMPORTFILES): the admin uploads transcript PDFs (a grade transcript or an
    // attendance file, one student per file) into a batch, in as many requests as needed. Every file is read and checked on upload and shown in
    // the preview; invalid files are removed (or the batch cancelled). The batch is imported all-or-nothing: only when every file is valid, all
    // of them are written to STUDENTDATA in one save, as values of the per-subject data attributes "Subject mark" (grade file) and
    // "Attendance rate" (attendance file), one row per student, subject and term (insert-only, like the Excel/CSV import)
    public class TranscriptImportBatchService : ITranscriptImportBatchService
    {
        private const string DataValueField = "dataValue";
        private const string GradeAttributeName = "Subject mark";
        private const string AttendanceAttributeName = "Attendance rate";
        private const int MaxFilesPerUpload = 50;
        private const int MaxFilesPerBatch = 500;
        private const long MaxFileBytes = 5 * 1024 * 1024;

        private static readonly Regex SemesterPattern = new(@"^(spring|summer|fall)\s*(\d{4})$", RegexOptions.IgnoreCase);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IGenericRepository<Transcriptimportbatch> _transcriptImportBatchRepository;
        private readonly IGenericRepository<Transcriptimportfile> _transcriptImportFileRepository;
        private readonly IGenericRepository<Studentdata> _studentDataRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IGenericRepository<Academicterm> _academicTermRepository;
        private readonly IGenericRepository<Subject> _subjectRepository;
        private readonly ITranscriptPdfReader _transcriptPdfReader;
        private readonly IAttributeKeyService _attributeKeyService;
        private readonly IActivityLogService _activityLogService;

        public TranscriptImportBatchService(
            IGenericRepository<Transcriptimportbatch> transcriptImportBatchRepository,
            IGenericRepository<Transcriptimportfile> transcriptImportFileRepository,
            IGenericRepository<Studentdata> studentDataRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IGenericRepository<Academicterm> academicTermRepository,
            IGenericRepository<Subject> subjectRepository,
            ITranscriptPdfReader transcriptPdfReader,
            IAttributeKeyService attributeKeyService,
            IActivityLogService activityLogService)
        {
            _transcriptImportBatchRepository = transcriptImportBatchRepository;
            _transcriptImportFileRepository = transcriptImportFileRepository;
            _studentDataRepository = studentDataRepository;
            _studentProfileRepository = studentProfileRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _academicTermRepository = academicTermRepository;
            _subjectRepository = subjectRepository;
            _transcriptPdfReader = transcriptPdfReader;
            _attributeKeyService = attributeKeyService;
            _activityLogService = activityLogService;
        }

        // Get all Transcript import batches, newest first (file counts only)
        public async Task<IEnumerable<GetAllTranscriptImportBatchResponse>> GetAll()
        {
            var batches = await GetBatchesAsync(b => true);

            return batches.Select(b => new GetAllTranscriptImportBatchResponse
            {
                TranscriptImportBatchId = b.TranscriptImportBatchId,
                ImportedBy = b.ImportedBy,
                ImportedByName = b.ImportedByNavigation?.FullName ?? string.Empty,
                CreatedAt = b.CreatedAt,
                CommittedAt = b.CommittedAt,
                TotalFiles = b.Transcriptimportfiles.Count,
                ValidFiles = b.Transcriptimportfiles.Count(f => f.Status == "valid"),
                InvalidFiles = b.Transcriptimportfiles.Count(f => f.Status == "invalid"),
                CommittedFiles = b.Transcriptimportfiles.Count(f => f.Status == "committed"),
                Status = b.Status
            });
        }

        // Get all Transcript import batches with pagination, newest first
        public async Task<IEnumerable<GetAllTranscriptImportBatchWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var batches = await GetBatchesAsync(b => true);

            return batches
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new GetAllTranscriptImportBatchWithPaginationResponse
                {
                    TranscriptImportBatchId = b.TranscriptImportBatchId,
                    ImportedBy = b.ImportedBy,
                    ImportedByName = b.ImportedByNavigation?.FullName ?? string.Empty,
                    CreatedAt = b.CreatedAt,
                    CommittedAt = b.CommittedAt,
                    TotalFiles = b.Transcriptimportfiles.Count,
                    ValidFiles = b.Transcriptimportfiles.Count(f => f.Status == "valid"),
                    InvalidFiles = b.Transcriptimportfiles.Count(f => f.Status == "invalid"),
                    CommittedFiles = b.Transcriptimportfiles.Count(f => f.Status == "committed"),
                    Status = b.Status
                });
        }

        // Get Transcript import batch by Id, with every file's check result
        public async Task<GetTranscriptImportBatchByIdResponse> GetByIdAsync(Guid TranscriptImportBatchId)
        {
            var batch = await _transcriptImportBatchRepository.GetByIdAsyncInclude(TranscriptImportBatchId, b => b.ImportedByNavigation)
                ?? throw new NotFoundException("Transcript import batch not found.");
            var files = (await _transcriptImportFileRepository.SearchAsyncInclude(f => f.TranscriptImportBatchId == TranscriptImportBatchId, f => f.Student))
                .OrderBy(f => f.UploadedAt)
                .ThenBy(f => f.FileName)
                .ToList();
            var fileResponses = files.Select(f =>
            {
                var check = ReadCheck(f.ValidationResult);
                return new TranscriptImportFileResponse
                {
                    TranscriptImportFileId = f.TranscriptImportFileId,
                    StudentId = f.StudentId,
                    CurriculumCode = f.CurriculumCode ?? string.Empty,
                    FileName = f.FileName,
                    FullNameInFile = f.FullNameInFile ?? string.Empty,
                    StudentCode = f.StudentCodeInFile ?? string.Empty,
                    StudentName = f.Student?.FullName ?? string.Empty,
                    RowCount = f.RowCount,
                    InsertCount = check.InsertCount,
                    ReplaceCount = check.ReplaceCount,
                    SkippedRowCount = check.SkippedRowCount,
                    UploadedAt = f.UploadedAt,
                    Errors = check.Errors,
                    Warnings = check.Warnings,
                    ImportType = f.ImportType ?? string.Empty,
                    Status = f.Status
                };
            }).ToList();

            return new GetTranscriptImportBatchByIdResponse
            {
                TranscriptImportBatchId = batch.TranscriptImportBatchId,
                ImportedBy = batch.ImportedBy,
                ImportedByName = batch.ImportedByNavigation?.FullName ?? string.Empty,
                CreatedAt = batch.CreatedAt,
                CommittedAt = batch.CommittedAt,
                TotalFiles = files.Count,
                ValidFiles = files.Count(f => f.Status == "valid"),
                InvalidFiles = files.Count(f => f.Status == "invalid"),
                CommittedFiles = files.Count(f => f.Status == "committed"),
                InsertCount = fileResponses.Where(f => f.Status != "invalid").Sum(f => f.InsertCount),
                ReplaceCount = fileResponses.Where(f => f.Status != "invalid").Sum(f => f.ReplaceCount),
                Files = fileResponses,
                Status = batch.Status
            };
        }

        // Search Transcript import batches by the admin who started them or by status
        public async Task<List<SearchTranscriptImportBatchResponse>> SearchAsync(SearchTranscriptImportBatchRequest request)
        {
            var status = request.Status.Trim();
            var batches = await GetBatchesAsync(b =>
                (!request.ImportedBy.HasValue || b.ImportedBy == request.ImportedBy.Value) &&
                (status == string.Empty || b.Status == status));

            return batches.Select(b => new SearchTranscriptImportBatchResponse
            {
                TranscriptImportBatchId = b.TranscriptImportBatchId,
                ImportedBy = b.ImportedBy,
                ImportedByName = b.ImportedByNavigation?.FullName ?? string.Empty,
                CreatedAt = b.CreatedAt,
                CommittedAt = b.CommittedAt,
                TotalFiles = b.Transcriptimportfiles.Count,
                ValidFiles = b.Transcriptimportfiles.Count(f => f.Status == "valid"),
                InvalidFiles = b.Transcriptimportfiles.Count(f => f.Status == "invalid"),
                CommittedFiles = b.Transcriptimportfiles.Count(f => f.Status == "committed"),
                Status = b.Status
            }).ToList();
        }

        // Create Transcript import batch with its first files: every file is read and checked, nothing is written to STUDENTDATA yet
        public async Task<GetTranscriptImportBatchByIdResponse> CreateAsync(Guid CreatorId, CreateTranscriptImportBatchRequest request)
        {
            var uploads = CheckUpload(request.Files, 0);
            var batch = new Transcriptimportbatch
            {
                TranscriptImportBatchId = Guid.NewGuid(),
                ImportedBy = CreatorId,
                CreatedAt = DateTime.UtcNow,
                Status = "previewed"
            };
            _transcriptImportBatchRepository.PrepareCreate(batch);
            await ReadFilesAsync(batch.TranscriptImportBatchId, new List<Transcriptimportfile>(), uploads);
            await _transcriptImportBatchRepository.SaveAsync();

            return await GetByIdAsync(batch.TranscriptImportBatchId);
        }

        // Add more files to a Transcript import batch that is still in preview (the web app sends a large batch in parts)
        public async Task<GetTranscriptImportBatchByIdResponse> AddFilesAsync(Guid TranscriptImportBatchId, AddTranscriptImportFilesRequest request)
        {
            var batch = await GetPreviewedBatchAsync(TranscriptImportBatchId);
            var existingFiles = await _transcriptImportFileRepository.SearchAsync(f => f.TranscriptImportBatchId == batch.TranscriptImportBatchId);
            var uploads = CheckUpload(request.Files, existingFiles.Count);
            await ReadFilesAsync(batch.TranscriptImportBatchId, existingFiles, uploads);
            await _transcriptImportFileRepository.SaveAsync();

            return await GetByIdAsync(batch.TranscriptImportBatchId);
        }

        // Delete a Transcript import file from a batch in preview (an invalid file, or one uploaded by mistake)
        public async Task<GetTranscriptImportBatchByIdResponse> DeleteFileAsync(Guid TranscriptImportBatchId, Guid TranscriptImportFileId)
        {
            var batch = await GetPreviewedBatchAsync(TranscriptImportBatchId);
            var file = await _transcriptImportFileRepository.FirstOrDefaultAsync(f =>
                    f.TranscriptImportFileId == TranscriptImportFileId && f.TranscriptImportBatchId == batch.TranscriptImportBatchId)
                ?? throw new NotFoundException("Transcript import file not found in this batch.");
            await _transcriptImportFileRepository.RemoveAsync(file);

            return await GetByIdAsync(batch.TranscriptImportBatchId);
        }

        // Import a Transcript import batch: only when every file is valid, all of them are written to STUDENTDATA in one save
        public async Task<CommitTranscriptImportBatchResponse> CommitAsync(Guid AdminId, Guid TranscriptImportBatchId)
        {
            var batch = await GetPreviewedBatchAsync(TranscriptImportBatchId);
            var files = (await _transcriptImportFileRepository.SearchAsync(f => f.TranscriptImportBatchId == batch.TranscriptImportBatchId))
                .OrderBy(f => f.UploadedAt)
                .ToList();
            if (files.Count == 0)
                throw new ValidationException("The batch has no file to import.");
            var invalidCount = files.Count(f => f.Status != "valid");
            if (invalidCount > 0)
                throw new ValidationException($"The batch has {invalidCount} invalid file(s); remove them (or upload fixed files) before importing.");

            var attributes = await GetTranscriptAttributesAsync();
            var subjects = SubjectLookup(await _subjectRepository.GetAllAsync());
            var terms = TermLookup(await _academicTermRepository.GetAllAsync());
            var currentKeys = await GetCurrentValueKeysAsync(files.Select(f => f.StudentId!.Value).Distinct().ToList(), attributes);

            // One STUDENTDATA row per checked row; subjects and terms are matched again, since they could have changed since the preview
            var transactionAt = DateTime.UtcNow;
            var newRows = new List<(Studentdata Row, string Value)>();
            var replacedCount = 0;
            foreach (var file in files)
            {
                var dataAttribute = file.ImportType == "grade" ? attributes.Grade : attributes.Attendance;
                foreach (var row in ReadRows(file.ParsedRows))
                {
                    if (!subjects.TryGetValue(row.SubjectCode, out var subject))
                        throw new ValidationException($"{file.FileName}: subject {row.SubjectCode} is no longer in AdviseCare; upload the file again.");
                    if (!terms.TryGetValue(TermKey(row.TermName), out var term))
                        throw new ValidationException($"{file.FileName}: term {row.TermName} is no longer in AdviseCare; upload the file again.");

                    if (currentKeys.Contains((file.StudentId!.Value, dataAttribute.DataAttributeId, term.TermId, subject.SubjectId)))
                        replacedCount++;
                    newRows.Add((new Studentdata
                    {
                        StudentDataId = Guid.NewGuid(),
                        DataSourceId = null,
                        DataAttributeId = dataAttribute.DataAttributeId,
                        StudentId = file.StudentId!.Value,
                        SubjectId = subject.SubjectId,
                        TermId = term.TermId,
                        ProvenanceNote = $"Imported from transcript {file.FileName} on {transactionAt:yyyy-MM-dd}.",
                        TransactionFrom = transactionAt,
                        ValidFrom = ValidFromOf(term, transactionAt),
                        IsMissing = false,
                        Status = "original"
                    }, row.Value));
                }
            }

            // Encrypt every value with the student's key for its attribute (new keys are staged and saved below with the rows)
            var encrypted = await _attributeKeyService.EncryptValuesAsync(newRows.Select(r => new PlainAttributeValue
            {
                StudentId = r.Row.StudentId,
                DataAttributeId = r.Row.DataAttributeId,
                RowId = r.Row.StudentDataId,
                FieldName = DataValueField,
                Value = r.Value
            }).ToList());
            for (var index = 0; index < newRows.Count; index++)
            {
                newRows[index].Row.AttributeKeyId = encrypted[index].AttributeKeyId;
                newRows[index].Row.DataValue = encrypted[index].Payload;
                _studentDataRepository.PrepareCreate(newRows[index].Row);
            }

            foreach (var file in files)
            {
                file.Status = "committed";
                _transcriptImportFileRepository.PrepareUpdate(file);
            }
            batch.Status = "committed";
            batch.CommittedAt = transactionAt;
            _transcriptImportBatchRepository.PrepareUpdate(batch);
            await _studentDataRepository.SaveAsync();

            var gradeFiles = files.Count(f => f.ImportType == "grade");
            await _activityLogService.LogAsync(AdminId, "transcriptImportCommitted",
                $"Imported {files.Count} transcript file(s) ({gradeFiles} grade, {files.Count - gradeFiles} attendance): {newRows.Count} value(s), {replacedCount} replacing a current value");

            return new CommitTranscriptImportBatchResponse
            {
                TranscriptImportBatchId = batch.TranscriptImportBatchId,
                CommittedFiles = files.Count,
                InsertedCount = newRows.Count,
                ReplacedCount = replacedCount
            };
        }

        // Cancel a Transcript import batch in preview: nothing was written to STUDENTDATA, and the rows read from the files are cleared
        public async Task<GetTranscriptImportBatchByIdResponse> CancelAsync(Guid AdminId, Guid TranscriptImportBatchId)
        {
            var batch = await GetPreviewedBatchAsync(TranscriptImportBatchId);
            var files = await _transcriptImportFileRepository.SearchAsync(f => f.TranscriptImportBatchId == batch.TranscriptImportBatchId);
            foreach (var file in files)
            {
                file.ParsedRows = null;
                _transcriptImportFileRepository.PrepareUpdate(file);
            }
            batch.Status = "cancelled";
            _transcriptImportBatchRepository.PrepareUpdate(batch);
            await _transcriptImportBatchRepository.SaveAsync();
            await _activityLogService.LogAsync(AdminId, "transcriptImportCancelled", $"Cancelled a transcript import batch of {files.Count} file(s)");

            return await GetByIdAsync(batch.TranscriptImportBatchId);
        }

        // Helper: load Transcript import batches with the admin and the files, newest first
        private async Task<List<Transcriptimportbatch>> GetBatchesAsync(Expression<Func<Transcriptimportbatch, bool>> predicate)
        {
            var batches = await _transcriptImportBatchRepository.SearchAsyncInclude(predicate, b => b.ImportedByNavigation, b => b.Transcriptimportfiles);
            return batches.OrderByDescending(b => b.CreatedAt).ToList();
        }

        // Helper: a batch that can still be changed (files added or removed, imported or cancelled)
        private async Task<Transcriptimportbatch> GetPreviewedBatchAsync(Guid TranscriptImportBatchId)
        {
            var batch = await _transcriptImportBatchRepository.GetByIdAsync(TranscriptImportBatchId)
                ?? throw new NotFoundException("Transcript import batch not found.");
            if (batch.Status != "previewed")
                throw new ConflictException($"This transcript import batch is already {batch.Status}; start a new one.");
            return batch;
        }

        // Helper: the two per-subject data attributes the transcripts fill ("Subject mark" from grade files, "Attendance rate" from attendance files)
        private async Task<(Dataattribute Grade, Dataattribute Attendance)> GetTranscriptAttributesAsync()
        {
            var attributes = await _dataAttributeRepository.SearchAsync(a => a.AttributeName == GradeAttributeName || a.AttributeName == AttendanceAttributeName);
            Dataattribute Find(string name)
            {
                var attribute = attributes.FirstOrDefault(a => a.AttributeName == name)
                    ?? throw new ValidationException($"Data attribute '{name}' is not in AdviseCare, so transcripts cannot be imported.");
                if (attribute.Status != "active")
                    throw new ValidationException($"Data attribute '{name}' is archived, so transcripts cannot be imported.");
                if (!attribute.IsSubjectSpecific)
                    throw new ValidationException($"Data attribute '{name}' is not recorded per subject, so transcripts cannot be imported.");
                return attribute;
            }
            return (Find(GradeAttributeName), Find(AttendanceAttributeName));
        }

        // Helper: (student, data attribute, term, subject) of every value the students already have for the two transcript attributes
        private async Task<HashSet<(Guid StudentId, Guid DataAttributeId, Guid TermId, Guid SubjectId)>> GetCurrentValueKeysAsync(
            List<Guid> StudentIds, (Dataattribute Grade, Dataattribute Attendance) Attributes)
        {
            var keys = new HashSet<(Guid, Guid, Guid, Guid)>();
            if (StudentIds.Count == 0)
                return keys;

            var attributeIds = new[] { Attributes.Grade.DataAttributeId, Attributes.Attendance.DataAttributeId };
            var rows = await _studentDataRepository.SearchAsync(d =>
                StudentIds.Contains(d.StudentId) && attributeIds.Contains(d.DataAttributeId) && d.SubjectId != null && !d.IsMissing && d.ErasedAt == null);
            foreach (var row in rows)
                keys.Add((row.StudentId, row.DataAttributeId, row.TermId, row.SubjectId!.Value));
            return keys;
        }

        // Helper: check the count of an upload
        private static List<IFormFile> CheckUpload(List<IFormFile>? Files, int FilesInBatch)
        {
            var uploads = (Files ?? new List<IFormFile>()).Where(f => f != null).ToList();
            if (uploads.Count == 0)
                throw new ValidationException("At least one PDF file is required.");
            if (uploads.Count > MaxFilesPerUpload)
                throw new ValidationException($"Send at most {MaxFilesPerUpload} files at a time.");
            if (FilesInBatch + uploads.Count > MaxFilesPerBatch)
                throw new ValidationException($"A transcript import batch holds at most {MaxFilesPerBatch} files; import this one and start another.");
            return uploads;
        }

        // Helper: read and check each uploaded file and stage it as a valid or invalid file of the batch (saved by the caller)
        private async Task ReadFilesAsync(Guid TranscriptImportBatchId, List<Transcriptimportfile> ExistingFiles, List<IFormFile> Uploads)
        {
            var read = new List<(string FileName, string Hash, ParsedTranscript Parsed, List<string> Errors)>();
            foreach (var upload in Uploads)
            {
                var fileName = Cut(Path.GetFileName(upload.FileName ?? string.Empty), 255);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "transcript.pdf";
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

                var parsed = content.Length > 0 ? _transcriptPdfReader.Read(content) : new ParsedTranscript();
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
            var attributes = await GetTranscriptAttributesAsync();
            var subjects = SubjectLookup(await _subjectRepository.GetAllAsync());
            var terms = TermLookup(await _academicTermRepository.GetAllAsync());
            var currentKeys = await GetCurrentValueKeysAsync(profiles.Select(p => p.StudentId).ToList(), attributes);
            var hashes = read.Select(r => r.Hash).Distinct().ToList();
            var committedHashes = (await _transcriptImportFileRepository.SearchAsync(f => hashes.Contains(f.FileHash) && f.Status == "committed"))
                .GroupBy(f => f.FileHash)
                .ToDictionary(g => g.Key, g => g.Max(f => f.UploadedAt));

            // One file per student and type in a batch, and each file once
            var batchHashes = ExistingFiles.Select(f => f.FileHash).ToHashSet();
            var takenStudentTypes = new Dictionary<(Guid, string), string>();
            foreach (var file in ExistingFiles.Where(f => f.Status == "valid" && f.StudentId.HasValue && f.ImportType != null))
                takenStudentTypes.TryAdd((file.StudentId!.Value, file.ImportType), file.FileName);

            var uploadedAt = DateTime.UtcNow;
            foreach (var (fileName, hash, parsed, uploadErrors) in read)
            {
                var check = new TranscriptFileCheck();
                check.Errors.AddRange(uploadErrors);
                var file = new Transcriptimportfile
                {
                    TranscriptImportFileId = Guid.NewGuid(),
                    TranscriptImportBatchId = TranscriptImportBatchId,
                    CurriculumCode = NullIfEmpty(Cut(parsed.CurriculumCode, 50)),
                    FileHash = hash,
                    FileName = fileName,
                    FullNameInFile = NullIfEmpty(Cut(parsed.FullName, 255)),
                    StudentCodeInFile = NullIfEmpty(Cut(parsed.StudentCode, 50)),
                    UploadedAt = uploadedAt,
                    ImportType = parsed.ImportType.Length > 0 ? parsed.ImportType : null
                };

                if (check.Errors.Count == 0)
                {
                    if (parsed.StudentCode.Length == 0)
                        check.Errors.Add("No \"Student ID:\" was found at the top of the file.");
                    else if (!profileByCode.TryGetValue(parsed.StudentCode, out var profile))
                        check.Errors.Add($"Student {parsed.StudentCode} has no student profile in AdviseCare.");
                    else if (profile.Student?.Status != "active")
                        check.Errors.Add($"The account of student {parsed.StudentCode} is not active.");
                    else
                    {
                        file.StudentId = profile.StudentId;
                        if (parsed.FullName.Length > 0 && !SameName(parsed.FullName, profile.Student.FullName ?? string.Empty))
                            check.Warnings.Add($"The name in the file ({parsed.FullName}) is not the account's name ({profile.Student.FullName}).");
                    }
                }

                if (batchHashes.Contains(hash))
                    check.Errors.Add("The same file is already in this batch.");
                else if (committedHashes.TryGetValue(hash, out var importedAt))
                    check.Warnings.Add($"This exact file was already imported on {importedAt:yyyy-MM-dd}; importing it again records the same values again.");
                batchHashes.Add(hash);

                var rows = check.Errors.Count == 0 ? BuildRows(parsed, subjects, terms, check) : new List<TranscriptStoredRow>();

                if (check.Errors.Count == 0 && file.StudentId.HasValue && file.ImportType != null)
                {
                    var studentType = (file.StudentId.Value, file.ImportType);
                    if (takenStudentTypes.TryGetValue(studentType, out var otherFile))
                        check.Errors.Add($"{otherFile} in this batch already has the {file.ImportType} file of {parsed.StudentCode}.");
                    else
                        takenStudentTypes[studentType] = fileName;
                }

                if (check.Errors.Count == 0)
                {
                    var dataAttribute = file.ImportType == "grade" ? attributes.Grade : attributes.Attendance;
                    foreach (var row in rows)
                    {
                        var key = (file.StudentId!.Value, dataAttribute.DataAttributeId, terms[TermKey(row.TermName)].TermId, subjects[row.SubjectCode].SubjectId);
                        check.InsertCount++;
                        if (currentKeys.Contains(key))
                            check.ReplaceCount++;
                    }
                }

                file.RowCount = rows.Count;
                file.ParsedRows = check.Errors.Count == 0 ? JsonSerializer.Serialize(rows, JsonOptions) : null;
                file.ValidationResult = JsonSerializer.Serialize(check, JsonOptions);
                file.Status = check.Errors.Count == 0 ? "valid" : "invalid";
                _transcriptImportFileRepository.PrepareCreate(file);
            }
        }

        // Helper: turn the PDF's text rows into checked rows. A row without a semester (not started yet) or without a value ("-", e.g. exempt)
        // is left out with a warning; a subject or term AdviseCare does not have is an error, like in the Excel/CSV import
        private static List<TranscriptStoredRow> BuildRows(ParsedTranscript Parsed, Dictionary<string, Subject> Subjects,
            Dictionary<string, Academicterm> Terms, TranscriptFileCheck Check)
        {
            var rows = new List<TranscriptStoredRow>();
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
                    Check.SkippedRowCount++;
                    continue;
                }
                var termName = TermNameOf(semester);
                if (termName == null)
                {
                    Check.Errors.Add($"{label}: semester \"{semester}\" is not like Fall2024.");
                    continue;
                }
                if (!Terms.ContainsKey(TermKey(termName)))
                {
                    Check.Errors.Add($"{label}: term '{termName}' does not exist.");
                    continue;
                }
                if (code.Length == 0 || code == "-")
                {
                    Check.Errors.Add($"{label}: the subject code is empty.");
                    continue;
                }
                if (!Subjects.ContainsKey(code))
                {
                    Check.Errors.Add($"{label}: subject '{code}' does not exist.");
                    continue;
                }

                var number = ParseNumber(valueText.TrimEnd('%').Trim());
                if (!number.HasValue)
                {
                    Check.Errors.Add($"{label}: \"{valueText}\" is not a number.");
                    continue;
                }
                if (number < 0 || number > (isGrade ? 10m : 100m))
                {
                    Check.Errors.Add(isGrade
                        ? $"{label}: grade {valueText} is not between 0 and 10."
                        : $"{label}: attendance {valueText} is not between 0 and 100.");
                    continue;
                }
                if (!seen.Add($"{code}|{termName}"))
                {
                    Check.Errors.Add($"{label}: {code} in {termName} appears more than once.");
                    continue;
                }

                rows.Add(new TranscriptStoredRow
                {
                    RowNumber = row.RowNumber,
                    SubjectCode = Subjects[code].SubjectCode,
                    TermName = Terms[TermKey(termName)].TermName,
                    // Stored as STUDENTDATA values are: invariant decimal text (DataValueValidator, numeric)
                    Value = Math.Round(number.Value, 2).ToString(CultureInfo.InvariantCulture)
                });
            }

            if (Check.SkippedRowCount > 0)
                Check.Warnings.Add($"{Check.SkippedRowCount} row(s) have no semester or no value yet (not started, studying, exempt) and are left out.");
            if (rows.Count == 0 && Check.Errors.Count == 0)
                Check.Errors.Add("No subject row with a semester and a value was found in the file.");
            return rows;
        }

        // Helper: subjects by code, ignoring case
        private static Dictionary<string, Subject> SubjectLookup(List<Subject> Subjects) =>
            Subjects
                .GroupBy(s => s.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Helper: terms by name ("Fall 2026", "Fall2026" and "fall 2026" are the same term)
        private static Dictionary<string, Academicterm> TermLookup(List<Academicterm> Terms) =>
            Terms
                .GroupBy(t => TermKey(t.TermName))
                .ToDictionary(g => g.Key, g => g.First());

        // Helper: the key of a term name (letters and digits, lower case)
        private static string TermKey(string TermName) =>
            new string(TermName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        // Helper: "Summer2026" -> "Summer 2026" (the spelling of the ACADEMICTERMS rows); null when it is not a season and a year
        private static string? TermNameOf(string Semester)
        {
            var match = SemesterPattern.Match(Semester.Replace(" ", string.Empty));
            if (!match.Success)
                return null;
            var season = match.Groups[1].Value.ToLowerInvariant();
            return $"{char.ToUpperInvariant(season[0])}{season[1..]} {match.Groups[2].Value}";
        }

        // Helper: valid from the import day, kept inside the term (the term's end once it is over, its start before it begins)
        private static DateTime ValidFromOf(Academicterm Term, DateTime TransactionAt)
        {
            var date = TransactionAt.Date;
            if (date > Term.EndDate.Date)
                return Term.EndDate.Date;
            if (date < Term.StartDate.Date)
                return Term.StartDate.Date;
            return date;
        }

        // Helper: "8.3", "8,3", "85" -> number; no thousands separators, so "8,3" is never read as 83
        private static decimal? ParseNumber(string Text) =>
            decimal.TryParse(Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) ? number : null;

        // Helper: the same person's name, ignoring case, accents and spacing
        private static bool SameName(string A, string B) => NormalizeName(A) == NormalizeName(B);

        private static string NormalizeName(string Name)
        {
            var decomposed = Name.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            return new string(decomposed.Where(char.IsLetter).ToArray()).ToLowerInvariant();
        }

        private static string Cut(string Text, int Length) => Text.Length <= Length ? Text : Text[..Length];

        private static string? NullIfEmpty(string Text) => string.IsNullOrWhiteSpace(Text) ? null : Text;

        private static List<TranscriptStoredRow> ReadRows(string? Json) =>
            string.IsNullOrWhiteSpace(Json) ? new List<TranscriptStoredRow>() : JsonSerializer.Deserialize<List<TranscriptStoredRow>>(Json, JsonOptions) ?? new();

        private static TranscriptFileCheck ReadCheck(string? Json) =>
            string.IsNullOrWhiteSpace(Json) ? new TranscriptFileCheck() : JsonSerializer.Deserialize<TranscriptFileCheck>(Json, JsonOptions) ?? new();

        // Helper types: one checked row of a file (TRANSCRIPTIMPORTFILES.parsedRows, kept between preview and import) and what the check
        // of one file found (TRANSCRIPTIMPORTFILES.validationResult)
        private class TranscriptStoredRow
        {
            public int RowNumber { get; set; }
            public string SubjectCode { get; set; } = string.Empty;
            public string TermName { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private class TranscriptFileCheck
        {
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public int InsertCount { get; set; }
            public int ReplaceCount { get; set; }
            public int SkippedRowCount { get; set; }
        }
    }
}
