using AdviseCare.Application.Contracts.Requests.AttributeKey;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Data Protection Steward")]
    public class AttributeKeysController(IAttributeKeyService attributeKeyService) : ControllerBase
    {
        private readonly IAttributeKeyService _attributeKeyService = attributeKeyService;

        // Get all Attribute keys, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all attribute keys with optional pagination",
            Description = "Lists the encryption keys (one per student per data attribute), newest first, with the student, the attribute, when the key was created and whether it was destroyed by an erasure. " +
                "The key itself is never returned. Keys are created automatically when a student's first value for an attribute is stored. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _attributeKeyService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _attributeKeyService.GetAll());
        }

        // Get an Attribute key by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get attribute key by Id",
            Description = "Retrieves the status of one attribute key (active/destroyed). The key itself is never returned.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _attributeKeyService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Attribute keys by attribute, student or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter attribute keys",
            Description = "Filters attribute keys by data attribute, student and status (active/destroyed), e.g. to confirm an erasure destroyed the key. Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchAttributeKeyRequest request)
        {
            var response = await _attributeKeyService.SearchAsync(request);
            return Ok(response);
        }
    }
}
