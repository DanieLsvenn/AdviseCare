using AdviseCare.Application.Contracts.Requests.Case;
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
    public class CasesController(ICaseService caseService) : ControllerBase
    {
        private const string AdvisorRoles = "Academic Advisor,Wellbeing Advisor";
        private readonly ICaseService _caseService = caseService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        // ---------- D2 / D4.2 ----------

        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Create a case",
            Description = "Student (WF2): leave studentId empty; subjectId only for academic categories; sharedAttributeIds must belong to the category's consent purpose " +
                "and are recorded as consents in the same transaction. The case starts at stage pending and waits in the matching queue.\n\n" +
                "Advisor (EX3): give studentId of an active student and a category of your type; priority optional; sharedAttributeIds must be empty. " +
                "The case starts at stage assigned with you as its advisor and the student is notified with your contact information.")]
        public async Task<IActionResult> Create([FromBody] CreateCaseRequest request)
        {
            return Ok(await _caseService.CreateAsync(CurrentAccountId, request));
        }

        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get my cases", Description = "Newest first. Optional filters: status (open/closed/reopened), currentStage.")]
        public async Task<IActionResult> GetMine([FromQuery] SearchMyCasesRequest request)
        {
            return Ok(await _caseService.GetMyCasesAsync(CurrentAccountId, request));
        }

        [HttpGet("search")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Search cases",
            Description = "Newest first. Filters: studentId, advisorId, supportCategoryId, subjectId, supportType, currentStage, status, priority. " +
                "E.g. ?studentId=...&status=open shows which advisors already handle the student (WF2 step 3.2.3).")]
        public async Task<IActionResult> Search([FromQuery] SearchCasesRequest request)
        {
            return Ok(await _caseService.SearchAsync(request));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Involved accounts] Get a case",
            Description = "Allowed for the student it belongs to, its current advisor, the Programme Head and anyone who sent or received a referral of it. " +
                "Opening a case marks the caller's notifications that point to it as read.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _caseService.GetByIdAsync(CurrentAccountId, id));
        }

        // ---------- D3 ----------

        [HttpGet("matching-queue")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get the queue of unassigned cases", Description = "Cases without an advisor that are not closed, ordered by priority (urgent first), then oldest first.")]
        public async Task<IActionResult> GetMatchingQueue()
        {
            return Ok(await _caseService.GetMatchingQueueAsync());
        }

        [HttpGet("{id:guid}/suggested-advisors")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Suggest advisors for a case",
            Description = "Active advisors of the case's type (academic -> Academic Advisor, wellbeing -> Wellbeing Advisor), ordered: advisors already assigned to the student, " +
                "subject specialists, same faculty as the student (general advisors first), then fewest open cases. Each item explains why in matchReasons.")]
        public async Task<IActionResult> GetSuggestedAdvisors([FromRoute] Guid id)
        {
            return Ok(await _caseService.GetSuggestedAdvisorsAsync(id));
        }

        [HttpPut("{id:guid}/assign")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Assign an unassigned case to an advisor",
            Description = "Sets the case's advisor (the only advisor-student link), moves the case pending -> assigned and notifies the student (with the advisor's contact information) and the advisor.")]
        public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignCaseRequest request)
        {
            return Ok(await _caseService.AssignAsync(CurrentAccountId, id, request));
        }

        [HttpPut("{id:guid}/reassign")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Move one case to another advisor directly",
            Description = "The stage is kept; the hand-over is written to the stage history. Refused while a referral of the case is pending. " +
                "The old advisor, the new advisor and the student are notified (EX6 step 5).")]
        public async Task<IActionResult> Reassign([FromRoute] Guid id, [FromBody] ReassignCaseRequest request)
        {
            return Ok(await _caseService.ReassignAsync(CurrentAccountId, id, request));
        }

        // ---------- D4 ----------

        [HttpGet("assigned")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get my assigned cases",
            Description = "Filters: currentStage, status, supportType, priority, student (name or code contains), unreadOnly. " +
                "sortBy = openedAt (default) | priority | stage | studentName, sortDirection = desc (default) | asc. Optional page + pageSize. " +
                "unreadNotificationCount / unreadCaseCount come from the advisor's unread notifications that point to each case.")]
        public async Task<IActionResult> GetAssigned([FromQuery] SearchAssignedCasesRequest request)
        {
            return Ok(await _caseService.GetAssignedAsync(CurrentAccountId, request));
        }

        [HttpPut("{id:guid}/stage")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Move my case to another stage",
            Description = "toStage = contacted, followUp or closed, with a reason (written to the stage history). Closing sets status = closed and closedAt. " +
                "Refused while a referral of the case is pending.")]
        public async Task<IActionResult> ChangeStage([FromRoute] Guid id, [FromBody] ChangeCaseStageRequest request)
        {
            return Ok(await _caseService.ChangeStageAsync(CurrentAccountId, id, request));
        }

        [HttpPut("{id:guid}/reopen")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Reopen my closed case", Description = "status = reopened, stage closed -> assigned, closedAt cleared; the student is notified.")]
        public async Task<IActionResult> Reopen([FromRoute] Guid id, [FromBody] ReopenCaseRequest request)
        {
            return Ok(await _caseService.ReopenAsync(CurrentAccountId, id, request));
        }

        [HttpPut("{id:guid}/priority")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Case advisor, Welfare Officer with a referral of the case, Programme Head] Change a case's priority", Description = "priority = low, medium, high or urgent.")]
        public async Task<IActionResult> ChangePriority([FromRoute] Guid id, [FromBody] ChangeCasePriorityRequest request)
        {
            return Ok(await _caseService.ChangePriorityAsync(CurrentAccountId, id, request));
        }
    }
}
