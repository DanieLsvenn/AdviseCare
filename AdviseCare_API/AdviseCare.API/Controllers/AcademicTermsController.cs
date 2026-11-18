using AdviseCare.Application.Contracts.Requests.AcademicTerm;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicTermsController(IAcademicTermService academicTermService) : ControllerBase
    {
        private readonly IAcademicTermService _academicTermService = academicTermService;

        // Get all Academic terms, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all academic terms with optional pagination",
            Description = "Retrieves every academic term, newest start date first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _academicTermService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _academicTermService.GetAll());
        }

        // Get a Academic term by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get term by Id",
            Description = "Retrieves one academic term by its unique identifier.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Academic term not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _academicTermService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Academic terms by name or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter academic terms",
            Description = "Filters academic terms by name (contains) and by status (upcoming/current/past). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchAcademicTermRequest request)
        {
            var response = await _academicTermService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Academic term
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create term",
            Description = "Creates an academic term (name, start date, end date, status; status defaults to upcoming). The end date must be after the start date. Only one term can be current: creating a current term moves the previous current term to past.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Start date and end date are required.\n- End date must be after start date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A term named '{termName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateAcademicTermRequest request)
        {
            var response = await _academicTermService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Academic term
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update term",
            Description = "Updates an academic term's name, dates and status. The end date must be after the start date. Setting a term to current moves the previous current term to past.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Start date and end date are required.\n- End date must be after start date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Academic term not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A term named '{termName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAcademicTermRequest request)
        {
            var response = await _academicTermService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
