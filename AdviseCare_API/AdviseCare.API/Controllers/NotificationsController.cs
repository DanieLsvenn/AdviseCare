using AdviseCare.Application.Contracts.Requests.Notification;
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
    public class NotificationsController(INotificationService notificationService) : ControllerBase
    {
        private readonly INotificationService _notificationService = notificationService;

        // Get all Notifications, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all notifications with optional pagination",
            Description = "Retrieves every notification of every account, newest first. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _notificationService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _notificationService.GetAll());
        }

        // Get a Notification by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get notification by Id",
            Description = "Retrieves one notification by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _notificationService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Notifications by account, title or read state
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter notifications",
            Description = "Filters notifications by account, by title (contains) and by read state. Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchNotificationRequest request)
        {
            var response = await _notificationService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Notification
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create notification",
            Description = "Creates an in-app notification for an account by hand, for testing. It does not send an email; the features send notifications through NotificationService.NotifyAsync, which also follows the account's preferences.")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
        {
            var response = await _notificationService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Notification
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update notification",
            Description = "Updates a notification's title, message, redirect URL and read state, for testing.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateNotificationRequest request)
        {
            var response = await _notificationService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Delete a Notification
        [HttpDelete("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Delete notification",
            Description = "Deletes a notification of any account.")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var response = await _notificationService.DeleteAsync(id);
            return Ok(response);
        }

        // Get the notifications of the current Account
        [HttpGet("me")]
        [SwaggerOperation(Summary = "[All roles] Get my notifications",
            Description = "Retrieves the in-app notifications of the signed-in account, newest first. Each one has a title, a message, an optional redirectUrl to click through to the related item, and its read state.")]
        public async Task<IActionResult> GetMine()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.GetMyNotificationsAsync(accountId);
            return Ok(response);
        }

        // Count the unread notifications of the current Account
        [HttpGet("me/unread-count")]
        [SwaggerOperation(Summary = "[All roles] Count my unread notifications",
            Description = "Returns the number of unread notifications of the signed-in account, for the unread badge.")]
        public async Task<IActionResult> GetMyUnreadCount()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.GetMyUnreadCountAsync(accountId);
            return Ok(response);
        }

        // Mark one notification of the current Account as read
        [HttpPut("me/{id}/read")]
        [SwaggerOperation(Summary = "[All roles] Mark my notification as read",
            Description = "Marks one notification of the signed-in account as read. Returns 404 when the notification belongs to another account.")]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.MarkAsReadAsync(accountId, id);
            return Ok(response);
        }

        // Mark every notification of the current Account as read
        [HttpPut("me/read-all")]
        [SwaggerOperation(Summary = "[All roles] Mark all my notifications as read",
            Description = "Marks every unread notification of the signed-in account as read and returns how many were changed.")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.MarkAllAsReadAsync(accountId);
            return Ok(response);
        }

        // Delete one notification of the current Account
        [HttpDelete("me/{id}")]
        [SwaggerOperation(Summary = "[All roles] Delete my notification",
            Description = "Deletes one notification of the signed-in account. Returns 404 when the notification belongs to another account.")]
        public async Task<IActionResult> DeleteMine([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.DeleteMyNotificationAsync(accountId, id);
            return Ok(response);
        }

        // Clear (delete) the read notifications of the current Account
        [HttpDelete("me/clear")]
        [SwaggerOperation(Summary = "[All roles] Clear my old notifications",
            Description = "Deletes every read notification of the signed-in account and returns how many were deleted. Unread notifications are kept.")]
        public async Task<IActionResult> ClearMine()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _notificationService.ClearMyReadNotificationsAsync(accountId);
            return Ok(response);
        }
    }
}
