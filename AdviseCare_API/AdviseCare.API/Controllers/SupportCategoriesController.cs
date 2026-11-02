using AdviseCare.Application.Contracts.Requests.SupportCategory;
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

        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all support categories with optional pagination",
            Description = "Ordered by support type, then name. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            return Ok(await _supportCategoryService.GetAllAsync(page, pageSize));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search support categories",
            Description = "Filters: supportType (academic/wellbeing), status (active/archived), categoryName (contains), consentPurposeId. " +
                "The student's request form uses ?supportType=academic&status=active.")]
        public async Task<IActionResult> Search([FromQuery] SearchSupportCategoryRequest request)
        {
            return Ok(await _supportCategoryService.SearchAsync(request));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[All roles] Get a support category by id", Description = "Includes the governing consent purpose's name and status.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _supportCategoryService.GetByIdAsync(id));
        }

        [HttpGet("{id:guid}/shareable-attributes")]
        [SwaggerOperation(Summary = "[All roles] List the data attributes a student may share with a request in this category",
            Description = "The active attributes of the category's consent purpose (WF2 step 2.4.1). Empty with canShare = false when the purpose is not active. " +
                "For a Student, alreadyShared tells which attributes already have a granted consent for this purpose.")]
        public async Task<IActionResult> GetShareableAttributes([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _supportCategoryService.GetShareableAttributesAsync(id, accountId, User.IsInRole("Student")));
        }

        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create a support category",
            Description = "supportType is academic or wellbeing; consentPurposeId is the purpose that governs which data a student may share (must not be retired). " +
                "Names are unique within a support type. createdBy is taken from the token.")]
        public async Task<IActionResult> Create([FromBody] CreateSupportCategoryRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _supportCategoryService.CreateAsync(accountId, request));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Edit or archive a support category",
            Description = "Send every field; status = archived hides it from new requests (existing cases keep it). " +
                "The support type cannot change once cases use the category.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSupportCategoryRequest request)
        {
            return Ok(await _supportCategoryService.UpdateAsync(id, request));
        }
    }
}
