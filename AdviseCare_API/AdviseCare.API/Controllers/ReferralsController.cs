using AdviseCare.Application.Contracts.Requests.Referral;
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
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class ReferralsController(IReferralService referralService) : ControllerBase
    {
        private readonly IReferralService _referralService = referralService;

        // Get all Referrals, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get all referrals with optional pagination",
            Description = "Retrieves every referral, newest first, with its case, student, sender, receiver, reason, priority and status. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _referralService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _referralService.GetAll());
        }

        // Get a Referral by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get referral by Id",
            Description = "Retrieves one referral for its sender, its receiver or the staff of its case (anyone else gets 404).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Referral not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Referrals
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Search/Filter referrals",
            Description = "Filters referrals by case, priority and status, newest first. With caseId: the referrals of that case (staff of the case only). Without caseId: Programme Head only.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- A case is required. Use the incoming or outgoing list for your own referrals.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchReferralRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Referral
        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Refer my case to another advisor or to the Welfare Officer",
            Description = "toAccountId must be an active Academic Advisor, Wellbeing Advisor or Student Welfare Officer; priority = low, medium (default), high or urgent. " +
                "The case moves to referred (written to the stage history) and the receiver is notified. One pending referral per case.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- You cannot refer a case to yourself.\n- {FullName} is not an active account.\n- {FullName} is not an Academic Advisor, Wellbeing Advisor or Student Welfare Officer.\n- Referral reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the case's current advisor can refer it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Receiver account not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- This case already has a referral waiting for an answer.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateReferralRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Get the Referrals sent to me
        [HttpGet("me/incoming")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer] Get the referrals sent to me",
            Description = "Lists the referrals the signed-in account received, newest first. Optional status (pending/accepted/returned).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyIncoming([FromQuery] SearchMyReferralRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.GetMyIncomingAsync(accountId, request);
            return Ok(response);
        }

        // Get the Referrals I sent
        [HttpGet("me/outgoing")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get the referrals I sent",
            Description = "Lists the referrals the signed-in advisor sent, newest first. Optional status (pending/accepted/returned).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyOutgoing([FromQuery] SearchMyReferralRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.GetMyOutgoingAsync(accountId, request);
            return Ok(response);
        }

        // Accept a Referral
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer] Accept a pending referral sent to me",
            Description = "An advisor receiver becomes the case's advisor (stage referred -> assigned) and the student gets the new contact information. " +
                "A Welfare Officer receiver works on the case while the advisor keeps it (stage stays referred, WF2b step 6). The sender is notified.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the receiver can answer a referral.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.\n- Referral not found.\n- Case not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The referral was already {Status}.", typeof(ErrorResponse))]
        public async Task<IActionResult> Accept([FromRoute] Guid id)
        {
            var receiverId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.AcceptAsync(receiverId, id);
            return Ok(response);
        }

        // Return a Referral
        [HttpPut("{id}/return")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer] Return a pending referral sent to me",
            Description = "Returns the referral with a reason: the case goes back to the stage it had before the referral, the reason is written to the stage history and sent to the sender.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the receiver can answer a referral.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.\n- Referral not found.\n- Case not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The referral was already {Status}.", typeof(ErrorResponse))]
        public async Task<IActionResult> Return([FromRoute] Guid id, [FromBody] ReturnReferralRequest request)
        {
            var receiverId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _referralService.ReturnAsync(receiverId, id, request);
            return Ok(response);
        }
    }
}
