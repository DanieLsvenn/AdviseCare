using AdviseCare.Application.Contracts.Requests.ExternalService;
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
    public class ExternalServicesController(IExternalServiceService externalServiceService) : ControllerBase
    {
        private readonly IExternalServiceService _externalServiceService = externalServiceService;

        // Get all External services, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all external services with optional pagination",
            Description = "Retrieves the catalogue of outside services (banks, lending services...) ordered by name, active and inactive. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _externalServiceService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _externalServiceService.GetAll());
        }

        // Get an External service by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get external service by Id",
            Description = "Retrieves one external service with its contact information.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- External service not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _externalServiceService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search External services
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter external services",
            Description = "Filters external services by name or type (contains) and active state, e.g. isActive=true for the services a new referral can go to. Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchExternalServiceRequest request)
        {
            var response = await _externalServiceService.SearchAsync(request);
            return Ok(response);
        }

        // Create an External service
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create external service",
            Description = "Adds an active service (name, type, contact information) to the catalogue. Welfare Officers read the catalogue but cannot change it.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateExternalServiceRequest request)
        {
            var response = await _externalServiceService.CreateAsync(request);
            return Ok(response);
        }

        // Update an External service
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update external service",
            Description = "Edits a service's name, type, contact information and active state. An inactive service stays in old referrals but cannot receive new ones.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- External service not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateExternalServiceRequest request)
        {
            var response = await _externalServiceService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
