using AdviseCare.Application.Contracts.Requests.ExternalReferral;
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
    public class ExternalReferralsController(IExternalReferralService externalReferralService) : ControllerBase
    {
        private readonly IExternalReferralService _externalReferralService = externalReferralService;

        // Get all External referrals, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get all external referrals with optional pagination",
            Description = "Lists the referrals to outside services, newest first, with the service, the referring officer, the follow-up date and outcome, and whether the referral is overdue. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _externalReferralService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _externalReferralService.GetAll());
        }

        // Get an External referral by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Get external referral by Id",
            Description = "Retrieves one external referral.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _externalReferralService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search External referrals
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Search/Filter external referrals",
            Description = "Filters external referrals by case, service, status (sent/followedUp/closed) and overdue state. A sent referral past its follow-up date counts as overdue at once; the background check also stores the flag every 5 minutes. Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchExternalReferralRequest request)
        {
            var response = await _externalReferralService.SearchAsync(request);
            return Ok(response);
        }

        // Create an External referral
        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Refer a case to an external service",
            Description = "Sends an open case that was referred to (and accepted by) the signed-in officer to an active external service, with an optional follow-up due date. The referring officer is taken from the token.")]
        public async Task<IActionResult> Create([FromBody] CreateExternalReferralRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _externalReferralService.CreateAsync(officerId, request);
            return Ok(response);
        }

        // Update an External referral (record its follow-up)
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Record an external referral's follow-up",
            Description = "Records the follow-up outcome of a sent referral; the status becomes followedUp and the overdue flag is cleared.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateExternalReferralRequest request)
        {
            var response = await _externalReferralService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Close an External referral
        [HttpPut("{id}/close")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Close an external referral",
            Description = "Closes a followed-up referral (followedUp -> closed).")]
        public async Task<IActionResult> Close([FromRoute] Guid id)
        {
            var response = await _externalReferralService.CloseAsync(id);
            return Ok(response);
        }
    }
}
