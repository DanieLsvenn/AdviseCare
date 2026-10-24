using AdviseCare.Application.Contracts.Requests.Role;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesController(IRoleService roleService) : ControllerBase
    {
        private readonly IRoleService _roleService = roleService;

        // Get all Roles, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Get all roles with optional pagination",
            Description = "Retrieves every role (System Administrator, Data Protection Steward, Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Student, Programme Head), ordered by name. Pass page and pageSize together to page the results. The Steward uses it to pick the role of an access policy.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _roleService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _roleService.GetAll());
        }

        // Get a Role by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Get role by Id",
            Description = "Retrieves one role by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _roleService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Roles by role name
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Search/Filter roles",
            Description = "Searches roles whose name contains the given text. An empty request returns every role.")]
        public async Task<IActionResult> Search([FromQuery] SearchRoleRequest request)
        {
            var response = await _roleService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Role
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create role",
            Description = "Creates a new role with a unique name. A new role has no access to any endpoint until the code authorizes it by name.")]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
        {
            var response = await _roleService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Role
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update role",
            Description = "Renames a role. Endpoints authorize by the exact role name, so renaming one of the 7 built-in roles locks its users out of their endpoints until the code is changed too.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
        {
            var response = await _roleService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
