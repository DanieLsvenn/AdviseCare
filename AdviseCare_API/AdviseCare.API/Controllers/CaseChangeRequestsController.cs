using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Programme Head")]
    public class CaseChangeRequestsController(ICaseChangeRequestService caseChangeRequestService) : ControllerBase
    {
        private readonly ICaseChangeRequestService _caseChangeRequestService = caseChangeRequestService;

        // Get all Case change requests, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor, Programme Head] Get all case change requests with optional pagination",
            Description = "Newest first. The Programme Head sees every request; others only the requests they made, are proposed for, or that concern their own cases. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseChangeRequestService.GetAllWithPaginationAsync(
                    callerId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseChangeRequestService.GetAll(callerId));
        }

        // Get a Case change request by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor, Programme Head] Get case change request by Id",
            Description = "Retrieves one request for the Programme Head or the people involved (anyone else gets 404).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case change request not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseChangeRequestService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Case change requests
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor, Programme Head] Search/Filter case change requests",
            Description = "Filters the requests the caller may see by case and status (pending/approved/rejected), newest first. Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchCaseChangeRequestRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseChangeRequestService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Case change request
        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Ask the Programme Head to move a case to another advisor",
            Description = "Made by the case's student or its current advisor. The case must have an advisor and not be closed; the proposed advisor must be an active advisor of the case's type; " +
                "one pending request per case. The Programme Heads are notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The proposed advisor already handles this case.\n- Reason is required.\n- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Advisor account not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- The case has no advisor yet; the Programme Head will match it.\n- A change request for this case is already waiting for the Programme Head.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateCaseChangeRequestRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseChangeRequestService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Approve a Case change request
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Approve a pending case change request",
            Description = "The case moves to the proposed advisor (stage kept, hand-over written to the stage history); programmeHeadId, decidedAt and the optional decisionNotes are set (the body may be left empty). " +
                "Refused while a referral of the case is pending. The student and both advisors are notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Case change request not found.\n- Advisor account not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case was closed after the request. Reject the request instead.\n- The proposed advisor already handles this case. Reject the request instead.\n- A referral of this case is waiting for an answer. Decide the request after the referral is accepted or returned.\n- The request was already {Status}.", typeof(ErrorResponse))]
        public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ApproveCaseChangeRequestRequest? request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseChangeRequestService.ApproveAsync(programmeHeadId, id, request ?? new ApproveCaseChangeRequestRequest());
            return Ok(response);
        }

        // Reject a Case change request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Reject a pending case change request",
            Description = "decisionNotes (required) explain why; the case keeps its advisor. The requester, the student and both advisors are notified with the reason.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Decision notes are required when rejecting.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Case change request not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The request was already {Status}.", typeof(ErrorResponse))]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectCaseChangeRequestRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseChangeRequestService.RejectAsync(programmeHeadId, id, request);
            return Ok(response);
        }
    }
}
