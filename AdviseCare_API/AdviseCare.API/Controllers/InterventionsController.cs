using AdviseCare.Application.Contracts.Requests.Intervention;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student Welfare Officer")]
    public class InterventionsController(IInterventionService interventionService) : ControllerBase
    {
        private readonly IInterventionService _interventionService = interventionService;

        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Log an intervention for a case")]
        public async Task<IActionResult> Create([FromBody] CreateInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _interventionService.CreateAsync(officerId, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] View intervention history",
            Description = "Search by student id or case id, optionally filtering by status. Results are ordered newest first.")]
        public async Task<IActionResult> Search([FromQuery] SearchInterventionRequest request)
        {
            return Ok(await _interventionService.SearchAsync(request));
        }

        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Update an open intervention",
            Description = "Only the Welfare Officer who created the intervention can update it. Completed interventions are immutable.")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _interventionService.UpdateAsync(officerId, id, request));
        }

        [HttpPut("{id:guid}/outcome")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Record an outcome for an open intervention")]
        public async Task<IActionResult> RecordOutcome(
            [FromRoute] Guid id,
            [FromBody] RecordInterventionOutcomeRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _interventionService.RecordOutcomeAsync(officerId, id, request));
        }

        [HttpPut("{id:guid}/close")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Close an intervention",
            Description = "Completes the intervention and notifies the case's assigned advisor when one is assigned.")]
        public async Task<IActionResult> Close(
            [FromRoute] Guid id,
            [FromBody] CloseInterventionRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _interventionService.CloseAsync(officerId, id, request));
        }
    }
}
