using AdviseCare.Application.Contracts.Requests.AdvisorAssignment;
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
    public class AdvisorAssignmentsController(IAdvisorAssignmentService advisorAssignmentService) : ControllerBase
    {
        private readonly IAdvisorAssignmentService _advisorAssignmentService = advisorAssignmentService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Programme Head, advisors, Student] Search advisor-student assignments",
            Description = "Filters: advisorId, studentId, status (active/revoked). The Programme Head sees every pair; an advisor only their own pairs; a student only their own advisors.")]
        public async Task<IActionResult> Search([FromQuery] SearchAdvisorAssignmentRequest request)
        {
            return Ok(await _advisorAssignmentService.SearchAsync(CurrentAccountId, request));
        }

        [HttpGet("me/students")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get the students assigned to me",
            Description = "Active assignments, with each student's code, email and number of open cases with me (EX3 step 1).")]
        public async Task<IActionResult> GetMyStudents()
        {
            return Ok(await _advisorAssignmentService.GetMyStudentsAsync(CurrentAccountId));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Programme Head, the advisor or student of the pair] Get an advisor assignment")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _advisorAssignmentService.GetByIdAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/reassign")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Reassign a student to another advisor directly (EX6 step 5)",
            Description = "Revokes this assignment, creates (or keeps) the new advisor's assignment with the same student and moves the pair's open cases " +
                "whose support type the new advisor handles and that have no pending referral. All three people are notified.")]
        public async Task<IActionResult> Reassign([FromRoute] Guid id, [FromBody] ReassignAdvisorAssignmentRequest request)
        {
            return Ok(await _advisorAssignmentService.ReassignAsync(CurrentAccountId, id, request));
        }
    }
}
