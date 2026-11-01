using AdviseCare.Application.Contracts.Requests.CaseNote;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class CaseNotesController(ICaseNoteService caseNoteService) : ControllerBase
    {
        private readonly ICaseNoteService _caseNoteService = caseNoteService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Case advisor] Write a draft note on my case", Description = "The note starts as a draft (isFinalized = false) that only the author can see and edit.")]
        public async Task<IActionResult> Create([FromBody] CreateCaseNoteRequest request)
        {
            return Ok(await _caseNoteService.CreateAsync(CurrentAccountId, request));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Note author] Edit my draft note", Description = "Sets editedAt. A finalized note cannot be edited (400).")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCaseNoteRequest request)
        {
            return Ok(await _caseNoteService.UpdateAsync(CurrentAccountId, id, request));
        }

        [HttpPut("{id:guid}/finalize")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Note author] Finalize my draft note", Description = "After this the note is visible to the staff of the case and can never change.")]
        public async Task<IActionResult> Finalize([FromRoute] Guid id)
        {
            return Ok(await _caseNoteService.FinalizeAsync(CurrentAccountId, id));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Staff of the case] Get a case note", Description = "Drafts are only visible to their author.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _caseNoteService.GetByIdAsync(CurrentAccountId, id));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Staff of the case] Get the notes of a case",
            Description = "caseId is required; optional isFinalized. Returns finalized notes plus the caller's own drafts, oldest first. " +
                "Staff of the case = its current advisor, the Programme Head and the sender/receiver of any of its referrals (e.g. the Welfare Officer). Students cannot read notes.")]
        public async Task<IActionResult> Search([FromQuery] SearchCaseNoteRequest request)
        {
            return Ok(await _caseNoteService.SearchAsync(CurrentAccountId, request));
        }
    }
}
