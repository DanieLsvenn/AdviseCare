using AdviseCare.Application.Contracts.Requests.Case;
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
    public class CasesController(ICaseService caseService) : ControllerBase
    {
        private readonly ICaseService _caseService = caseService;

        // Get all Cases, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get all cases with optional pagination",
            Description = "Retrieves every case, newest first, with its student (and student code), advisor, category, subject, stage, priority and status. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseService.GetAll());
        }

        // Get a Case by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get case by Id",
            Description = "Retrieves one case for its student, its current advisor, the Programme Head, or anyone who sent or received a referral of it (anyone else gets 404). " +
                "Opening a case marks the caller's notifications that point to it as read.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Cases
        [HttpGet("search")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Search/Filter cases",
            Description = "Filters cases by student, advisor, support category, subject, support type, stage, priority and status, newest first. Empty fields are ignored. " +
                "E.g. ?studentId=...&status=open shows which advisors already handle the student (WF2 step 3.2.3).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchCaseRequest request)
        {
            var response = await _caseService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Case
        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Create case",
            Description = "Student (WF2): leave studentId empty; subjectId only for academic categories; sharedAttributes (each a dataAttributeId with an optional shareReason of up to 1000 characters) must belong to the category's consent purpose; they are granted as consents and recorded on the case (CASE_ATTRIBUTE) in the same save, and requestReason holds only the overall story. " +
                "The case starts at stage pending with priority medium and waits in the matching queue; the Programme Heads are notified.\n\n" +
                "Advisor (EX3): give the studentId of an active student and a category of your own type; priority optional; sharedAttributes must stay empty. " +
                "The case starts at stage assigned with you as its advisor, and the student is notified with your contact information.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The support category '{CategoryName}' is archived.\n- Request reason is required.\n- Only academic requests can name a subject.\n- The subject {SubjectCode} is archived.\n- The note for a shared data attribute cannot exceed 1000 characters.\n- Shared data is chosen by the student. Leave the shared attributes empty when an advisor opens a case.\n- {FullName} is not a student.\n- {FullName} is not an active account.\n- Data cannot be shared for '{CategoryName}' right now because '{PurposeName}' is not active. Submit the request without shared data.\n- Only the data attributes of '{PurposeName}' can be shared with a '{CategoryName}' request.\n- The expiry date must be in the future.\n- Some data attributes are not covered by '{PurposeName}'.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only a {advisorRole} can open a {SupportType} case for a student.\n- Only students can request support. An advisor must give the student to open a case for them.\n- Only students can grant consent.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Support category not found.\n- Subject not found.\n- Student not found.\n- Account not found.\n- The consent purpose of this category was not found.\n- Case not found.\n- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- '{PurposeName}' is not active, so consent cannot be given for it.\n- These data attributes are archived and cannot be shared any more: {string.Join(", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateCaseRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Get my Cases
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get my cases",
            Description = "Lists the signed-in student's own cases, newest first, with the advisor's contact information once matched. Optional filters: currentStage and status (open/closed/reopened).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyAll([FromQuery] SearchMyCaseRequest request)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.GetMyAllAsync(studentId, request);
            return Ok(response);
        }

        // Get the matching queue
        [HttpGet("matching-queue")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get the queue of unassigned cases",
            Description = "Lists the cases without an advisor that are not closed, most urgent first, then oldest first (WF2 step 3.1).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMatchingQueue()
        {
            var response = await _caseService.GetMatchingQueueAsync();
            return Ok(response);
        }

        // Get suggested advisors for a Case
        [HttpGet("{id}/suggested-advisors")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Get suggested advisors for a case",
            Description = "Lists the active advisors of the case's type (academic -> Academic Advisor, wellbeing -> Wellbeing Advisor), ordered by: an advisor already handling another open case of the student, " +
                "subject specialists, the student's faculty (general advisors first), then the fewest open cases. matchReasons explains each place.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetSuggestedAdvisors([FromRoute] Guid id)
        {
            var response = await _caseService.GetSuggestedAdvisorsAsync(id);
            return Ok(response);
        }

        // Assign a Case to an advisor
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Assign an unassigned case to an advisor",
            Description = "Sets the case's advisor (the only advisor-student link) and moves the case from pending to assigned (written to the stage history). " +
                "The student is notified with the advisor's contact information, and the advisor is notified of the case.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Account not found.\n- Advisor account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- The case already has an advisor. Use reassign or a case change request to change it.", typeof(ErrorResponse))]
        public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignCaseRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.AssignAsync(programmeHeadId, id, request);
            return Ok(response);
        }

        // Reassign a Case to another advisor
        [HttpPut("{id}/reassign")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Reassign a case to another advisor directly",
            Description = "Moves the case to another advisor of its type with a reason; the stage is kept and the hand-over is written to the stage history. " +
                "Refused while a referral of the case is pending. The old advisor, the new advisor and the student are notified (EX6 step 5).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The case is already handled by this advisor.\n- Reason is required.\n- {FullName} is not an active account.\n- {FullName} does not have the role {RoleName}.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Account not found.\n- Advisor account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- The case has no advisor yet. Use assign instead.\n- A referral of this case is waiting for an answer. Reassign it after the referral is accepted or returned.", typeof(ErrorResponse))]
        public async Task<IActionResult> Reassign([FromRoute] Guid id, [FromBody] ReassignCaseRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.ReassignAsync(programmeHeadId, id, request);
            return Ok(response);
        }

        // Get my assigned Cases, with optional pagination
        [HttpGet("assigned")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get my assigned cases with optional pagination",
            Description = "Lists the signed-in advisor's cases. Filters: student (name or student code contains), supportType, currentStage, priority, status, unreadOnly. " +
                "sortBy = openedAt (default), priority, stage or studentName; sortDirection = desc (default) or asc. Each case has the advisor's unread notifications that point to it. " +
                "Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAssigned([FromQuery] SearchAssignedCaseRequest request, [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseService.GetAssignedWithPaginationAsync(
                    advisorId,
                    request,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseService.GetAssignedAsync(advisorId, request));
        }

        // Get how many of my assigned Cases have unread notifications
        [HttpGet("assigned/unread-count")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Count my cases with unread notifications",
            Description = "Returns how many of the signed-in advisor's cases have at least one unread notification pointing to them (the new/unread badge of D4.1).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAssignedUnreadCount()
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.GetAssignedUnreadCountAsync(advisorId);
            return Ok(response);
        }

        // Change the stage of a Case
        [HttpPut("{id}/stage")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Move my case to another stage",
            Description = "toStage = contacted, followUp or closed, with a reason written to the stage history. Closing sets status = closed and closedAt and notifies the student. " +
                "Refused while a referral of the case is pending.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The case is already at stage {ToStage}.\n- Reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the case's current advisor can do this.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed. Reopen it first.\n- A referral of this case is waiting for an answer.", typeof(ErrorResponse))]
        public async Task<IActionResult> ChangeStage([FromRoute] Guid id, [FromBody] ChangeCaseStageRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.ChangeStageAsync(advisorId, id, request);
            return Ok(response);
        }

        // Reopen a Case
        [HttpPut("{id}/reopen")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Reopen my closed case",
            Description = "Sets status = reopened, moves the stage from closed to assigned (written to the stage history with the reason) and clears closedAt; the student is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the case's current advisor can do this.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a closed case can be reopened.", typeof(ErrorResponse))]
        public async Task<IActionResult> Reopen([FromRoute] Guid id, [FromBody] ReopenCaseRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.ReopenAsync(advisorId, id, request);
            return Ok(response);
        }

        // Change the priority of a Case
        [HttpPut("{id}/priority")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Change the priority of a case",
            Description = "priority = low, medium, high or urgent. Allowed for the case's advisor, the Programme Head, and a Welfare Officer who received a referral of the case (WF2b step 2.2).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You cannot change the priority of this case.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.", typeof(ErrorResponse))]
        public async Task<IActionResult> ChangePriority([FromRoute] Guid id, [FromBody] ChangeCasePriorityRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.ChangePriorityAsync(callerId, id, request);
            return Ok(response);
        }
    }
}
