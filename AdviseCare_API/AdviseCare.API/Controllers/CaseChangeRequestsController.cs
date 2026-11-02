using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
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
    public class CaseChangeRequestsController(ICaseChangeRequestService caseChangeRequestService) : ControllerBase
    {
        private readonly ICaseChangeRequestService _caseChangeRequestService = caseChangeRequestService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student")]
        [SwaggerOperation(Summary = "[The case's student or current advisor] Ask the Programme Head to move a case to another advisor",
            Description = "The case must have an advisor and not be closed; proposedAdvisorId must be an active advisor of the case's type " +
                "(academic -> Academic Advisor, wellbeing -> Wellbeing Advisor). Only one pending request per case. " +
                "programmeHeadId stays null until a Programme Head decides; the active Programme Head(s) are notified.")]
        public async Task<IActionResult> Create([FromBody] CreateCaseChangeRequestRequest request)
        {
            return Ok(await _caseChangeRequestService.CreateAsync(CurrentAccountId, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Programme Head, involved accounts] Search case change requests",
            Description = "Filters: status (pending/approved/rejected), caseId. Newest first. The Programme Head sees all; others only requests they made, " +
                "are proposed for, or that concern a case they are the student or the advisor of.")]
        public async Task<IActionResult> Search([FromQuery] SearchCaseChangeRequestRequest request)
        {
            return Ok(await _caseChangeRequestService.SearchAsync(CurrentAccountId, request));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Programme Head, involved accounts] Get a case change request")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _caseChangeRequestService.GetByIdAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/approve")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Approve a pending case change request",
            Description = "CASES.advisorId becomes the proposed advisor (stage kept, hand-over written to the stage history); programmeHeadId and decidedAt are set. " +
                "Optional body { \"decisionNotes\": \"...\" }. Both advisors and the student are notified. Refused while a referral of the case is pending.")]
        public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ApproveCaseChangeRequestRequest? request)
        {
            return Ok(await _caseChangeRequestService.ApproveAsync(CurrentAccountId, id, request ?? new ApproveCaseChangeRequestRequest()));
        }

        [HttpPut("{id:guid}/reject")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Reject a pending case change request with a reason",
            Description = "Body { \"reason\": \"...\" } is stored in decisionNotes and sent to the requester, both advisors and the student.")]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectCaseChangeRequestRequest request)
        {
            return Ok(await _caseChangeRequestService.RejectAsync(CurrentAccountId, id, request));
        }
    }
}
