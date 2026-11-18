using AdviseCare.Application.Contracts.Requests.ConsentPurpose;
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
    public class ConsentPurposesController(IConsentPurposeService consentPurposeService) : ControllerBase
    {
        private readonly IConsentPurposeService _consentPurposeService = consentPurposeService;

        // Get all Consent purposes, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all consent purposes with optional pagination",
            Description = "Retrieves every consent purpose (active, inactive and retired), ordered by name, with the steward who created it. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _consentPurposeService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _consentPurposeService.GetAll());
        }

        // Get a Consent purpose by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get consent purpose by Id",
            Description = "Retrieves one consent purpose with the data attributes it covers (name, description shown to students, status). " +
                "The support request form uses it to list the attributes a student may share for a category.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _consentPurposeService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Consent purposes by name or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter consent purposes",
            Description = "Filters consent purposes by name (contains) and by status (active/inactive/retired). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchConsentPurposeRequest request)
        {
            var response = await _consentPurposeService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Consent purpose
        [HttpPost]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Create consent purpose",
            Description = "Creates a consent purpose (name, description shown to students). It starts inactive: attach its data attributes, then activate it. The name must be unique.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A consent purpose named '{purposeName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateConsentPurposeRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentPurposeService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update a Consent purpose
        [HttpPut("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Update consent purpose",
            Description = "Edits the name and description of a consent purpose. A retired purpose cannot be edited. Status changes have their own endpoints below.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A retired consent purpose cannot be edited.\n- A consent purpose named '{purposeName}' already exists.\n- Another active consent purpose is already the statistics purpose; deactivate it first.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateConsentPurposeRequest request)
        {
            var response = await _consentPurposeService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Activate a Consent purpose
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Activate consent purpose",
            Description = "Sets an inactive purpose to active, so students can grant consent for it and access policies on it take effect. The purpose needs at least one active data attribute; a retired purpose cannot be activated.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Attach at least one active data attribute before activating the purpose.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A retired consent purpose cannot be brought back.\n- This consent purpose is already active.\n- Another active consent purpose is already the statistics purpose; deactivate it first.", typeof(ErrorResponse))]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            var response = await _consentPurposeService.ActivateAsync(id);
            return Ok(response);
        }

        // Deactivate a Consent purpose
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Deactivate consent purpose",
            Description = "Sets an active purpose to inactive: no new consents can be given and staff get no access through it, but the consents already given stay on record and apply again once it is reactivated.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only an active consent purpose can be deactivated.", typeof(ErrorResponse))]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            var response = await _consentPurposeService.DeactivateAsync(id);
            return Ok(response);
        }

        // Retire a Consent purpose
        [HttpPut("{id}/retire")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Retire consent purpose",
            Description = "Retires a purpose that is no longer used. This is one-way: it is never deleted (historical consents keep pointing to it) and cannot be brought back.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This consent purpose is already retired.", typeof(ErrorResponse))]
        public async Task<IActionResult> Retire([FromRoute] Guid id)
        {
            var response = await _consentPurposeService.RetireAsync(id);
            return Ok(response);
        }

        // Get all purpose-attribute links (PURPOSE_ATTRIBUTE), with optional pagination
        [HttpGet("purpose-attributes")]
        [SwaggerOperation(Summary = "[All roles] Get all purpose-attribute links with optional pagination",
            Description = "Retrieves every link between a consent purpose and a data attribute it covers, with both names and statuses. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAllPurposeAttribute([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _consentPurposeService.GetAllPurposeAttributeWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _consentPurposeService.GetAllPurposeAttributeAsync());
        }

        // Get a purpose-attribute link (PURPOSE_ATTRIBUTE) by purpose Id and attribute Id
        [HttpGet("purpose-attributes/{consentPurposeId}/{dataAttributeId}")]
        [SwaggerOperation(Summary = "[All roles] Get purpose-attribute link",
            Description = "Retrieves the link between one consent purpose and one data attribute. Returns 404 when the attribute is not attached to that purpose.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.\n- This data attribute is not attached to the purpose.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetPurposeAttributeById([FromRoute] Guid consentPurposeId, [FromRoute] Guid dataAttributeId)
        {
            var response = await _consentPurposeService.GetPurposeAttributeByIdAsync(consentPurposeId, dataAttributeId);
            return Ok(response);
        }

        // Search purpose-attribute links (PURPOSE_ATTRIBUTE) by purpose or attribute
        [HttpGet("purpose-attributes/search")]
        [SwaggerOperation(Summary = "[All roles] Search purpose-attribute links",
            Description = "Filters purpose-attribute links by purpose (consentPurposeId) and/or attribute (dataAttributeId), e.g. to see every purpose that covers one attribute. Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        public async Task<IActionResult> SearchPurposeAttribute([FromQuery] SearchPurposeAttributeRequest request)
        {
            var response = await _consentPurposeService.SearchPurposeAttributeAsync(request);
            return Ok(response);
        }

        // Attach a Data attribute to a Consent purpose (PURPOSE_ATTRIBUTE)
        [HttpPost("purpose-attributes")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Attach a data attribute to a consent purpose",
            Description = "Adds a data attribute to the attributes a purpose covers. An attribute can belong to several purposes. The attribute must be active, the purpose must not be retired, and the same link cannot be added twice.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.\n- Data attribute not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The attributes of a retired consent purpose cannot be changed.\n- An archived data attribute cannot be attached to a purpose.\n- '{AttributeName}' is already attached to '{PurposeName}'.", typeof(ErrorResponse))]
        public async Task<IActionResult> CreatePurposeAttribute([FromBody] CreatePurposeAttributeRequest request)
        {
            var response = await _consentPurposeService.CreatePurposeAttributeAsync(request);
            return Ok(response);
        }

        // Remove a Data attribute from a Consent purpose (PURPOSE_ATTRIBUTE)
        [HttpDelete("purpose-attributes/{consentPurposeId}/{dataAttributeId}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Remove a data attribute from a consent purpose",
            Description = "Removes a data attribute from a purpose, so it is no longer offered for that purpose. Consents students already gave for it stay on record but no longer give access through this purpose. The purpose must not be retired.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Consent purpose not found.\n- This data attribute is not attached to the purpose.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The attributes of a retired consent purpose cannot be changed.", typeof(ErrorResponse))]
        public async Task<IActionResult> DeletePurposeAttribute([FromRoute] Guid consentPurposeId, [FromRoute] Guid dataAttributeId)
        {
            var response = await _consentPurposeService.DeletePurposeAttributeAsync(consentPurposeId, dataAttributeId);
            return Ok(response);
        }
    }
}
