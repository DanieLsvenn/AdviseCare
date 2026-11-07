using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Reports are generated on the fly and never stored, so this controller does not use the REPORTS table (stored report history is FU2)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Programme Head")]
    public class ReportsController(IReportService reportService) : ControllerBase
    {
        private readonly IReportService _reportService = reportService;

        // Get the Case report
        [HttpGet("cases")]
        [SwaggerOperation(Summary = "[Programme Head] List the cases opened in a date range",
            Description = "from and to are inclusive UTC dates (e.g. 2026-09-07). Optional filters: advisorId, supportType, supportCategoryId, subjectId, currentStage, status. " +
                "Each row has daysToClose for closed cases.")]
        public async Task<IActionResult> GetCaseReport([FromQuery] GetCaseReportRequest request)
        {
            var response = await _reportService.GetCaseReportAsync(request);
            return Ok(response);
        }

        // Export the Case report
        [HttpGet("cases/export")]
        [SwaggerOperation(Summary = "[Programme Head] Export the case report",
            Description = "Same filters as /cases plus format = xlsx (default), csv or pdf. Downloads the file; nothing is stored, and the export is recorded in the activity log (reportExported).")]
        public async Task<IActionResult> ExportCaseReport([FromQuery] ExportCaseReportRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _reportService.ExportCaseReportAsync(programmeHeadId, request);
            return File(response.Content, response.ContentType, response.FileName);
        }

        // Get the Caseload and closure report
        [HttpGet("caseload-closure")]
        [SwaggerOperation(Summary = "[Programme Head] Caseload volume and average closure time per advisor",
            Description = "Over the cases opened in the range, grouped by their current advisor: total, open and closed cases, and the average days to close (closed cases only). " +
                "Optional filters: advisorId, supportType.")]
        public async Task<IActionResult> GetCaseloadClosureReport([FromQuery] GetCaseloadClosureReportRequest request)
        {
            var response = await _reportService.GetCaseloadClosureReportAsync(request);
            return Ok(response);
        }

        // Export the Caseload and closure report
        [HttpGet("caseload-closure/export")]
        [SwaggerOperation(Summary = "[Programme Head] Export the caseload and closure report",
            Description = "Same filters as /caseload-closure plus format = xlsx (default), csv or pdf. Downloads the file; nothing is stored, and the export is recorded in the activity log (reportExported).")]
        public async Task<IActionResult> ExportCaseloadClosureReport([FromQuery] ExportCaseloadClosureReportRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _reportService.ExportCaseloadClosureReportAsync(programmeHeadId, request);
            return File(response.Content, response.ContentType, response.FileName);
        }
    }
}
