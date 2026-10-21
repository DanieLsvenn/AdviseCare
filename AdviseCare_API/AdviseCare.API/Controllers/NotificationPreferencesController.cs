using AdviseCare.Application.Contracts.Requests.NotificationPreference;
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
    public class NotificationPreferencesController(INotificationPreferenceService notificationPreferenceService) : ControllerBase
    {
        private readonly INotificationPreferenceService _notificationPreferenceService = notificationPreferenceService;

        // Get all Notification preferences, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all notification preferences with optional pagination",
            Description = "Retrieves every notification preference row of every account. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _notificationPreferenceService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _notificationPreferenceService.GetAll());
        }

        // Get a Notification preference by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get notification preference by Id",
            Description = "Retrieves one notification preference row by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _notificationPreferenceService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Notification preferences by account, channel or state
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter notification preferences",
            Description = "Filters notification preference rows by account, by channel (email/inApp) and by on/off state. Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchNotificationPreferenceRequest request)
        {
            var response = await _notificationPreferenceService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Notification preference
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create notification preference",
            Description = "Creates a notification preference row (account, channel email/inApp, on/off), for testing. An account can have one row per channel; a missing row means the channel is on.")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationPreferenceRequest request)
        {
            var response = await _notificationPreferenceService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Notification preference
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update notification preference",
            Description = "Turns a notification preference row on or off, for testing.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateNotificationPreferenceRequest request)
        {
            var response = await _notificationPreferenceService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Get the notification preferences of the current Account
        [HttpGet("me")]
        [SwaggerOperation(Summary = "[All roles] Get my notification preferences",
            Description = "Returns both notification channels of the signed-in account: email and inApp, each on or off. A channel the account never changed is on.")]
        public async Task<IActionResult> GetMine()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationPreferenceService.GetMyNotificationPreferencesAsync(accountId);
            return Ok(response);
        }

        // Turn one notification channel of the current Account on or off
        [HttpPut("me")]
        [SwaggerOperation(Summary = "[All roles] Update my notification preference",
            Description = "Turns the email or inApp channel of the signed-in account on or off. With email off, notifications are no longer emailed; with inApp off, they are no longer shown in the app. Account-linking and password-reset emails are always sent.")]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateMyNotificationPreferenceRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationPreferenceService.UpdateMyNotificationPreferenceAsync(accountId, request);
            return Ok(response);
        }
    }
}
