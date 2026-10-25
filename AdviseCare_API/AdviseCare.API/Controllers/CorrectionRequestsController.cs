using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class CorrectionRequestsController(ICorrectionRequestService correctionRequestService) : ControllerBase
{
    private readonly ICorrectionRequestService _correctionRequestService = correctionRequestService;

    [HttpPost]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "[Student] Request a correction to your current indicator value",
        Description = "Ties the request to an exact student-indicator row. The API derives and encrypts the current value; the submitted proposed value is encrypted before storage.")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentAccountId(out var studentId))
        {
            return Unauthorized();
        }

        var response = await _correctionRequestService.CreateAsync(studentId, request, cancellationToken);
        return CreatedAtRoute("GetMyCorrectionRequest", new { correctionRequestId = response.CorrectionRequestId }, response);
    }

    [HttpGet("me/{correctionRequestId:guid}", Name = "GetMyCorrectionRequest")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "[Student] Read the status of your correction request")]
    public async Task<IActionResult> GetMine(Guid correctionRequestId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentAccountId(out var studentId))
        {
            return Unauthorized();
        }

        return Ok(await _correctionRequestService.GetMineAsync(studentId, correctionRequestId, cancellationToken));
    }

    [HttpGet("search")]
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
    [SwaggerOperation(Summary = "[Advisor] Search correction requests for assigned students",
        Description = "Only returns requests for students with an active advisor assignment. Consent and data-attribute access are checked before encrypted values are disclosed.")]
    public async Task<IActionResult> Search(
        [FromQuery] string status = "pending",
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentAccountId(out var advisorId))
        {
            return Unauthorized();
        }

        return Ok(await _correctionRequestService.SearchForAdvisorAsync(advisorId, status, cancellationToken));
    }

    [HttpPut("{correctionRequestId:guid}/approve")]
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
    [SwaggerOperation(Summary = "[Advisor] Approve a correction request",
        Description = "Creates a new correction-version student-indicator row and updates the request in one database save. The original row remains unchanged.")]
    public async Task<IActionResult> Approve(
        Guid correctionRequestId,
        [FromBody] CorrectionDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentAccountId(out var advisorId))
        {
            return Unauthorized();
        }

        return Ok(await _correctionRequestService.ApproveAsync(
            advisorId,
            correctionRequestId,
            request,
            cancellationToken));
    }

    [HttpPut("{correctionRequestId:guid}/reject")]
    [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
    [SwaggerOperation(Summary = "[Advisor] Reject a correction request",
        Description = "Records the reviewer and decision notes, then notifies the student.")]
    public async Task<IActionResult> Reject(
        Guid correctionRequestId,
        [FromBody] CorrectionDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentAccountId(out var advisorId))
        {
            return Unauthorized();
        }

        return Ok(await _correctionRequestService.RejectAsync(
            advisorId,
            correctionRequestId,
            request,
            cancellationToken));
    }

    [HttpPut("{correctionRequestId:guid}/withdraw")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "[Student] Withdraw a pending correction request")]
    public async Task<IActionResult> Withdraw(
        Guid correctionRequestId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentAccountId(out var studentId))
        {
            return Unauthorized();
        }

        await _correctionRequestService.WithdrawAsync(studentId, correctionRequestId, cancellationToken);
        return NoContent();
    }

    private bool TryGetCurrentAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.Sid), out accountId);
}
