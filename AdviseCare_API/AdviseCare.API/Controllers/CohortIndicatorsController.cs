using AdviseCare.Application.Contracts.Requests.CohortIndicator;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "System Administrator,Data Protection Steward,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
public sealed class CohortIndicatorsController(ICohortIndicatorService cohortIndicatorService) : ControllerBase
{
    private readonly ICohortIndicatorService _cohortIndicatorService = cohortIndicatorService;

    [HttpGet]
    [SwaggerOperation(Summary = "[Staff with Cohort Analytics consent] View anonymised cohort indicator statistics",
        Description = "Requires termId, facultyId, or both. Only values covered by the active Cohort Analytics purpose, caller role policy, and student consent are decrypted. Results use the latest value per student/indicator/term and suppress cells with fewer than ten distinct students.")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
        var response = await _cohortIndicatorService.GetSummaryAsync(
            accessorId,
            request,
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("export")]
    [SwaggerOperation(Summary = "[Staff with Cohort Analytics consent] Export anonymised cohort indicator statistics",
        Description = "Returns the same threshold-suppressed summary as CSV and records the export in the activity log.")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
        var file = await _cohortIndicatorService.ExportCsvAsync(
            accessorId,
            request,
            cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
