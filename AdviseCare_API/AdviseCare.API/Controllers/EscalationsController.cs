using AdviseCare.Application.Contracts.Requests.Escalation;
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
    [Authorize(Roles = "Student Welfare Officer,Programme Head")]
    public class EscalationsController(IEscalationService escalationService) : ControllerBase
    {
        private readonly IEscalationService _escalationService = escalationService;

        // Get all Escalations of the signed-in account, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Get own escalations with optional pagination",
            Description = "Lists the escalations the signed-in account raised (Welfare Officer) or received (Programme Head), newest first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _escalationService.GetAllWithPaginationAsync(
                    accountId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _escalationService.GetAll(accountId));
        }

        // Get an Escalation by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Get escalation by Id",
            Description = "Retrieves one escalation the signed-in account raised or received; any other escalation returns 404.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Escalation not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _escalationService.GetByIdAsync(accountId, id);
            return Ok(response);
        }

        // Search Escalations
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Search/Filter own escalations",
            Description = "Filters the signed-in account's escalations by case and status (pending/responded). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchEscalationRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _escalationService.SearchAsync(accountId, request);
            return Ok(response);
        }

        // Create an Escalation
        [HttpPost]
        [Authorize(Roles = "Student Welfare Officer")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Escalate a case to a Programme Head",
            Description = "Escalates an open case that was referred to (and accepted by) the signed-in officer to an active Programme Head, with the reason. The Programme Head is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- An escalation can only be sent to an active Programme Head.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- This case has not been referred to you (or you have not accepted the referral yet).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Programme Head not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateEscalationRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _escalationService.CreateAsync(officerId, request);
            return Ok(response);
        }

        // Respond to an Escalation
        [HttpPut("{id}/respond")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Respond to an escalation",
            Description = "Answers a pending escalation addressed to the signed-in Programme Head; the status becomes responded and the Welfare Officer is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Escalation not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending escalation can be answered.", typeof(ErrorResponse))]
        public async Task<IActionResult> Respond([FromRoute] Guid id, [FromBody] RespondEscalationRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _escalationService.RespondAsync(programmeHeadId, id, request);
            return Ok(response);
        }
    }
}
