using AdviseCare.Application.Contracts.Requests.Account;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Account management for the System Administrator (sign-in and own-account endpoints are in AuthController)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "System Administrator")]
    public class AccountsController(IAccountService accountService) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;

        // Get all Accounts, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[System Administrator] Get all accounts with optional pagination",
            Description = "Retrieves every account, newest first, with its faculty, link status, account status, roles and whether it has a password. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _accountService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _accountService.GetAll());
        }

        // Get an Account by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[System Administrator] Get account by Id",
            Description = "Retrieves one account by its unique identifier, including its roles.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _accountService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Accounts for the account dashboard
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter accounts",
            Description = "Filters the account dashboard by email, full name or username (contains), by faculty, by role name, by link status (notLinked/emailSent/linked) and by account status (active/deactivated/banned). Empty fields are ignored; sorting is done by the web app.")]
        public async Task<IActionResult> Search([FromQuery] SearchAccountRequest request)
        {
            var response = await _accountService.SearchAsync(request);
            return Ok(response);
        }

        // Create a single Account
        [HttpPost]
        [SwaggerOperation(Summary = "[System Administrator] Create account",
            Description = "Creates one account (email, full name, username, optional faculty, one or more role names) for special cases such as late enrolment or staff. " +
                "The account has no password and linkStatus = notLinked; send it the account-linking email afterwards. Email and username must be unique and the faculty must be active.")]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            var response = await _accountService.CreateAsync(request);
            return Ok(response);
        }

        // Update an Account's details
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "[System Administrator] Update account",
            Description = "Updates an account's email, full name, username and faculty. Changing the email unlinks the Google account (linkStatus back to notLinked), so the linking email has to be sent again. " +
                "Roles and status have their own endpoints below.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAccountRequest request)
        {
            var response = await _accountService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Import Accounts from an Excel file
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Import accounts from Excel",
            Description = "Uploads an .xlsx file whose first sheet has the header row email, fullName, userName, facultyCode, role (role may hold several role names separated by commas). " +
                "Every row is validated (required fields, email format, duplicate email/username in the file or the system, faculty code exists and is active, role exists). " +
                "All-or-nothing: if any row is invalid nothing is created and isSuccess = false with the list of invalid rows; otherwise every account is created and their ids are returned, ready for sending the linking emails.")]
        public async Task<IActionResult> Import([FromForm] ImportAccountRequest request)
        {
            var response = await _accountService.ImportAsync(request);
            return Ok(response);
        }

        // Send or resend account-linking emails
        [HttpPost("linking-emails")]
        [SwaggerOperation(Summary = "[System Administrator] Send account-linking emails",
            Description = "Emails each given account a one-time linking link (valid for 7 days; a new email replaces the old link) and sets linkStatus = emailSent. " +
                "Accounts that are already linked, not active or not found are skipped; accounts whose email could not be sent are listed as failed.")]
        public async Task<IActionResult> SendLinkingEmails([FromBody] SendLinkingEmailRequest request)
        {
            var response = await _accountService.SendLinkingEmailsAsync(request);
            return Ok(response);
        }

        // Activate an Account
        [HttpPut("{id}/activate")]
        [SwaggerOperation(Summary = "[System Administrator] Activate account",
            Description = "Sets an account's status back to active, for example to restore a deactivated account.")]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            var response = await _accountService.ActivateAsync(id);
            return Ok(response);
        }

        // Deactivate an Account
        [HttpPut("{id}/deactivate")]
        [SwaggerOperation(Summary = "[System Administrator] Deactivate account",
            Description = "Sets an account's status to deactivated: sign-in is refused and every active session ends immediately. An admin cannot deactivate their own account.")]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.DeactivateAsync(actorId, id);
            return Ok(response);
        }

        // Ban an Account
        [HttpPut("{id}/ban")]
        [SwaggerOperation(Summary = "[System Administrator] Ban account",
            Description = "Sets an account's status to banned, typically for a policy violation: sign-in is refused and every active session ends immediately. An admin cannot ban their own account.")]
        public async Task<IActionResult> Ban([FromRoute] Guid id)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.BanAsync(actorId, id);
            return Ok(response);
        }

        // Unban an Account
        [HttpPut("{id}/unban")]
        [SwaggerOperation(Summary = "[System Administrator] Unban account",
            Description = "Reverses a ban, setting the account's status back to active.")]
        public async Task<IActionResult> Unban([FromRoute] Guid id)
        {
            var response = await _accountService.UnbanAsync(id);
            return Ok(response);
        }

        // Get all account-role assignments (ACCOUNT_ROLE), with optional pagination
        [HttpGet("roles")]
        [SwaggerOperation(Summary = "[System Administrator] Get all account roles with optional pagination",
            Description = "Retrieves every account-role assignment with the account's full name, the role name and when it was assigned. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAllAccountRole([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _accountService.GetAllAccountRoleWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _accountService.GetAllAccountRoleAsync());
        }

        // Get an account-role assignment (ACCOUNT_ROLE) by account Id and role Id
        [HttpGet("{accountId}/roles/{roleId}")]
        [SwaggerOperation(Summary = "[System Administrator] Get account role",
            Description = "Retrieves one account-role assignment. Returns 404 when the account does not have that role.")]
        public async Task<IActionResult> GetAccountRoleById([FromRoute] Guid accountId, [FromRoute] Guid roleId)
        {
            var response = await _accountService.GetAccountRoleByIdAsync(accountId, roleId);
            return Ok(response);
        }

        // Search account-role assignments (ACCOUNT_ROLE) by account or role
        [HttpGet("roles/search")]
        [SwaggerOperation(Summary = "[System Administrator] Search account roles",
            Description = "Filters account-role assignments by account (accountId) and/or role (roleId), e.g. to list every Programme Head. Empty fields are ignored.")]
        public async Task<IActionResult> SearchAccountRole([FromQuery] SearchAccountRoleRequest request)
        {
            var response = await _accountService.SearchAccountRoleAsync(request);
            return Ok(response);
        }

        // Give an Account a role (ACCOUNT_ROLE)
        [HttpPost("roles")]
        [SwaggerOperation(Summary = "[System Administrator] Add a role to an account",
            Description = "Gives an account one more role. An account can hold several roles; the same role cannot be added twice. The new role is in the JWT from the account's next sign-in.")]
        public async Task<IActionResult> CreateAccountRole([FromBody] CreateAccountRoleRequest request)
        {
            var response = await _accountService.CreateAccountRoleAsync(request);
            return Ok(response);
        }

        // Remove a role from an Account (ACCOUNT_ROLE)
        [HttpDelete("{accountId}/roles/{roleId}")]
        [SwaggerOperation(Summary = "[System Administrator] Remove a role from an account",
            Description = "Removes one role from an account. The last System Administrator role in the system cannot be removed, so the app always keeps an admin.")]
        public async Task<IActionResult> DeleteAccountRole([FromRoute] Guid accountId, [FromRoute] Guid roleId)
        {
            var response = await _accountService.DeleteAccountRoleAsync(accountId, roleId);
            return Ok(response);
        }
    }
}
