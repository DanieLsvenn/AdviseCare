using AdviseCare.Application.Contracts.Requests.Auth;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        // Log in with email/username and password
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Login with email/username and password",
            Description = "Finds the account by email or username, verifies the password and checks that the account is active. " +
                "On success, opens a new session (SESSIONS row, with the optional device info) and returns the account's profile, roles and a JWT valid for 1 day. " +
                "The JWT carries the accountId, sessionId, email, full name and every role name of the account. " +
                "Returns 401 for a wrong identifier/password or an account with no password set yet, and 403 for a deactivated/banned account.")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        // Get the profile of the currently authenticated Account
        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Get my profile",
            Description = "Retrieves the profile (faculty, linking status, preferred language, status, sign-in method) and the roles of the account in the JWT. " +
                "Available to every role, so the web app can restore the signed-in user after a page reload.")]
        public async Task<IActionResult> Me()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _authService.GetSelfProfileAsync(accountId);
            return Ok(response);
        }

        // Log out of the current Session
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Logout",
            Description = "Ends the session in the JWT (isActive = false, logoutAt = now). " +
                "After this, every request with the same JWT is rejected with 401, even before the token expires. Calling it again on an ended session still returns success.")]
        public async Task<IActionResult> Logout()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _authService.LogoutAsync(accountId, sessionId);
            return Ok(response);
        }
    }
}
