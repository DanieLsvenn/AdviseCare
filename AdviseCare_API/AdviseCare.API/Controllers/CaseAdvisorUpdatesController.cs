using AdviseCare.Application.Contracts.Requests.CaseAdvisorUpdate;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/Cases/{caseId:guid}/advisor-updates")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class CaseAdvisorUpdatesController(ICaseAdvisorUpdateService caseAdvisorUpdateService) : ControllerBase
    {
        private readonly ICaseAdvisorUpdateService _caseAdvisorUpdateService = caseAdvisorUpdateService;

        [HttpGet]
        [SwaggerOperation(
            Summary = "[Student and case staff] List student-visible advisor updates",
            Description = "Returns the append-only updates intentionally shared by the current advisor with the student. The student, current advisor, Programme Head, and staff who sent or received a case referral may read them.")]
        public async Task<IActionResult> GetForCase([FromRoute] Guid caseId)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _caseAdvisorUpdateService.GetForCaseAsync(callerId, caseId));
        }

        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(
            Summary = "[Current case advisor] Publish a student-visible update",
            Description = "Adds an attributed, timestamped update of up to 1000 characters. Updates cannot be edited or deleted through the API. The student is notified; private case notes remain separate.")]
        public async Task<IActionResult> Create(
            [FromRoute] Guid caseId,
            [FromBody] CreateCaseAdvisorUpdateRequest request)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _caseAdvisorUpdateService.CreateAsync(authorId, caseId, request));
        }
    }
}
