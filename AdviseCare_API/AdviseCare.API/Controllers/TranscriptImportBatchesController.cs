using AdviseCare.Application.Contracts.Requests.TranscriptImportBatch;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Transcript import batches: the admin imports grade transcripts and attendance files (PDF, one student per file) as per-subject student data
    // (upload in parts -> preview -> remove invalid files -> import all-or-nothing, or cancel)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "System Administrator")]
    public class TranscriptImportBatchesController(ITranscriptImportBatchService transcriptImportBatchService) : ControllerBase
    {
        // One upload request: at most 50 PDFs of 5 MB; the web app sends a large batch in parts of about 20 MB
        private const long UploadLimitBytes = 100L * 1024 * 1024;
        private readonly ITranscriptImportBatchService _transcriptImportBatchService = transcriptImportBatchService;

        // Get all Transcript import batches, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[System Administrator] Get all transcript import batches with optional pagination",
            Description = "Lists the transcript import batches, newest first: who started each one, when, when it was imported, its status (previewed/committed/cancelled) " +
                "and its file counts (total, valid, invalid, committed). Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _transcriptImportBatchService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _transcriptImportBatchService.GetAll());
        }

        // Get a Transcript import batch by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[System Administrator] Get transcript import batch by Id",
            Description = "The batch preview (or the record of an imported/cancelled batch) with every file: student matched by the printed Student ID, type (grade/attendance), " +
                "rows read, values to add and how many replace a current value, rows left out, errors and warnings.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Transcript import batch not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _transcriptImportBatchService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Transcript import batches
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter transcript import batches",
            Description = "Filters the batches by the admin who started them (importedBy) and by status (previewed/committed/cancelled), newest first.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchTranscriptImportBatchRequest request)
        {
            var response = await _transcriptImportBatchService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Transcript import batch with its first files
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadLimitBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimitBytes)]
        [SwaggerOperation(Summary = "[System Administrator] Start a transcript import batch",
            Description = "Reads each PDF (files: up to 50 per request; one student per file; a grade transcript or an attendance file, told by its Grade or Attendance column) and checks it: " +
                "the printed Student ID belongs to an active student, every semester is a term in AdviseCare (e.g. Fall2024 -> Fall 2024), every subject code is a subject in AdviseCare, " +
                "grades are 0-10 and attendance 0-100, no subject is listed twice in a term, one file per student and type in the batch, the same file only once. " +
                "Rows without a semester or without a value (\"-\") are left out with a warning. Nothing is written to the student data yet; the batch preview is returned.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- At least one PDF file is required.\n- Send at most 50 files at a time.\n- Data attribute '{name}' is not in AdviseCare / is archived / is not recorded per subject, so transcripts cannot be imported.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromForm] CreateTranscriptImportBatchRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _transcriptImportBatchService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Add files to a Transcript import batch
        [HttpPost("{id}/files")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadLimitBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimitBytes)]
        [SwaggerOperation(Summary = "[System Administrator] Add files to a transcript import batch",
            Description = "Same checks as starting a batch; used for the next parts of a large batch and for fixed files. A batch holds at most 500 files.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- At least one PDF file is required.\n- Send at most 50 files at a time.\n- A transcript import batch holds at most 500 files; import this one and start another.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Transcript import batch not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This transcript import batch is already committed/cancelled; start a new one.", typeof(ErrorResponse))]
        public async Task<IActionResult> AddFiles([FromRoute] Guid id, [FromForm] AddTranscriptImportFilesRequest request)
        {
            var response = await _transcriptImportBatchService.AddFilesAsync(id, request);
            return Ok(response);
        }

        // Delete a file from a Transcript import batch
        [HttpDelete("{id}/files/{fileId}")]
        [SwaggerOperation(Summary = "[System Administrator] Remove a file from a transcript import batch",
            Description = "Removes one file (an invalid one, or one uploaded by mistake) from a batch that is still in preview, so the rest can be imported. Returns the batch preview.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Transcript import batch not found.\n- Transcript import file not found in this batch.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This transcript import batch is already committed/cancelled; start a new one.", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteFile([FromRoute] Guid id, [FromRoute] Guid fileId)
        {
            var response = await _transcriptImportBatchService.DeleteFileAsync(id, fileId);
            return Ok(response);
        }

        // Import a Transcript import batch
        [HttpPost("{id}/commit")]
        [SwaggerOperation(Summary = "[System Administrator] Import a transcript import batch",
            Description = "All-or-nothing: only when every file of the batch is valid, every row is encrypted and inserted into the student data in one save, as a per-subject value of " +
                "Subject mark (grade files) or Attendance rate (attendance files) for the student, subject and term; valid from the import day, kept inside the term. " +
                "The ledger is insert-only, so a value that replaces a current one is added as a newer row. No missing rows are recorded (per-subject attributes). Logged as transcriptImportCommitted.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- The batch has no file to import.\n- The batch has {n} invalid file(s); remove them (or upload fixed files) before importing.\n- {file}: subject/term ... is no longer in AdviseCare; upload the file again.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Transcript import batch not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This transcript import batch is already committed/cancelled; start a new one.", typeof(ErrorResponse))]
        public async Task<IActionResult> Commit([FromRoute] Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _transcriptImportBatchService.CommitAsync(adminId, id);
            return Ok(response);
        }

        // Cancel a Transcript import batch
        [HttpPost("{id}/cancel")]
        [SwaggerOperation(Summary = "[System Administrator] Cancel a transcript import batch",
            Description = "Nothing is written to the student data; the rows read from the files are cleared and the batch is kept as cancelled. Logged as transcriptImportCancelled.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Transcript import batch not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This transcript import batch is already committed/cancelled; start a new one.", typeof(ErrorResponse))]
        public async Task<IActionResult> Cancel([FromRoute] Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _transcriptImportBatchService.CancelAsync(adminId, id);
            return Ok(response);
        }
    }
}
