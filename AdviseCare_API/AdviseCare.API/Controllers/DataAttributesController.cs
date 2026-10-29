using AdviseCare.Application.Contracts.Requests.DataAttribute;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataAttributesController(IDataAttributeService dataAttributeService) : ControllerBase
    {
        private readonly IDataAttributeService _dataAttributeService = dataAttributeService;

        // Get all Data attributes, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all data attributes with optional pagination",
            Description = "Retrieves every data attribute (active and archived), ordered by name. Pass page and pageSize together to page the results. " +
                "Used by the Steward's attribute list, the Admin's column mapping for student data imports, and the student and advisor views of what can be shared.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _dataAttributeService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _dataAttributeService.GetAll());
        }

        // Get a Data attribute by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get data attribute by Id",
            Description = "Retrieves one data attribute (active or archived).")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _dataAttributeService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Data attributes by name, data type or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter data attributes",
            Description = "Filters data attributes by name (contains), data type (numeric/categorical/boolean) and status (active/archived). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchDataAttributeRequest request)
        {
            var response = await _dataAttributeService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Data attribute
        [HttpPost]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Create data attribute",
            Description = "Creates an active data attribute (name, description shown to students and staff, data type). The name must be unique; it is also the value the student data import matches.")]
        public async Task<IActionResult> Create([FromBody] CreateDataAttributeRequest request)
        {
            var response = await _dataAttributeService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Data attribute
        [HttpPut("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Update data attribute",
            Description = "Edits the description. The name and the data type cannot be changed, and an archived data attribute cannot be edited.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDataAttributeRequest request)
        {
            var response = await _dataAttributeService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Archive a Data attribute
        [HttpPut("{id}/archive")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Archive data attribute",
            Description = "Archives the data attribute. This is one-way: it cannot be made active again. " +
                "It can no longer be attached to a purpose, granted, requested or imported, but existing values and consents are kept and still apply.")]
        public async Task<IActionResult> Archive([FromRoute] Guid id)
        {
            var response = await _dataAttributeService.ArchiveAsync(id);
            return Ok(response);
        }
    }
}
