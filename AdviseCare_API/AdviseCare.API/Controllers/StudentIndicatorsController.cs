using AdviseCare.Application.Contracts.Requests.StudentIndicator;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentIndicatorsController(
        IStudentIndicatorImportService importService,
        IStudentIndicatorProfileService profileService,
        IStudentDataExportService dataExportService) : ControllerBase
    {
        private readonly IStudentIndicatorImportService _importService = importService;
        private readonly IStudentIndicatorProfileService _profileService = profileService;
        private readonly IStudentDataExportService _dataExportService = dataExportService;

        [HttpPost("import/preview")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Preview student indicator import file",
            Description = "Reads the header and up to five sample rows from an .xlsx or .csv file. Does not write data.")]
        public async Task<IActionResult> PreviewImport([FromForm] PreviewStudentIndicatorImportRequest request)
        {
            return Ok(await _importService.PreviewAsync(request));
        }

        [HttpPost("import/validate")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Validate a student indicator import",
            Description = "Validates column mapping and every row in an .xlsx or .csv file. This endpoint does not insert or update student indicator data.")]
        public async Task<IActionResult> ValidateImport([FromForm] ValidateStudentIndicatorImportRequest request)
        {
            return Ok(await _importService.ValidateAsync(request));
        }

        [HttpPost("import")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Import student indicator values",
            Description = "Validates the complete .xlsx or .csv file, then inserts encrypted values and expected-but-missing rows in one database save. Any invalid row prevents all writes.")]
        public async Task<IActionResult> Import([FromForm] ValidateStudentIndicatorImportRequest request)
        {
            return Ok(await _importService.ImportAsync(request));
        }

        [HttpGet("students/{studentId:guid}/profile")]
        [SwaggerOperation(Summary = "[Assigned advisor] Read a student's indicator profile",
            Description = "Checks case assignment and consent access before decrypting each value; writes one access log per permitted indicator read.")]
        public async Task<IActionResult> GetStudentProfile(
            Guid studentId,
            [FromQuery] Guid caseId,
            [FromQuery] Guid? termId,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out var accessorId))
            {
                return Unauthorized();
            }

            return Ok(await _profileService.GetProfileAsync(accessorId, studentId, caseId, termId, cancellationToken));
        }

        [HttpGet("students/{studentId:guid}/as-of")]
        [SwaggerOperation(Summary = "[Assigned advisor] Read a student's indicator profile as of a date",
            Description = "Reconstructs the latest indicator row known by the system through the end of the selected UTC date. Applies the case consent guard and logs each permitted value.")]
        public async Task<IActionResult> GetStudentProfileAsOf(
            Guid studentId,
            [FromQuery] Guid caseId,
            [FromQuery] DateOnly date,
            [FromQuery] Guid? termId,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out var accessorId))
            {
                return Unauthorized();
            }

            return Ok(await _profileService.GetAsOfAsync(
                accessorId,
                studentId,
                caseId,
                date,
                termId,
                cancellationToken));
        }

        [HttpGet("students/{studentId:guid}/compare")]
        [SwaggerOperation(Summary = "[Assigned advisor] Compare a student's indicator profiles on two dates",
            Description = "Returns the union of indicator/term rows in both snapshots; each date is independently authorized and permitted values are logged.")]
        public async Task<IActionResult> CompareStudentProfiles(
            Guid studentId,
            [FromQuery] Guid caseId,
            [FromQuery] DateOnly dateA,
            [FromQuery] DateOnly dateB,
            [FromQuery] Guid? termId,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out var accessorId))
            {
                return Unauthorized();
            }

            return Ok(await _profileService.CompareAsync(
                accessorId,
                studentId,
                caseId,
                dateA,
                dateB,
                termId,
                cancellationToken));
        }

        [HttpGet("me/profile")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Read your own indicator profile",
            Description = "Returns the authenticated student's current indicator profile. The student can always read their own values; no case or consent is required.")]
        public async Task<IActionResult> GetMyProfile(
            [FromQuery] Guid? termId,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out var studentId))
            {
                return Unauthorized();
            }

            return Ok(await _profileService.GetOwnProfileAsync(studentId, termId, cancellationToken));
        }

        [HttpGet("me/export")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Export your own data as JSON",
            Description = "Downloads the student's identity, current indicator profile, own cases, and own access log. The export is recorded in the activity log.")]
        public async Task<IActionResult> ExportMyData(CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out var studentId))
            {
                return Unauthorized();
            }

            var export = await _dataExportService.ExportOwnDataAsync(studentId, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
