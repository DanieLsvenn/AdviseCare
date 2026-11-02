using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Case stage histories are insert-only; they are written by the case workflow in CaseService
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CaseStageHistoriesController(ICaseService caseService) : ControllerBase
    {
        private readonly ICaseService _caseService = caseService;

        // Search Case stage histories of a case
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get the stage history of a case",
            Description = "caseId is required. Lists every stage change and hand-over of the case, oldest first, with fromStage, toStage, who changed it, why and when. " +
                "Same access as GET /api/Cases/{id} (the student can see their own).")]
        public async Task<IActionResult> Search([FromQuery] SearchCaseStageHistoryRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseService.SearchCaseStageHistoryAsync(callerId, request);
            return Ok(response);
        }
    }
}
