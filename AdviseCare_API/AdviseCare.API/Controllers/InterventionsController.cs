using AdviseCare.Application.Contracts.Requests.Intervention;
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
    [Authorize(Roles = "Student Welfare Officer")]
    public class InterventionsController(IInterventionService interventionService) : ControllerBase
    {
        private readonly IInterventionService _interventionService = interventionService;

        // Get all Interventions, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get all interventions with optional pagination",
            Description = "Lists the welfare interventions, newest first, with their case, student, officer, type, notes, outcome and status. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _interventionService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _interventionService.GetAll());
        }

        // Get an Intervention by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get intervention by Id",
            Description = "Retrieves one intervention.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Intervention not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _interventionService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Interventions
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Search/Filter interventions",
            Description = "Filters interventions by case, student (the student's intervention history) and status (scheduled/inProgress/completed/cancelled). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchInterventionRequest request)
        {
            var response = await _interventionService.SearchAsync(request);
            return Ok(response);
        }

        // Create an Intervention
        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Log an intervention",
            Description = "Logs an intervention (type, date, notes, status scheduled or inProgress) for an open case that was referred to (and accepted by) the signed-in officer.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- This case has not been referred to you (or you have not accepted the referral yet).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _interventionService.CreateAsync(officerId, request);
            return Ok(response);
        }

        // Update an Intervention
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Update an intervention",
            Description = "Edits the notes, the outcome so far, the date, the type and the status (scheduled/inProgress). Only the officer who logged it can change it, and only until it is closed.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the Welfare Officer who logged the intervention can change it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Intervention not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A closed intervention cannot be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _interventionService.UpdateAsync(officerId, id, request);
            return Ok(response);
        }

        // Close an Intervention
        [HttpPut("{id}/close")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Close an intervention",
            Description = "Records the final outcome and completes the intervention (no further changes). The case's advisor is notified to continue the follow-up and closure.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the Welfare Officer who logged the intervention can change it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Intervention not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A closed intervention cannot be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> Close([FromRoute] Guid id, [FromBody] CloseInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _interventionService.CloseAsync(officerId, id, request);
            return Ok(response);
        }
    }
}
