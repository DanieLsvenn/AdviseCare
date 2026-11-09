using AdviseCare.Application.Contracts.Requests.SupportGrant;
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
    public class SupportGrantsController(ISupportGrantService supportGrantService) : ControllerBase
    {
        private readonly ISupportGrantService _supportGrantService = supportGrantService;

        // Get all Support grants, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get all support grants with optional pagination",
            Description = "Lists the support grants, newest first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _supportGrantService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _supportGrantService.GetAll());
        }

        // Get a Support grant by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get support grant by Id",
            Description = "Retrieves one support grant.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support grant not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _supportGrantService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Support grants
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Search/Filter support grants",
            Description = "Filters support grants by intervention and status (issued/revoked). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchSupportGrantRequest request)
        {
            var response = await _supportGrantService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Support grant
        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Record a support grant",
            Description = "Records a grant (type, description, issue date) under one of the signed-in officer's own open interventions.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the Welfare Officer of the intervention can record a grant under it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Intervention not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A support grant cannot be added to a closed intervention.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateSupportGrantRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _supportGrantService.CreateAsync(officerId, request);
            return Ok(response);
        }

        // Revoke a Support grant
        [HttpPut("{id}/revoke")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Revoke a support grant",
            Description = "Marks an issued grant as revoked. Only the officer of its intervention can revoke it.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the Welfare Officer of the intervention can revoke its grants.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support grant not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This support grant is already revoked.", typeof(ErrorResponse))]
        public async Task<IActionResult> Revoke([FromRoute] Guid id)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _supportGrantService.RevokeAsync(officerId, id);
            return Ok(response);
        }
    }
}
