using AdviseCare.Application.Contracts.Requests.ConsentRequest;
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
    [Authorize]
    public class ConsentRequestsController(IConsentRequestService consentRequestService) : ControllerBase
    {
        private readonly IConsentRequestService _consentRequestService = consentRequestService;

        // Get all Consent requests, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all consent requests with optional pagination",
            Description = "Retrieves every advisor request for extra consent, newest first, with the advisor, student, purpose and attribute. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _consentRequestService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _consentRequestService.GetAll());
        }

        // Get a Consent request by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get consent request by Id",
            Description = "Retrieves one consent request.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent request not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _consentRequestService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Consent requests by advisor, purpose, attribute, student or status
        [HttpGet("search")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter consent requests",
            Description = "Filters consent requests by advisor, purpose, attribute, student and status (pending/granted/denied/withdrawn). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchConsentRequestRequest request)
        {
            var response = await _consentRequestService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Consent request (current advisor)
        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Request consent from a student",
            Description = "Asks a student to share one data attribute under one consent purpose, with the reason shown to the student. The student is notified. " +
                "The purpose must be active and cover the attribute, the attribute must be active, one of the advisor's roles needs an active access policy for the purpose, " +
                "the student must not already share it, and the advisor cannot have a second pending request for the same thing.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Consent can only be requested from a student.\n- '{PurposeName}' is the statistics purpose; advisors cannot request consent for it.\n- This data attribute is not covered by '{PurposeName}'.\n- None of your roles has an active access policy under '{PurposeName}', so a granted request would not give you access.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Consent purpose not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- '{PurposeName}' is not active.\n- '{AttributeName}' is archived and cannot be requested.\n- The student already shares '{AttributeName}' under '{PurposeName}'.\n- You already have a pending request for this attribute.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateConsentRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.CreateAsync(advisorId, request);
            return Ok(response);
        }

        // Update an own pending Consent request (current advisor)
        [HttpPut("{id}")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Edit my consent request",
            Description = "Rewords the stated purpose of one of the advisor's own pending requests. Returns 404 for another advisor's request.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending request can be edited.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateConsentRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.UpdateAsync(advisorId, id, request);
            return Ok(response);
        }

        // Grant a received Consent request (current Student)
        [HttpPut("{id}/grant")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Grant a consent request",
            Description = "Shares the requested attribute under the request's purpose: a consent and its history row are created and the advisor is notified. Returns 404 for a request sent to another student.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- The expiry date must be in the future.\n- Some data attributes are not covered by '{PurposeName}'.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only students can grant consent.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent request not found.\n- Consent purpose not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This request has already been answered or withdrawn.\n- '{PurposeName}' is not active, so consent cannot be given for it.\n- These data attributes are archived and cannot be shared any more: {string.Join(", typeof(ErrorResponse))]
        public async Task<IActionResult> Grant([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.GrantAsync(studentId, id);
            return Ok(response);
        }

        // Deny a received Consent request (current Student)
        [HttpPut("{id}/deny")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Deny a consent request",
            Description = "Refuses the request; the advisor is notified and keeps seeing the attribute as \"exists, not shared\".")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent request not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This request has already been answered or withdrawn.", typeof(ErrorResponse))]
        public async Task<IActionResult> Deny([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.DenyAsync(studentId, id);
            return Ok(response);
        }

        // Withdraw an own pending Consent request (current advisor)
        [HttpPut("{id}/withdraw")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Withdraw my consent request",
            Description = "Takes back one of the advisor's own requests while it is still pending.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending request can be withdrawn.", typeof(ErrorResponse))]
        public async Task<IActionResult> Withdraw([FromRoute] Guid id)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.WithdrawAsync(advisorId, id);
            return Ok(response);
        }

        // Get the Consent requests the current Student received
        [HttpGet("me/received")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get consent requests sent to me",
            Description = "Lists every consent request the signed-in student received, newest first, with the advisor, purpose, attribute, stated reason and status.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyReceived()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.GetMyReceivedRequestsAsync(studentId);
            return Ok(response);
        }

        // Get the Consent requests the current advisor sent
        [HttpGet("me/sent")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get my sent consent requests",
            Description = "Lists every consent request the signed-in advisor sent, newest first, with its status (pending/granted/denied/withdrawn).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMySent()
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentRequestService.GetMySentRequestsAsync(advisorId);
            return Ok(response);
        }
    }
}
