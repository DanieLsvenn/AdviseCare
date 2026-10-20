using AdviseCare.Application.Contracts.Requests.Programme;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProgrammesController(IProgrammeService programmeService) : ControllerBase
    {
        private readonly IProgrammeService _programmeService = programmeService;

        // Get all Programmes, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all programmes with optional pagination",
            Description = "Retrieves every programme, ordered by programme code. Pass page and pageSize together to page the results. Programmes are a simple reference list for now and are not used by other tables.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _programmeService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _programmeService.GetAll());
        }

        // Get a Programme by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get programme by Id",
            Description = "Retrieves one programme by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _programmeService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Programmes by faculty, code, name or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter programmes",
            Description = "Filters programmes by faculty, by code or name (contains) and by status (active/archived). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchProgrammeRequest request)
        {
            var response = await _programmeService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Programme
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create programme",
            Description = "Creates a new active programme under an existing faculty. The programme code must be unique.")]
        public async Task<IActionResult> Create([FromBody] CreateProgrammeRequest request)
        {
            var response = await _programmeService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Programme
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update programme",
            Description = "Updates a programme's faculty, code, name and status (active/archived).")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProgrammeRequest request)
        {
            var response = await _programmeService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
