using AdviseCare.Application.Contracts.Requests.Referral;
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
    public class ReferralsController(IReferralService referralService) : ControllerBase
    {
        private const string HandlerRoles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer";
        private readonly IReferralService _referralService = referralService;

        private Guid CurrentAccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);

        [HttpPost]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Case advisor] Refer my case to another advisor or to the Welfare Officer",
            Description = "toAccountId must be an active Academic Advisor, Wellbeing Advisor or Student Welfare Officer; priority = low, medium (default), high or urgent. " +
                "The case moves to stage referred and the receiver is notified. Only one pending referral per case.")]
        public async Task<IActionResult> Create([FromBody] CreateReferralRequest request)
        {
            return Ok(await _referralService.CreateAsync(CurrentAccountId, request));
        }

        [HttpGet("me/incoming")]
        [Authorize(Roles = HandlerRoles)]
        [SwaggerOperation(Summary = "[Advisors, Welfare Officer] Get referrals sent to me", Description = "Newest first; optional status (pending/accepted/returned).")]
        public async Task<IActionResult> GetIncoming([FromQuery] SearchMyReferralsRequest request)
        {
            return Ok(await _referralService.GetIncomingAsync(CurrentAccountId, request));
        }

        [HttpGet("me/outgoing")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Advisors] Get referrals I sent", Description = "Newest first; optional status (pending/accepted/returned).")]
        public async Task<IActionResult> GetOutgoing([FromQuery] SearchMyReferralsRequest request)
        {
            return Ok(await _referralService.GetOutgoingAsync(CurrentAccountId, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Staff of the case, Programme Head] Search referrals",
            Description = "With caseId: the referrals of that case (staff of the case only). Without caseId: Programme Head only, all referrals. Optional status.")]
        public async Task<IActionResult> Search([FromQuery] SearchReferralRequest request)
        {
            return Ok(await _referralService.SearchAsync(CurrentAccountId, request));
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "[Sender, receiver, staff of the case] Get a referral")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            return Ok(await _referralService.GetByIdAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/accept")]
        [Authorize(Roles = HandlerRoles)]
        [SwaggerOperation(Summary = "[Receiver] Accept a pending referral",
            Description = "Advisor receiver: becomes the case's advisor (assignment created or kept), stage referred -> assigned, the student gets the new contact information. " +
                "Welfare Officer: the advisor keeps the case and the stage stays referred until the welfare work is done.")]
        public async Task<IActionResult> Accept([FromRoute] Guid id)
        {
            return Ok(await _referralService.AcceptAsync(CurrentAccountId, id));
        }

        [HttpPut("{id:guid}/return")]
        [Authorize(Roles = HandlerRoles)]
        [SwaggerOperation(Summary = "[Receiver] Return a pending referral with a reason",
            Description = "The case goes back to the stage it had before the referral; the reason is written to the stage history and sent to the sender.")]
        public async Task<IActionResult> Return([FromRoute] Guid id, [FromBody] ReturnReferralRequest request)
        {
            return Ok(await _referralService.ReturnAsync(CurrentAccountId, id, request));
        }
    }
}
