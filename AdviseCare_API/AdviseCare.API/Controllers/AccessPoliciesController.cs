using AdviseCare.Application.Contracts.Requests.AccessPolicy;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Data Protection Steward")]
    public class AccessPoliciesController(IAccessPolicyService accessPolicyService) : ControllerBase
    {
        private readonly IAccessPolicyService _accessPolicyService = accessPolicyService;

        // Get all Access policies, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all access policies with optional pagination",
            Description = "Retrieves every access policy (draft, active and revoked) with its role, purpose and creator, ordered by role then purpose. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _accessPolicyService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _accessPolicyService.GetAll());
        }

        // Get an Access policy by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get access policy by Id",
            Description = "Retrieves one access policy with its role, purpose and creator.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _accessPolicyService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Access policies by purpose, role or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter access policies",
            Description = "Filters access policies by purpose (consentPurposeId), by role (roleId, e.g. every policy affecting the Wellbeing Advisor) and by status (draft/active/revoked). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchAccessPolicyRequest request)
        {
            var response = await _accessPolicyService.SearchAsync(request);
            return Ok(response);
        }

        // Create an Access policy
        [HttpPost]
        [SwaggerOperation(Summary = "[Data Protection Steward] Create access policy",
            Description = "Links a role to a consent purpose: once active, accounts with that role may read the attributes a student has granted under that purpose. " +
                "The policy starts as a draft. The purpose must not be retired, the role cannot be Student, and a role can have only one draft/active policy per purpose.")]
        public async Task<IActionResult> Create([FromBody] CreateAccessPolicyRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accessPolicyService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update an Access policy
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Update draft access policy",
            Description = "Changes the role or purpose of a draft policy. Active and revoked policies cannot be edited: revoke the policy and create a new one instead.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAccessPolicyRequest request)
        {
            var response = await _accessPolicyService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Activate an Access policy
        [HttpPut("{id}/activate")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Activate access policy",
            Description = "Sets a draft policy to active, so it is enforced. Access is only given while its purpose is also active.")]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            var response = await _accessPolicyService.ActivateAsync(id);
            return Ok(response);
        }

        // Revoke an Access policy
        [HttpPut("{id}/revoke")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Revoke access policy",
            Description = "Revokes a draft or active policy. This is one-way and policies are never deleted, so the history of who could see what stays visible.")]
        public async Task<IActionResult> Revoke([FromRoute] Guid id)
        {
            var response = await _accessPolicyService.RevokeAsync(id);
            return Ok(response);
        }

        // Test an Access policy against a sample student
        [HttpPost("{id}/test")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Test access policy against a sample student",
            Description = "Shows, for every attribute of the policy's purpose, whether the student has granted consent, whether a value exists, and the result for the policy's role: " +
                "allowed, allowedOnceActive (consented, but the policy or purpose is not active yet) or notShared. Values are never returned and nothing is written to the access log.")]
        public async Task<IActionResult> Test([FromRoute] Guid id, [FromBody] TestAccessPolicyRequest request)
        {
            var response = await _accessPolicyService.TestAsync(id, request);
            return Ok(response);
        }
    }
}
