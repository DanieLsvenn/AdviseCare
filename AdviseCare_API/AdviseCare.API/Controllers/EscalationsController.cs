using AdviseCare.Application.Contracts.Requests.Escalation;
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
    public class EscalationsController(IEscalationService escalationService) : ControllerBase
    {
        private readonly IEscalationService _escalationService = escalationService;

        [HttpPost]
        [Authorize(Roles = "Student Welfare Officer")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Escalate a case to a Programme Head")]
        public async Task<IActionResult> Create([FromBody] CreateEscalationRequest request)
        {
            var escalatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _escalationService.CreateAsync(escalatedBy, request));
        }

        [HttpGet("search")]
        [Authorize(Roles = "Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Search own escalations",
            Description = "Programme Heads see escalations assigned to them; Welfare Officers see escalations they raised.")]
        public async Task<IActionResult> Search([FromQuery] SearchEscalationRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _escalationService.SearchAsync(accountId, User.IsInRole("Programme Head"), request));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Get an escalation involving the signed-in account")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _escalationService.GetByIdAsync(accountId, id));
        }

        [HttpPut("{id:guid}/respond")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Respond to a pending escalation")]
        public async Task<IActionResult> Respond(
            [FromRoute] Guid id,
            [FromBody] RespondToEscalationRequest request)
        {
            var programmeHeadId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _escalationService.RespondAsync(programmeHeadId, id, request));
        }
    }
}
