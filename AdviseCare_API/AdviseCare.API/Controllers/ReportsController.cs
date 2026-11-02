using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Reports are generated on the fly and never stored, so this controller does not use the REPORTS table
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Programme Head")]
    public class ReportsController(ICaseReportService caseReportService) : ControllerBase
    {
        private readonly ICaseReportService _caseReportService = caseReportService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpGet("cases")]
        [SwaggerOperation(Summary = "[Programme Head] List the cases opened in a date range",
            Description = "from and to are inclusive UTC dates (e.g. 2026-09-07). Optional filters: advisorId, supportType, supportCategoryId, subjectId, currentStage, status.")]
        public async Task<IActionResult> GetCases([FromQuery] CaseReportRequest request)
        {
            return Ok(await _caseReportService.GetCasesAsync(request));
        }

        [HttpGet("caseload-closure")]
        [SwaggerOperation(Summary = "[Programme Head] Caseload volume and average closure time per advisor",
            Description = "Over the cases opened in the range and grouped by their current advisor: total, open and closed cases, and the average days to close (closed cases only). " +
                "Optional filters: advisorId, supportType.")]
        public async Task<IActionResult> GetCaseloadClosure([FromQuery] CaseloadClosureRequest request)
        {
            return Ok(await _caseReportService.GetCaseloadClosureAsync(request));
        }

        [HttpGet("cases/export")]
        [SwaggerOperation(Summary = "[Programme Head] Export the cases list",
            Description = "Same filters as /cases plus format = xlsx (default), csv or pdf. Returns the file itself; nothing is stored.")]
        public async Task<IActionResult> ExportCases([FromQuery] CaseReportExportRequest request)
        {
            var file = await _caseReportService.ExportCasesAsync(CurrentAccountId, request);
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("caseload-closure/export")]
        [SwaggerOperation(Summary = "[Programme Head] Export the caseload and closure report",
            Description = "Same filters as /caseload-closure plus format = xlsx (default), csv or pdf. Returns the file itself; nothing is stored.")]
        public async Task<IActionResult> ExportCaseloadClosure([FromQuery] CaseloadClosureExportRequest request)
        {
            var file = await _caseReportService.ExportCaseloadClosureAsync(CurrentAccountId, request);
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
