using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // CASESTAGEHISTORIES is insert-only: rows are written by the case, referral and assignment endpoints, never through this controller
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CaseStageHistoriesController(ICaseService caseService) : ControllerBase
    {
        private readonly ICaseService _caseService = caseService;

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Involved accounts] Get the stage history of a case",
            Description = "Oldest first, each row with fromStage, toStage, who changed it, why and when. Same access as GET /api/Cases/{id} (the student can see their own).")]
        public async Task<IActionResult> Search([FromQuery] SearchCaseStageHistoryRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _caseService.GetStageHistoryAsync(accountId, request.CaseId));
        }
    }
}
