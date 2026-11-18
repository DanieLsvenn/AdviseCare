using AdviseCare.Application.Contracts.Requests.AcademicResult;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Academic results: per-subject grade and attendance per term. Import of transcript PDFs by the admin (batch: upload -> preview -> commit),
    // the student's own view, and consent-guarded reads by staff
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicResultsController(IAcademicResultService academicResultService) : ControllerBase
    {
        private const string ReaderRoles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head";
        // One upload request: at most 50 PDFs; the web app sends a large batch in parts of about 20 MB
        private const long UploadLimitBytes = 100L * 1024 * 1024;
        private readonly IAcademicResultService _academicResultService = academicResultService;

        // Start an import batch
        [HttpPost("imports")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadLimitBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimitBytes)]
        [SwaggerOperation(Summary = "[System Administrator] Start a transcript import batch",
            Description = "Reads each PDF (one student per file; a grade transcript or an attendance file, told by its Grade or Attendance column) and checks it: " +
                "student code in AdviseCare, semester like Fall2024, number ranges, status, duplicates. Each file is valid or invalid on its own; nothing is written to the results yet. " +
                "Returns the batch preview: per file the student, type, rows, results to create/update, errors and warnings; the new subjects and terms the commit will add.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- At least one PDF file is required.\n- Send at most 50 files at a time.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateBatch([FromForm] UploadTranscriptsRequest request)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.CreateBatchAsync(adminId, request));
        }

        // Add files to a batch in preview
        [HttpPost("imports/{importBatchId}/files")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadLimitBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimitBytes)]
        [SwaggerOperation(Summary = "[System Administrator] Add files to a transcript import batch",
            Description = "Same checks as starting a batch. A batch holds at most 500 files and one file per student and type.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- At least one PDF file is required.\n- Send at most 50 files at a time.\n- An import batch holds at most 500 files; commit this one and start another.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Import batch not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This import batch is already committed/cancelled; start a new one.", typeof(ErrorResponse))]
        public async Task<IActionResult> AddFiles([FromRoute] Guid importBatchId, [FromForm] UploadTranscriptsRequest request)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.AddFilesAsync(adminId, importBatchId, request));
        }

        // Recent import batches
        [HttpGet("imports")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get recent transcript import batches",
            Description = "Newest first, with file counts per status. count: 1-100, default 20.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetBatches([FromQuery] int? count)
        {
            return Ok(await _academicResultService.GetBatchesAsync(count ?? 20));
        }

        // One import batch with its files
        [HttpGet("imports/{importBatchId}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get a transcript import batch",
            Description = "The batch preview (or the record of a committed/cancelled batch) with every file's check result.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Import batch not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetBatch([FromRoute] Guid importBatchId)
        {
            return Ok(await _academicResultService.GetBatchAsync(importBatchId));
        }

        // Commit the valid files of a batch
        [HttpPost("imports/{importBatchId}/commit")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Commit a transcript import batch",
            Description = "Writes every valid file in one transaction: one result per student, subject and term is created or updated (the grade file fills the grade and status, the attendance file the attendance rate). " +
                "Invalid files are left out. Subjects not in AdviseCare are created under facultyId (required only when there are such subjects); missing terms are created with the season dates. " +
                "The curriculum on the transcript is saved on the student profile. Logged as academicResultsImported.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- The batch has no valid file to import.\n- Choose the faculty of the {n} new subject(s): ...", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Import batch not found.\n- Faculty not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This import batch is already committed/cancelled.", typeof(ErrorResponse))]
        public async Task<IActionResult> Commit([FromRoute] Guid importBatchId, [FromBody] CommitTranscriptImportRequest request)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.CommitAsync(adminId, importBatchId, request));
        }

        // Cancel a batch in preview
        [HttpPost("imports/{importBatchId}/cancel")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Cancel a transcript import batch",
            Description = "Nothing is written to the results; the rows read from the files are cleared. Logged as academicResultsImportCancelled.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Import batch not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This import batch is already committed/cancelled.", typeof(ErrorResponse))]
        public async Task<IActionResult> Cancel([FromRoute] Guid importBatchId)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.CancelAsync(adminId, importBatchId));
        }

        // The signed-in student's own results
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get own academic results",
            Description = "Grade, attendance rate, absence rate and status of every subject, grouped by term (newest first), with a summary per term. No consent is needed and nothing is logged.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Your student profile has not been set up yet.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMine([FromQuery] Guid? termId)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.GetMyResultsAsync(studentId, termId));
        }

        // A student's results for staff
        [HttpGet("students/{studentId}")]
        [Authorize(Roles = ReaderRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get a student's academic results",
            Description = "The grade part (grade, status) is shown only when the student shared the GPA data attribute with the reader, the attendance part only for Attendance rate; " +
                "same rules as the student data (with caseId: only the case's advisor and the case category's purpose; without: the reader's role policies or an approved emergency access). " +
                "isGradeShared / isAttendanceShared say which part is shown. Each part shown is written to the access log.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetStudentResults([FromRoute] Guid studentId, [FromQuery] GetAcademicResultsRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _academicResultService.GetStudentResultsAsync(accessorId, studentId, request));
        }
    }
}
