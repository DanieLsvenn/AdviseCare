using AdviseCare.Application.Contracts.Requests.Statistic;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Anonymised statistics (QC-024), generated on the fly and never stored: welfare statistics over case activity, and cohort data over consented student data
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController(IStatisticService statisticService) : ControllerBase
    {
        private const string WelfareRoles = "Student Welfare Officer,Programme Head";
        private readonly IStatisticService _statisticService = statisticService;

        // Get the Welfare statistics
        [HttpGet("welfare")]
        [Authorize(Roles = WelfareRoles)]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] View anonymised welfare statistics",
            Description = "from and to are inclusive UTC dates. Per support category (and per faculty when isByFaculty = true): cases opened, referred cases, interventions, support grants, external referrals and escalations dated in the range, and the number of different students behind them. " +
                "Optional filters: facultyId, supportType (academic/wellbeing). Every cell counted from fewer than 5 different students is suppressed (isSuppressed = true, counts null). No student-level data is returned.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Both from and to dates are required.\n- The to date must not be before the from date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Faculty not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetWelfareStatistic([FromQuery] GetWelfareStatisticRequest request)
        {
            var response = await _statisticService.GetWelfareStatisticAsync(request);
            return Ok(response);
        }

        // Export the Welfare statistics
        [HttpGet("welfare/export")]
        [Authorize(Roles = WelfareRoles)]
        [SwaggerOperation(Summary = "[Student Welfare Officer, Programme Head] Export the welfare statistics",
            Description = "Same filters as /welfare plus format = xlsx (default), csv or pdf. Suppressed cells are written as \"hidden\". Downloads the file; nothing is stored, and the export is recorded in the activity log (statisticsExported).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Both from and to dates are required.\n- The to date must not be before the from date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Faculty not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> ExportWelfareStatistic([FromQuery] ExportWelfareStatisticRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _statisticService.ExportWelfareStatisticAsync(accessorId, request);
            return File(response.Content, response.ContentType, response.FileName);
        }

        // Get the Cohort data
        [HttpGet("cohort")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] View anonymised cohort data of one data attribute",
            Description = "dataAttributeId and termId are required; facultyId filters, isByFaculty = true splits the result per faculty. Uses only the latest value per student that the student shared under the statistics consent purpose (isForStatistics), never emergency access. " +
                "Numeric attributes give the average, minimum and maximum; categorical and boolean ones give the number of students per value. Every cell (and every value) with fewer than 10 students is suppressed. Each value read is written to the access log.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- This data attribute is not covered by '{PurposeName}', so it has no cohort data.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Academic term not found.\n- Faculty not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- There is no active statistics consent purpose, so no cohort data can be shown.\n- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetCohortStatistic([FromQuery] GetCohortStatisticRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _statisticService.GetCohortStatisticAsync(accessorId, request);
            return Ok(response);
        }

        // Export the Cohort data
        [HttpGet("cohort/export")]
        [Authorize(Roles = "Programme Head")]
        [SwaggerOperation(Summary = "[Programme Head] Export the cohort data",
            Description = "Same filters as /cohort plus format = xlsx (default), csv or pdf. Suppressed cells are written as \"hidden\". Downloads the file; nothing is stored, and the export is recorded in the activity log (statisticsExported).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- This data attribute is not covered by '{PurposeName}', so it has no cohort data.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Academic term not found.\n- Faculty not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- There is no active statistics consent purpose, so no cohort data can be shown.\n- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> ExportCohortStatistic([FromQuery] ExportCohortStatisticRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _statisticService.ExportCohortStatisticAsync(accessorId, request);
            return File(response.Content, response.ContentType, response.FileName);
        }
    }
}
