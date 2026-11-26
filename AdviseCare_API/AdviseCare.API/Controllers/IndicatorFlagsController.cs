using AdviseCare.Application.Contracts.Requests.IndicatorFlag;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Early-support indicator flags ("Students in need"): raised and cleared by the server, handled by the Programme Head, asked for by advisors
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Programme Head,Academic Advisor,Wellbeing Advisor")]
    public class IndicatorFlagsController(IIndicatorFlagService indicatorFlagService) : ControllerBase
    {
        private readonly IIndicatorFlagService _indicatorFlagService = indicatorFlagService;

        // Get all Indicator flags, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Programme Head, Academic Advisor, Wellbeing Advisor] Get all indicator flags with optional pagination",
            Description = "The Programme Head sees every flag; an advisor sees the open flags of their support type (academic or wellbeing, from the rule's support category). " +
                "Ordered by severity (critical first), then priority, then oldest. A flag shows the student's directory data (name, student code, faculty), the rule (name, description, data attribute, comparisonOperator, thresholdValue), severity, priority, term and subject, never the student's value, so no ACCESSLOGS row is written. " +
                "pendingTakeOnRequestCount (Programme Head) is the number of advisors' requests waiting for the flag (the red dot on its Requests button); myTakeOnRequestId and myTakeOnRequestStatus (advisor) are the advisor's own latest request. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _indicatorFlagService.GetAllWithPaginationAsync(
                    callerId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _indicatorFlagService.GetAll(callerId));
        }

        // Get an Indicator flag by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Programme Head, Academic Advisor, Wellbeing Advisor] Get indicator flag by Id",
            Description = "The Programme Head sees any flag; an advisor sees an open flag of their support type, or a flag they sent a take-on request for. Never shows the student's value.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Indicator flag not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _indicatorFlagService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Indicator flags
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Programme Head, Academic Advisor, Wellbeing Advisor] Search indicator flags",
            Description = "Filters: indicatorId, studentId, subjectId, termId, priority (low/medium/high/urgent), severity (low/moderate/high/critical), supportType (academic/wellbeing) and status (open/caseOpened/dismissed/cleared). Empty fields are ignored. Same visibility and order as GET /api/IndicatorFlags.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchIndicatorFlagRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _indicatorFlagService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Run the Indicator flag check
        [HttpPost("check")]
        [Authorize(Roles = "Programme Head,Data Protection Steward")]
        [SwaggerOperation(Summary = "[Programme Head, Data Protection Steward] Run the indicator flag check now",
            Description = "The check also runs by itself after every student data import (file and transcripts). It compares each enrolled, active student's current value of every active indicator's data attribute (per term, and per subject for a per-subject attribute) with the rule: " +
                "numeric values compare as numbers (lessThan, lessOrEqual, greaterThan, greaterOrEqual, equal, notEqual); boolean and categorical values support equal and notEqual. " +
                "A hit raises one open flag per student, rule, term and subject (with the rule's severity and priority), unless the Programme Head already dismissed or opened a case from a flag for the same student, rule, term and subject. " +
                "An open flag whose student no longer hits the rule (or whose rule is no longer active) is cleared and its pending take-on requests are cancelled. Every Programme Head is notified when new flags are raised. The values are decrypted only to compare; no ACCESSLOGS row is written.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Check()
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _indicatorFlagService.CheckAsync(callerId);
            return Ok(response);
        }

        // Dismiss an Indicator flag
        [HttpPut("{id}/dismiss")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Dismiss an indicator flag",
            Description = "Closes an open flag with a reason (required, max 1000 characters); the flag stays in the list with status dismissed and is not raised again for the same student, rule, term and subject. Its pending take-on requests are cancelled and their advisors are notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Dismiss reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Indicator flag not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only an open flag can be dismissed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Dismiss([FromRoute] Guid id, [FromBody] DismissIndicatorFlagRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _indicatorFlagService.DismissAsync(programmeHeadId, id, request);
            return Ok(response);
        }

        // Open a Case from an Indicator flag
        [HttpPut("{id}/open-case")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Open a case from an indicator flag for an advisor",
            Description = "Opens a case of the rule's support category for the chosen advisor (an active account with the advisor role of the category's support type): opened by the Programme Head, stage assigned (with its stage history row; reason optional), the flag's subject, severity and priority, and the rule as the request reason. " +
                "The flag gets the case (status caseOpened); its pending take-on requests are cancelled. The student is told who will contact them and the advisor is told about the case. To approve an advisor's request instead, use PUT /api/TakeOnRequests/{id}/approve.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Indicator flag not found.\n- Advisor account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only an open flag can be turned into a case.", typeof(ErrorResponse))]
        public async Task<IActionResult> OpenCase([FromRoute] Guid id, [FromBody] OpenCaseFromIndicatorFlagRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _indicatorFlagService.OpenCaseAsync(programmeHeadId, id, request);
            return Ok(response);
        }
    }
}
