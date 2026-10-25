using AdviseCare.Application.Contracts.Requests.EmergencyAccessRequest;
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
    public class EmergencyAccessRequestsController(IEmergencyAccessRequestService emergencyAccessRequestService) : ControllerBase
    {
        private readonly IEmergencyAccessRequestService _emergencyAccessRequestService = emergencyAccessRequestService;

        // Get all Emergency access requests, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all emergency access requests with optional pagination",
            Description = "Retrieves every emergency access request (pending review feature), newest first, with the requester, the student, the justification and the decision. " +
                "isExpired is true once an approved access window has passed. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _emergencyAccessRequestService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _emergencyAccessRequestService.GetAll());
        }

        // Get an Emergency access request by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get emergency access request by Id",
            Description = "Retrieves one emergency access request with the requester's justification.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _emergencyAccessRequestService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Emergency access requests by requester, deciding steward, student or status
        [HttpGet("search")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter emergency access requests",
            Description = "Filters requests by requester (requestedBy), deciding steward (stewardId), student and status (pending/approved/rejected). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchEmergencyAccessRequestRequest request)
        {
            var response = await _emergencyAccessRequestService.SearchAsync(request);
            return Ok(response);
        }

        // Create an Emergency access request (current staff member)
        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Request emergency access",
            Description = "Asks for access to a student's record beyond the student's consent, with a justification. Every Data Protection Steward is notified; they share one pool of requests and the first to act decides. " +
                "Only one pending request or running access per requester and student.")]
        public async Task<IActionResult> Create([FromBody] CreateEmergencyAccessRequestRequest request)
        {
            var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _emergencyAccessRequestService.CreateAsync(requesterId, request);
            return Ok(response);
        }

        // Update an own pending Emergency access request (current staff member)
        [HttpPut("{id}")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Edit my emergency access request",
            Description = "Rewords the justification of one of the signed-in staff member's own pending requests. Returns 404 for another staff member's request.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateEmergencyAccessRequestRequest request)
        {
            var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _emergencyAccessRequestService.UpdateAsync(requesterId, id, request);
            return Ok(response);
        }

        // Approve an Emergency access request
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Approve emergency access",
            Description = "Approves a pending request until scopeExpiresAt (must be in the future); the access ends on its own after that. The signed-in steward is recorded as the decider. " +
                "Returns 409 when another steward has already decided the request.")]
        public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ApproveEmergencyAccessRequestRequest request)
        {
            var stewardId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _emergencyAccessRequestService.ApproveAsync(stewardId, id, request);
            return Ok(response);
        }

        // Reject an Emergency access request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Reject emergency access",
            Description = "Rejects a pending request; the requester is notified and the signed-in steward is recorded as the decider. Returns 409 when another steward has already decided the request.")]
        public async Task<IActionResult> Reject([FromRoute] Guid id)
        {
            var stewardId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _emergencyAccessRequestService.RejectAsync(stewardId, id);
            return Ok(response);
        }

        // Get the Emergency access requests of the current staff member
        [HttpGet("me")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get my emergency access requests",
            Description = "Lists the signed-in staff member's emergency access requests, newest first, with their status and access window.")]
        public async Task<IActionResult> GetMine()
        {
            var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _emergencyAccessRequestService.GetMyRequestsAsync(requesterId);
            return Ok(response);
        }
    }
}
