using AdviseCare.Application.Contracts.Requests.WelfareStatistics;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Student Welfare Officer")]
public sealed class WelfareStatisticsController(IWelfareStatisticsService welfareStatisticsService) : ControllerBase
{
    private readonly IWelfareStatisticsService _welfareStatisticsService = welfareStatisticsService;

    [HttpGet]
    [SwaggerOperation(Summary = "[Student Welfare Officer] View anonymised case statistics by support category",
        Description = "Choose one academic term or a complete UTC calendar-date range. Categories with fewer than five distinct students are suppressed; no student-level records or overall totals are returned.")]
    public async Task<IActionResult> GetByCategory(
        [FromQuery] SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _welfareStatisticsService.GetByCategoryAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("export")]
    [SwaggerOperation(Summary = "[Student Welfare Officer] Export anonymised case statistics as CSV",
        Description = "Uses the same term/date filters and category suppression as the summary endpoint. The generated CSV is not stored.")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        var file = await _welfareStatisticsService.ExportCsvAsync(request, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
