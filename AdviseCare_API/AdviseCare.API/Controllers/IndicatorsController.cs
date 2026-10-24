using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IndicatorsController(IIndicatorService indicatorService) : ControllerBase
    {
        private readonly IIndicatorService _indicatorService = indicatorService;

        // Get all Indicators, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all indicators with optional pagination",
            Description = "Retrieves every indicator (active and archived), ordered by name, each with its paired data attribute. Pass page and pageSize together to page the results. " +
                "Used by the Steward's indicator list, the Admin's column mapping for indicator imports, and the student and advisor views of what can be shared.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _indicatorService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _indicatorService.GetAll());
        }

        // Get an Indicator by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get indicator by Id",
            Description = "Retrieves one indicator (active or archived) with its paired data attribute.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _indicatorService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Indicators by attribute name, indicator name, data type or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter indicators",
            Description = "Filters indicators by attribute name or indicator name (contains), by data type (numeric/categorical/boolean) and by status (active/archived). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchIndicatorRequest request)
        {
            var response = await _indicatorService.SearchAsync(request);
            return Ok(response);
        }

        // Create an Indicator together with its Data attribute
        [HttpPost]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Create indicator",
            Description = "Creates an active indicator (name, description, data type) together with its one-to-one data attribute (name, description shown to students) in one transaction. " +
                "The indicator name and the attribute name must both be unique.")]
        public async Task<IActionResult> Create([FromBody] CreateIndicatorRequest request)
        {
            var response = await _indicatorService.CreateAsync(request);
            return Ok(response);
        }

        // Update an Indicator
        [HttpPut("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Update indicator",
            Description = "Edits the descriptions of the indicator and its data attribute. The names and the data type cannot be changed, and an archived indicator cannot be edited.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateIndicatorRequest request)
        {
            var response = await _indicatorService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Archive an Indicator
        [HttpPut("{id}/archive")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Archive indicator",
            Description = "Archives the indicator and its data attribute. This is one-way: an archived indicator cannot be made active again. " +
                "The attribute can no longer be attached to a purpose, granted or requested, but existing values and consents are kept and still apply.")]
        public async Task<IActionResult> Archive([FromRoute] Guid id)
        {
            var response = await _indicatorService.ArchiveAsync(id);
            return Ok(response);
        }
    }
}
