using AdviseCare.Application.Contracts.Requests.Account;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Sign-in, account linking, passwords, own profile and own sessions, for every role
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAccountService accountService, ISessionService sessionService) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;
        private readonly ISessionService _sessionService = sessionService;

        // Log in with email/username and password
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Login with email/username and password",
            Description = "Finds the account by email or username, verifies the password and checks that the account is active. " +
                "On success, opens a new session (with the optional device info, e.g. the browser user agent) and returns the profile, roles, hasPassword and a JWT valid for 1 month. " +
                "The JWT carries the accountId, sessionId, email, full name and every role name. " +
                "Returns 401 for a wrong identifier/password or an account with no password yet, and 403 for a deactivated/banned account.")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _accountService.LoginAsync(request);
            return Ok(response);
        }

        // Log in with "Sign in with Google"
        [HttpPost("google")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Sign in with Google",
            Description = "Takes the ID token from the Google sign-in button and verifies it with Google. " +
                "Only an account already linked to that Google account (through the account-linking email) can sign in; any other Google account is refused with 401. " +
                "On success, opens a session and returns the same response as the password login.")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var response = await _accountService.GoogleLoginAsync(request);
            return Ok(response);
        }

        // Link the account from the account-linking email to a Google account
        [HttpPost("link-google")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Link account with Google",
            Description = "Called by the page opened from the account-linking email, with the token from the link and the Google ID token. " +
                "Checks that the token is valid and not expired (7 days), that the Google email matches the account email, and that the Google account is not linked to another account. " +
                "Then stores the Google id, sets linkStatus = linked, clears the token and signs the user in. " +
                "hasPassword = false in the response tells the web app to show the optional set-password step (with a Skip button).")]
        public async Task<IActionResult> LinkGoogle([FromBody] LinkGoogleAccountRequest request)
        {
            var response = await _accountService.LinkGoogleAccountAsync(request);
            return Ok(response);
        }

        // Request a password reset email
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Forgot password",
            Description = "Emails a one-time reset link (valid for 1 hour) when a linked, active account uses this email. " +
                "Accounts that are not linked yet get nothing, so a pending linking link is never replaced. Always returns the same message, so it can't be used to find out which emails exist.")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var response = await _accountService.ForgotPasswordAsync(request);
            return Ok(response);
        }

        // Set a new password with the token from the reset email
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Anonymous] Reset password",
            Description = "Sets a new password using the token from the reset email. The token works once; afterwards every session of the account is logged out, so the user signs in again.")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = await _accountService.ResetPasswordAsync(request);
            return Ok(response);
        }

        // Set or change the password of the current Account
        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Set or change password",
            Description = "One endpoint for both cases, decided by the server: when the account has no password yet (skipped at setup), oldPassword is not needed and the new password is set; " +
                "when it already has one, oldPassword is required and checked. The other sessions of the account are logged out; the current one stays signed in.")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _accountService.ChangePasswordAsync(accountId, sessionId, request);
            return Ok(response);
        }

        // Get the profile of the current Account
        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Get my profile",
            Description = "Retrieves the profile of the account in the JWT: faculty, avatar, sign-in method, linking status, preferred language, status, roles and hasPassword (which form to show in Settings). " +
                "The web app uses it to restore the signed-in user after a page reload.")]
        public async Task<IActionResult> Me()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.GetMyProfileAsync(accountId);
            return Ok(response);
        }

        // Update the settings of the current Account
        [HttpPut("me")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Update my profile",
            Description = "Changes the username and the preferred interface language (vi/en) of the signed-in account. The full name and email can only be changed by the System Administrator.")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.UpdateMyProfileAsync(accountId, request);
            return Ok(response);
        }

        // Upload a new avatar for the current Account
        [HttpPost("me/avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[All roles] Upload my avatar",
            Description = "Uploads a PNG, JPEG, WEBP or GIF image of at most 2 MB to Supabase Storage and saves its public URL as the avatar of the signed-in account. Returns the new avatar URL.")]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.UploadAvatarAsync(accountId, request);
            return Ok(response);
        }

        // Log out of the current Session
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Logout",
            Description = "Ends the session in the JWT (isActive = false, logoutAt = now). After this, every request with the same JWT is rejected with 401. Calling it again still returns success.")]
        public async Task<IActionResult> Logout()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _sessionService.LogoutAsync(accountId, sessionId, sessionId);
            return Ok(response);
        }

        // Get the live Sessions of the current Account
        [HttpGet("sessions")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Get my active sessions",
            Description = "Lists the sessions of the signed-in account that are still live (not logged out and less than 1 month old), newest first, with the device, sign-in time, expiry time and which one is the current session.")]
        public async Task<IActionResult> GetMySessions()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _sessionService.GetMySessionsAsync(accountId, sessionId);
            return Ok(response);
        }

        // Log out one chosen Session of the current Account
        [HttpPost("sessions/{id}/logout")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Log out one of my sessions",
            Description = "Ends one session of the signed-in account chosen from the session list (e.g. a forgotten computer). Returns 404 when the session belongs to another account.")]
        public async Task<IActionResult> LogoutSession([FromRoute] Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _sessionService.LogoutAsync(accountId, sessionId, id);
            return Ok(response);
        }

        // Log out every other Session of the current Account
        [HttpPost("sessions/logout-others")]
        [Authorize]
        [SwaggerOperation(Summary = "[All roles] Log out of other sessions",
            Description = "Ends every active session of the signed-in account except the current one.")]
        public async Task<IActionResult> LogoutOtherSessions()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var sessionId = Guid.Parse(User.FindFirstValue("sessionId")!);
            var response = await _sessionService.LogoutOtherSessionsAsync(accountId, sessionId);
            return Ok(response);
        }
    }
}
