using AdviseCare.Application.Contracts.Requests.ExternalService;
using AdviseCare.Application.Contracts.Responses.ExternalService;
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

        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get external services with optional pagination",
            Description = "Retrieves external services ordered by name. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
                return Ok(await _externalServiceService.GetAllWithPaginationAsync(page.Value, pageSize.Value));

            return Ok(await _externalServiceService.GetAll());
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[All roles] Get external service by Id")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _externalServiceService.GetByIdAsync(id));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search external services",
            Description = "Filters by service name, service type, and active state. Empty filters are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchExternalServiceRequest request)
        {
            return Ok(await _externalServiceService.SearchAsync(request));
        }

        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Add an external service",
            Description = "Adds an active external service to the catalogue. Welfare Officers can read the catalogue but cannot create or update entries.")]
        public async Task<IActionResult> Create([FromBody] CreateExternalServiceRequest request)
        {
            return Ok(await _externalServiceService.CreateAsync(request));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update an external service",
            Description = "Updates an external service's name, type, contact information, and active state. Inactive services remain available for existing referral history.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateExternalServiceRequest request)
        {
            return Ok(await _externalServiceService.UpdateAsync(id, request));
        }
    }
}
