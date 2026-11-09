using AdviseCare.Application.Contracts.Requests.SupportCategory;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupportCategoriesController(ISupportCategoryService supportCategoryService) : ControllerBase
    {
        private readonly ISupportCategoryService _supportCategoryService = supportCategoryService;

        // Get all Support categories, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all support categories with optional pagination",
            Description = "Retrieves every support category with its governing consent purpose, ordered by support type, then name. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _supportCategoryService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _supportCategoryService.GetAll());
        }

        // Get a Support category by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get support category by Id",
            Description = "Retrieves one support category, including the name and status of the consent purpose that governs which data a student may share with a request in it.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support category not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _supportCategoryService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Support categories
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter support categories",
            Description = "Filters support categories by name (contains), governing consent purpose, support type (academic/wellbeing) and status (active/archived). Empty fields are ignored. " +
                "The request form uses ?supportType=academic&status=active (WF2 step 2.2).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchSupportCategoryRequest request)
        {
            var response = await _supportCategoryService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Support category
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create support category",
            Description = "Creates an active support category. The support type (academic/wellbeing) decides which advisor role handles its cases; the consent purpose (not retired) decides which data attributes a student may share with a request. " +
                "Names are unique within a support type.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- '{PurposeName}' is retired and cannot govern a support category.\n- '{PurposeName}' is the statistics purpose and cannot govern a support category.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support category not found.\n- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A {SupportType} support category named '{CategoryName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateSupportCategoryRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _supportCategoryService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update a Support category
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update support category",
            Description = "Updates the name, description, support type, governing consent purpose and status of a support category. Setting the status to archived hides it from new requests while existing cases keep it. " +
                "The support type cannot change once cases use the category.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- '{PurposeName}' is retired and cannot govern a support category.\n- '{PurposeName}' is the statistics purpose and cannot govern a support category.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support category not found.\n- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The support type cannot change because cases already use this category. Archive it and create a new category instead.\n- A {SupportType} support category named '{CategoryName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSupportCategoryRequest request)
        {
            var response = await _supportCategoryService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Get the data attributes a student may share with a request in a Support category
        [HttpGet("{id}/shareable-attributes")]
        [SwaggerOperation(Summary = "[All roles] Get the data attributes a student may share with a request in this category",
            Description = "Lists the active data attributes of the category's consent purpose (WF2 step 2.4.1); empty with canShare = false when the purpose is not active. " +
                "For a student, alreadyShared marks the attributes they already granted under that purpose.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support category not found.\n- The consent purpose of this category was not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetShareableAttributes([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _supportCategoryService.GetShareableAttributesAsync(accountId, User.IsInRole("Student"), id);
            return Ok(response);
        }
    }
}
