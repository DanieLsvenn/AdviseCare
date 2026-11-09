using AdviseCare.Application.Contracts.Requests.CaseMessage;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Case messages are append-only: there is no update or delete
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class CaseMessagesController(ICaseMessageService caseMessageService) : ControllerBase
    {
        private readonly ICaseMessageService _caseMessageService = caseMessageService;

        // Get all my Case messages, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Get all my case messages with optional pagination",
            Description = "Lists the messages the signed-in account wrote on any case, newest first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseMessageService.GetAllWithPaginationAsync(
                    authorId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseMessageService.GetAll(authorId));
        }

        // Get a Case message by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get case message by Id",
            Description = "Visible to the student of the case, its current advisor, the Programme Head, and anyone who sent or received a referral of the case.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case message not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Case messages of a case
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get the message thread of a case",
            Description = "caseId is required. Returns every message of the case, oldest first, with its author and authorRole (student/advisor). " +
                "Same readers as GET /api/CaseMessages/{id}.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchCaseMessageRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Case message
        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Write a message on a case",
            Description = "Only the student of the case and its current advisor can write (1-1000 characters), and not on a closed case; a student can write once an advisor is assigned. " +
                "Messages cannot be edited or deleted. The other side is notified. Private advisor records stay in case notes.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Message text is required.\n- Message text cannot exceed 1000 characters.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the student of the case and its current advisor can write messages.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Case message not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed. Messages can be written again if the advisor reopens it.\n- Your case has no advisor yet. You can write once an advisor is assigned.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateCaseMessageRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.CreateAsync(creatorId, request);
            return Ok(response);
        }
    }
}
