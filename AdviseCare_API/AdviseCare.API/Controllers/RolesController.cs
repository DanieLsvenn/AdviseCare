using AdviseCare.Application.Contracts.Requests.Role;
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
    public class RolesController(IRoleService roleService) : ControllerBase
    {
        private readonly IRoleService _roleService = roleService;

        // Get all Roles, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Get all roles with optional pagination",
            Description = "Retrieves every role (System Administrator, Data Protection Steward, Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Student, Programme Head), ordered by name. Pass page and pageSize together to page the results. The Steward uses it to pick the role of an access policy.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Role not found.", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A role with name '{RoleName}' already exists.", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Role not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A role with name '{RoleName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
        {
            var response = await _roleService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
