using AdviseCare.Application.Contracts.Requests.TakeOnRequest;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Take-on requests: an advisor asks to take on an unassigned student case or an open indicator flag; the Programme Head approves or declines
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Programme Head,Academic Advisor,Wellbeing Advisor")]
    public class TakeOnRequestsController(ITakeOnRequestService takeOnRequestService) : ControllerBase
    {
        private readonly ITakeOnRequestService _takeOnRequestService = takeOnRequestService;

        // Get all Take-on requests, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get all take-on requests with optional pagination",
            Description = "Lists every take-on request, pending first, then newest first. targetType is case or flag; each row shows the advisor (and advisorOpenCaseCount, their cases that are not closed), the student's directory data, the category, subject, term and rule name (flag), " +
                "the item's severity and priority, the note, and the decision (decidedBy, declineReason, decidedAt). Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _takeOnRequestService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _takeOnRequestService.GetAll());
        }

        // Get a Take-on request by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Programme Head, Academic Advisor, Wellbeing Advisor] Get take-on request by Id",
            Description = "Visible to the Programme Head and to the advisor who sent it.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Take-on request not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Take-on requests
        [HttpGet("search")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Search take-on requests",
            Description = "Filters: advisorId, caseId, indicatorFlagId and status (pending/approved/declined/withdrawn/cancelled). Empty fields are ignored. Pending first, then newest first. " +
                "E.g. ?indicatorFlagId=...&status=pending fills the Requests pop-up of a flag, and ?caseId=...&status=pending that of a case in the matching queue.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchTakeOnRequestRequest request)
        {
            var response = await _takeOnRequestService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Take-on request
        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Ask to take on a case or an indicator flag",
            Description = "Send exactly one of caseId (a student-created case of your support type with no advisor, in stage pending and not closed; GET /api/Cases/take-on) and indicatorFlagId (an open flag of your support type; GET /api/IndicatorFlags), with an optional note (max 1000 characters) to the Programme Head. " +
                "You can have one pending request per case or flag. Every Programme Head is notified (in-app, and by email when that preference is on).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Choose either a case or an indicator flag.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Indicator flag not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- The case already has an advisor.\n- You already have a pending request for this case.\n- This flag is no longer open.\n- You already have a pending request for this flag.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateTakeOnRequestRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update a Take-on request (the note)
        [HttpPut("{id}")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Change the note of my pending take-on request",
            Description = "Only the advisor who sent the request, while it is pending. An empty note removes it.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Take-on request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending take-on request can be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTakeOnRequestRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.UpdateAsync(callerId, id, request);
            return Ok(response);
        }

        // Get my Take-on requests
        [HttpGet("me")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get my take-on requests (My requests)",
            Description = "Lists the requests the signed-in advisor sent, pending first, then newest first, optionally by status (pending/approved/declined/withdrawn/cancelled). A declined request shows the Programme Head's declineReason.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMine([FromQuery] SearchMyTakeOnRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.GetMyAllAsync(advisorId, request);
            return Ok(response);
        }

        // Approve a Take-on request
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Approve a take-on request",
            Description = "A case request assigns the case to the advisor, as PUT /api/Cases/{id}/assign does (pending -> assigned, the student gets the advisor's contact information). " +
                "A flag request opens a case for the advisor from the flag, as PUT /api/IndicatorFlags/{id}/open-case does (the case copies the flag's severity and priority). " +
                "The request and the case change are saved together; the other pending requests for the same case or flag are cancelled. The advisor is told the request was approved; the other advisors are told theirs was cancelled.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Take-on request not found.\n- Case not found.\n- Indicator flag not found.\n- Advisor account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This take-on request has already been decided, withdrawn or cancelled.\n- The case is closed.\n- The case already has an advisor. Use reassign or a case change request to change it.\n- Only an open flag can be turned into a case.", typeof(ErrorResponse))]
        public async Task<IActionResult> Approve([FromRoute] Guid id)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.ApproveAsync(programmeHeadId, id);
            return Ok(response);
        }

        // Decline a Take-on request
        [HttpPut("{id}/decline")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Decline a take-on request",
            Description = "Turns down one advisor's request with a reason (required, max 1000 characters); the case or flag stays open for other advisors. The advisor is notified with the reason (in-app, and by email when that preference is on).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Decline reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Take-on request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This take-on request has already been decided, withdrawn or cancelled.", typeof(ErrorResponse))]
        public async Task<IActionResult> Decline([FromRoute] Guid id, [FromBody] DeclineTakeOnRequestRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.DeclineAsync(programmeHeadId, id, request);
            return Ok(response);
        }

        // Withdraw a Take-on request
        [HttpPut("{id}/withdraw")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Withdraw my take-on request",
            Description = "The advisor who sent a pending request takes it back (status withdrawn).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Take-on request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending take-on request can be withdrawn.", typeof(ErrorResponse))]
        public async Task<IActionResult> Withdraw([FromRoute] Guid id)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _takeOnRequestService.WithdrawAsync(advisorId, id);
            return Ok(response);
        }
    }
}
