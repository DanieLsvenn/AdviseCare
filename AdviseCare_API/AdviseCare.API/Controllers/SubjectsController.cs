using AdviseCare.Application.Contracts.Requests.Subject;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubjectsController(ISubjectService subjectService) : ControllerBase
    {
        private readonly ISubjectService _subjectService = subjectService;

        // Get all Subjects, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[All roles] Get all subjects with optional pagination",
            Description = "Retrieves every subject, ordered by subject code. Pass page and pageSize together to page the results. Used for the subject dropdown of academic support requests.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _subjectService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _subjectService.GetAll());
        }

        // Get a Subject by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[All roles] Get subject by Id",
            Description = "Retrieves one subject by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _subjectService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Subjects by faculty, code, name or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[All roles] Search/Filter subjects",
            Description = "Filters subjects by faculty, by code or name (contains) and by status (active/archived). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchSubjectRequest request)
        {
            var response = await _subjectService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Subject
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create subject",
            Description = "Creates a new active subject under an existing faculty. The subject code (e.g. PRN212) must be unique.")]
        public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
        {
            var response = await _subjectService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Subject
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update subject",
            Description = "Updates a subject's faculty, code, name and status. Setting the status to archived removes it from the request form without breaking old cases.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSubjectRequest request)
        {
            var response = await _subjectService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Get all advisor-subject links (ACCOUNT_SUBJECT), with optional pagination
        [HttpGet("account-subjects")]
        [Authorize(Roles = "System Administrator,Programme Head")]
        [SwaggerOperation(Summary = "[System Administrator, Programme Head] Get all advisor-subject links with optional pagination",
            Description = "Retrieves every link between an academic advisor and a subject they specialise in, with the advisor's full name and the subject's code and name. An academic advisor with no link is a general academic advisor. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAllAccountSubject([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _subjectService.GetAllAccountSubjectWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _subjectService.GetAllAccountSubjectAsync());
        }

        // Get an advisor-subject link (ACCOUNT_SUBJECT) by advisor Id and subject Id
        [HttpGet("account-subjects/{accountId}/{subjectId}")]
        [Authorize(Roles = "System Administrator,Programme Head")]
        [SwaggerOperation(Summary = "[System Administrator, Programme Head] Get advisor-subject link",
            Description = "Retrieves the link between one academic advisor and one subject. Returns 404 when the advisor is not linked to that subject.")]
        public async Task<IActionResult> GetAccountSubjectById([FromRoute] Guid accountId, [FromRoute] Guid subjectId)
        {
            var response = await _subjectService.GetAccountSubjectByIdAsync(accountId, subjectId);
            return Ok(response);
        }

        // Search advisor-subject links (ACCOUNT_SUBJECT) by advisor or subject
        [HttpGet("account-subjects/search")]
        [Authorize(Roles = "System Administrator,Programme Head")]
        [SwaggerOperation(Summary = "[System Administrator, Programme Head] Search advisor-subject links",
            Description = "Filters advisor-subject links by advisor (accountId) and/or subject (subjectId), e.g. to view the advisors of one subject or the subjects of one advisor. Empty fields are ignored.")]
        public async Task<IActionResult> SearchAccountSubject([FromQuery] SearchAccountSubjectRequest request)
        {
            var response = await _subjectService.SearchAccountSubjectAsync(request);
            return Ok(response);
        }

        // Link an Academic Advisor to a Subject (ACCOUNT_SUBJECT)
        [HttpPost("account-subjects")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Link an academic advisor to a subject",
            Description = "Records that an academic advisor specialises in a subject, so academic support requests for that subject can be matched to them. The account must have the Academic Advisor role, the subject must be active, and the same link cannot be added twice.")]
        public async Task<IActionResult> CreateAccountSubject([FromBody] CreateAccountSubjectRequest request)
        {
            var response = await _subjectService.CreateAccountSubjectAsync(request);
            return Ok(response);
        }

        // Remove an advisor-subject link (ACCOUNT_SUBJECT)
        [HttpDelete("account-subjects/{accountId}/{subjectId}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Unlink an academic advisor from a subject",
            Description = "Removes the link between an academic advisor and a subject. An advisor left with no links becomes a general academic advisor.")]
        public async Task<IActionResult> DeleteAccountSubject([FromRoute] Guid accountId, [FromRoute] Guid subjectId)
        {
            var response = await _subjectService.DeleteAccountSubjectAsync(accountId, subjectId);
            return Ok(response);
        }
    }
}
