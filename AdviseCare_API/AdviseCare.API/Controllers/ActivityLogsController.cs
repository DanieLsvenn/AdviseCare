using AdviseCare.Application.Contracts.Requests.ActivityLog;
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
    public class ActivityLogsController(IActivityLogService activityLogService) : ControllerBase
    {
        private readonly IActivityLogService _activityLogService = activityLogService;

        // Get all Activity logs, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all activity logs with optional pagination",
            Description = "Retrieves every activity log row of every account, newest first. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _activityLogService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _activityLogService.GetAll());
        }

        // Get a Activity log by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get activity log by Id",
            Description = "Retrieves one activity log row by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _activityLogService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Activity logs by account or activity type
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter activity logs",
            Description = "Filters activity log rows by account and by exact activity type (e.g. signedIn, accountsImported). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchActivityLogRequest request)
        {
            var response = await _activityLogService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Activity log
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create activity log",
            Description = "Creates an activity log row by hand, for testing. The system writes these rows itself when the actions happen.")]
        public async Task<IActionResult> Create([FromBody] CreateActivityLogRequest request)
        {
            var response = await _activityLogService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Activity log
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update activity log",
            Description = "Updates the type and detail of an activity log row, for testing. Activity history is normally never edited.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateActivityLogRequest request)
        {
            var response = await _activityLogService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Get the activity history of the current Account
        [HttpGet("me")]
        [SwaggerOperation(Summary = "[All roles] Get my activity history",
            Description = "Retrieves the activity history of the signed-in account (sign-ins, password changes, profile changes and, for admins, the management actions they did), newest first. The activity types are listed in AdviseCare_Activity_Types.txt.")]
        public async Task<IActionResult> GetMine()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _activityLogService.GetMyActivityLogsAsync(accountId);
            return Ok(response);
        }
    }
}
