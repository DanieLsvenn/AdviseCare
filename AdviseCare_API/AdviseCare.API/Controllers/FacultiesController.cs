using AdviseCare.Application.Contracts.Requests.Faculty;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FacultiesController(IFacultyService facultyService) : ControllerBase
    {
        private readonly IFacultyService _facultyService = facultyService;

        // Get all Faculties, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all faculties with optional pagination",
            Description = "Retrieves every faculty, ordered by faculty code. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _facultyService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _facultyService.GetAll());
        }

        // Get a Faculty by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get faculty by Id",
            Description = "Retrieves one faculty by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _facultyService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Faculties by code, name or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter faculties",
            Description = "Filters faculties by code or name (contains) and by status (active/archived). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchFacultyRequest request)
        {
            var response = await _facultyService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Faculty
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create faculty",
            Description = "Creates a new active faculty. The faculty code must be unique; it is the code used in the account import file.")]
        public async Task<IActionResult> Create([FromBody] CreateFacultyRequest request)
        {
            var response = await _facultyService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Faculty
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update faculty",
            Description = "Updates a faculty's code, name and status. Setting the status to archived retires a discontinued faculty without deleting its history; archived faculties cannot receive new accounts.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateFacultyRequest request)
        {
            var response = await _facultyService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
