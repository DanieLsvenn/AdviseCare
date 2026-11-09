using AdviseCare.Application.Contracts.Requests.CaseNote;
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
    public class CaseNotesController(ICaseNoteService caseNoteService) : ControllerBase
    {
        private readonly ICaseNoteService _caseNoteService = caseNoteService;

        // Get all my Case notes, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get all my case notes with optional pagination",
            Description = "Lists the notes the signed-in advisor wrote, newest first, drafts and finalized. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseNoteService.GetAllWithPaginationAsync(
                    authorId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseNoteService.GetAll(authorId));
        }

        // Get a Case note by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get case note by Id",
            Description = "A draft is visible only to its author; a finalized note to the staff of the case (its advisor, the Programme Head, and anyone who sent or received a referral of it). Students never read case notes.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case note not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseNoteService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Case notes of a case
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get the notes of a case",
            Description = "caseId is required; isFinalized is optional. Lists the case's finalized notes plus the caller's own drafts, oldest first (staff of the case only), e.g. the Welfare Officer reading the advisor's notes (WF2b step 2.1).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchCaseNoteRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseNoteService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Case note
        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Write a draft note on my case",
            Description = "Only the case's current advisor can write notes, and not on a closed case. The note starts as a draft (isFinalized = false) that only its author can see and edit.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Note text is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the case's current advisor can write case notes.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Case note not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed. Reopen it to add notes.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateCaseNoteRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseNoteService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update a Case note
        [HttpPut("{id}")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Edit my draft note",
            Description = "Changes the text of the author's own draft and sets editedAt. A finalized note can never change (409).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Note text is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case note not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A finalized note cannot be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCaseNoteRequest request)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseNoteService.UpdateAsync(authorId, id, request);
            return Ok(response);
        }

        // Finalize a Case note
        [HttpPut("{id}/finalize")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Finalize my draft note",
            Description = "After this the note is visible to the staff of the case and can never change again.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Case note not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A finalized note cannot be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Finalize([FromRoute] Guid id)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseNoteService.FinalizeAsync(authorId, id);
            return Ok(response);
        }
    }
}
