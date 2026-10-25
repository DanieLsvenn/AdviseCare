using AdviseCare.Application.Contracts.Requests.AccessLog;
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
    public class AccessLogsController(IAccessLogService accessLogService) : ControllerBase
    {
        private readonly IAccessLogService _accessLogService = accessLogService;

        // Get all Access logs, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all access log entries with optional pagination",
            Description = "Retrieves every logged read of student data (who, whose record, which resource, under which purpose, when), newest first. " +
                "Entries are written by the access guard, one per data attribute read, and are never edited apart from their review fields. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _accessLogService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _accessLogService.GetAll());
        }

        // Get an Access log entry by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get access log entry by Id",
            Description = "Retrieves the detail of one access event.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _accessLogService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Access logs by accessor, purpose, student, date range, flag or review status
        [HttpGet("search")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter access log",
            Description = "Filters the access log by accessor (accessorId), purpose (consentPurposeId), student (recordedUserId), accessedAt between fromDate and toDate, " +
                "flag (isFlagged) and review status (unreviewed/cleared/confirmedIssue). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchAccessLogRequest request)
        {
            var response = await _accessLogService.SearchAsync(request);
            return Ok(response);
        }

        // Review a flagged Access log entry
        [HttpPut("{id}/review")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Review a flagged access log entry",
            Description = "Records the outcome of the review of a flagged entry: cleared or confirmedIssue. Only flagged entries can be reviewed.")]
        public async Task<IActionResult> Review([FromRoute] Guid id, [FromBody] ReviewAccessLogRequest request)
        {
            var response = await _accessLogService.ReviewAsync(id, request);
            return Ok(response);
        }

        // Flag an Access log entry for follow-up
        [HttpPut("{id}/flag")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Flag an access log entry",
            Description = "Flags a suspicious entry for follow-up (isFlagged = true, status = unreviewed).")]
        public async Task<IActionResult> Flag([FromRoute] Guid id)
        {
            var response = await _accessLogService.FlagAsync(id);
            return Ok(response);
        }

        // Export a processing record
        [HttpGet("processing-record/export")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Export a processing record",
            Description = "Downloads the access log of one student (studentId) and/or a date range (fromDate and toDate, both needed when no student is given) as an .xlsx or .csv file, generated on the fly and not stored. " +
                "Returns the file itself instead of JSON.")]
        public async Task<IActionResult> ExportProcessingRecord([FromQuery] ExportProcessingRecordRequest request)
        {
            var response = await _accessLogService.ExportProcessingRecordAsync(request);
            return File(response.Content, response.ContentType, response.FileName);
        }

        // Get the access log of the current Student's own record
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get my access log",
            Description = "Lists who read which part of the signed-in student's record, when and under which purpose, newest first, with the accessor's roles. " +
                "Can be filtered by accessedAt between fromDate and toDate and by the accessor's role (roleName).")]
        public async Task<IActionResult> GetMine([FromQuery] SearchMyAccessLogRequest request)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accessLogService.GetMyAccessLogsAsync(studentId, request);
            return Ok(response);
        }

        // Flag an entry of the current Student's own access log
        [HttpPut("me/{id}/flag")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Flag an entry of my access log",
            Description = "Flags an access to the signed-in student's record as suspicious; every Data Protection Steward is notified. Returns 404 for an entry about another student.")]
        public async Task<IActionResult> FlagMine([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accessLogService.FlagMineAsync(studentId, id);
            return Ok(response);
        }
    }
}
