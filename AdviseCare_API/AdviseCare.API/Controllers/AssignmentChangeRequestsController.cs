using AdviseCare.Application.Contracts.Requests.AssignmentChangeRequest;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student,Programme Head")]
    public class AssignmentChangeRequestsController(IAssignmentChangeRequestService assignmentChangeRequestService) : ControllerBase
    {
        private readonly IAssignmentChangeRequestService _assignmentChangeRequestService = assignmentChangeRequestService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student")]
        [SwaggerOperation(Summary = "[The advisor or student of the assignment] Ask the Programme Head for a different advisor",
            Description = "advisorAssignmentId must be an active assignment you are part of; proposedAdvisorId an active Academic or Wellbeing Advisor. " +
                "Only one pending request per assignment. programmeHeadId is set to the active Programme Head, who is notified.")]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentChangeRequestRequest request)
        {
            return Ok(await _assignmentChangeRequestService.CreateAsync(CurrentAccountId, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Programme Head, involved accounts] Search assignment change requests",
            Description = "Filters: status (pending/approved/rejected), advisorAssignmentId. The Programme Head sees all; others only requests they are part of.")]
        public async Task<IActionResult> Search([FromQuery] SearchAssignmentChangeRequestRequest request)
        {
            return Ok(await _assignmentChangeRequestService.SearchAsync(CurrentAccountId, request));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Programme Head, involved accounts] Get an assignment change request")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _assignmentChangeRequestService.GetByIdAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/approve")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Approve a pending change request",
            Description = "Revokes the old assignment, creates the proposed advisor's assignment and moves the pair's open cases (see handover in the response). " +
                "Both advisors and the student are notified.")]
        public async Task<IActionResult> Approve([FromRoute] Guid id)
        {
            return Ok(await _assignmentChangeRequestService.ApproveAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/reject")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Reject a pending change request with a reason",
            Description = "The reason is sent to the requester, both advisors and the student and kept in the activity log (the table has no column for it).")]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectAssignmentChangeRequestRequest request)
        {
            return Ok(await _assignmentChangeRequestService.RejectAsync(CurrentAccountId, id, request));
        }
    }
}
