using AdviseCare.Application.Contracts.Requests.Consent;
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
    public class ConsentsController(IConsentService consentService) : ControllerBase
    {
        private readonly IConsentService _consentService = consentService;

        // Get all Consents, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all consents with optional pagination",
            Description = "Retrieves every consent row (one per student, purpose and attribute), newest first, with the purpose, attribute and student names. " +
                "isExpired is true for a granted consent past its expiry date, which no longer gives access. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _consentService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _consentService.GetAll());
        }

        // Get a Consent by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get consent by Id",
            Description = "Retrieves one consent row with the purpose, attribute and student names.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _consentService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Consents by purpose, attribute, student or status
        [HttpGet("search")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter consents",
            Description = "Filters consents by purpose (consentPurposeId), attribute (dataAttributeId), student (studentId) and status (granted/withdrawn). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchConsentRequest request)
        {
            var response = await _consentService.SearchAsync(request);
            return Ok(response);
        }

        // Grant Consent for one purpose (current Student)
        [HttpPost]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Grant consent",
            Description = "Shares one or more data attributes of one active purpose, with an optional expiry date (must be in the future). " +
                "Each attribute must be active and covered by the purpose. An attribute already granted is kept as it is, an expired one is renewed. Every change is written to the consent history.")]
        public async Task<IActionResult> Create([FromBody] CreateConsentRequest request)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.CreateAsync(studentId, request);
            return Ok(response);
        }

        // Change the expiry date of an own Consent (current Student)
        [HttpPut("{id}")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Change my consent's expiry date",
            Description = "Sets or clears (null) the expiry date of one of the signed-in student's granted consents. Returns 404 when the consent belongs to another student.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateConsentRequest request)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.UpdateAsync(studentId, id, request);
            return Ok(response);
        }

        // Withdraw one own Consent (current Student)
        [HttpPut("{id}/withdraw")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Withdraw one consent",
            Description = "Withdraws one granted consent of the signed-in student: access through it stops immediately. " +
                "When the attribute is no longer granted under any purpose and the student has stored values for it, an erasure request is queued for the Steward.")]
        public async Task<IActionResult> Withdraw([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.WithdrawAsync(studentId, id);
            return Ok(response);
        }

        // Withdraw every own Consent of one purpose (current Student)
        [HttpPut("me/purposes/{consentPurposeId}/withdraw")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Withdraw consent for a whole purpose",
            Description = "Withdraws every granted consent of the signed-in student under one purpose and returns how many were withdrawn. Erasure requests are queued the same way as for a single withdrawal.")]
        public async Task<IActionResult> WithdrawPurpose([FromRoute] Guid consentPurposeId)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.WithdrawPurposeAsync(studentId, consentPurposeId);
            return Ok(response);
        }

        // Get the consent page of the current Student
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get my consents",
            Description = "Lists every active purpose (and any other purpose the student has consents under) with its attributes and the student's state for each: none, granted, expired or withdrawn.")]
        public async Task<IActionResult> GetMine()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.GetMyConsentsAsync(studentId);
            return Ok(response);
        }

        // Get all Consent histories (CONSENTHISTORIES), with optional pagination
        [HttpGet("histories")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all consent history with optional pagination",
            Description = "Retrieves every consent change (previous status -> new status, who and when), newest first. History rows are insert-only. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAllConsentHistory([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _consentService.GetAllConsentHistoryWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _consentService.GetAllConsentHistoryAsync());
        }

        // Get a Consent history (CONSENTHISTORIES) by Id
        [HttpGet("histories/{id}")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get consent history entry by Id",
            Description = "Retrieves one consent change.")]
        public async Task<IActionResult> GetConsentHistoryById([FromRoute] Guid id)
        {
            var response = await _consentService.GetConsentHistoryByIdAsync(id);
            return Ok(response);
        }

        // Search Consent histories (CONSENTHISTORIES) by who changed it, consent or student
        [HttpGet("histories/search")]
        [Authorize(Roles = "Data Protection Steward")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search consent history",
            Description = "Filters consent changes by who made them (changedBy), by consent (consentId) and by student (studentId). Empty fields are ignored.")]
        public async Task<IActionResult> SearchConsentHistory([FromQuery] SearchConsentHistoryRequest request)
        {
            var response = await _consentService.SearchConsentHistoryAsync(request);
            return Ok(response);
        }

        // Get the consent change history of the current Student
        [HttpGet("me/histories")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get my consent history",
            Description = "Lists every change to the signed-in student's consents, newest first.")]
        public async Task<IActionResult> GetMyConsentHistory()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _consentService.GetMyConsentHistoryAsync(studentId);
            return Ok(response);
        }
    }
}
