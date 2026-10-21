using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Data Protection Steward")]
    public class IndicatorController(IIndicatorService indicatorService) : ControllerBase
    {
        private readonly IIndicatorService _indicatorService = indicatorService;

        // Get every active Indicator together with its DataAttribute
        [HttpGet]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all indicators",
            Description = "Returns every active (non-archived) indicator together with its paired data attribute. " +
                "Archived indicators are left out; use search with includeArchived to see them.")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _indicatorService.GetAllAsync();
            return Ok(response);
        }

        // Get a page of active Indicators together with their DataAttribute
        [HttpGet("paged")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get a page of indicators",
            Description = "Returns active (non-archived) indicators ordered by name, one page at a time.")]
        public async Task<IActionResult> GetAllWithPagination([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var response = await _indicatorService.GetAllWithPaginationAsync(page, pageSize);
            return Ok(response);
        }

        // Get one Indicator (active or archived) by id together with its DataAttribute
        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get an indicator by id",
            Description = "Returns one indicator (active or archived) together with its paired data attribute. Returns 404 if the id does not exist.")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _indicatorService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Indicators by name/attribute name
        [HttpPost("search")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search indicators",
            Description = "Searches indicators and their paired data attribute by name. " +
                "Archived ones are excluded unless includeArchived is set on the request.")]
        public async Task<IActionResult> Search([FromBody] SearchIndicatorRequest request)
        {
            var response = await _indicatorService.SearchAsync(request);
            return Ok(response);
        }

        // Create an Indicator together with its DataAttribute
        [HttpPost]
        [SwaggerOperation(Summary = "[Data Protection Steward] Create an indicator and its data attribute",
            Description = "Creates a new indicator (name, description, data type) together with its one-to-one data attribute " +
                "(name, description) in a single transaction. Returns 409 if the indicator name or attribute name is already in use.")]
        public async Task<IActionResult> Create([FromBody] CreateIndicatorRequest request)
        {
            var response = await _indicatorService.CreateAsync(request);
            return Ok(response);
        }

        // Edit the student-facing descriptions of an Indicator/DataAttribute pair
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Update an indicator's descriptions",
            Description = "Edits the descriptions shown to students for the indicator and its paired data attribute. " +
                "The indicator name, attribute name and data type cannot be changed here. Returns 404 if the id does not exist, " +
                "409 if the indicator is archived.")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIndicatorRequest request)
        {
            var response = await _indicatorService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Archive an Indicator/DataAttribute pair that is no longer used
        [HttpPost("{id:guid}/archive")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Archive an indicator",
            Description = "Archives the indicator and its paired data attribute so they stop appearing in active lists and consent purpose offerings. " +
                "This is one-way: an archived indicator cannot be brought back. Returns 404 if the id does not exist, 409 if it is already archived.")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var response = await _indicatorService.ArchiveAsync(id);
            return Ok(response);
        }
    }
}
