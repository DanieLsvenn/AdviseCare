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

        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Refer a case to an external service",
            Description = "Creates a referral to an active service. The referredBy account is taken from the signed-in token.")]
        public async Task<IActionResult> Create([FromBody] CreateExternalReferralRequest request)
        {
            var referredBy = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _externalReferralService.CreateAsync(referredBy, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Search external referrals",
            Description = "Filters by case, service, status, and overdue state. Results are ordered newest first.")]
        public async Task<IActionResult> Search([FromQuery] SearchExternalReferralRequest request)
        {
            return Ok(await _externalReferralService.SearchAsync(request));
        }

        [HttpPut("{id:guid}/follow-up")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Record an external referral follow-up",
            Description = "Records the outcome once and changes the referral status from sent to followedUp.")]
        public async Task<IActionResult> RecordFollowUp(
            [FromRoute] Guid id,
            [FromBody] RecordExternalReferralFollowUpRequest request)
        {
            return Ok(await _externalReferralService.RecordFollowUpAsync(id, request));
        }
    }
}
