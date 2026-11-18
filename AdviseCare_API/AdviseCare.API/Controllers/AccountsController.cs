using AdviseCare.Application.Contracts.Requests.Account;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Account management for the System Administrator (sign-in and own-account endpoints are in AuthController);
    // every endpoint is for the System Administrator except the staff directory search
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController(IAccountService accountService) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;

        // Get all Accounts, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all accounts with optional pagination",
            Description = "Retrieves every account, newest first, with its faculty, link status, account status, roles and whether it has a password. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
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
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get account by Id",
            Description = "Retrieves one account by its unique identifier, including its roles.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _accountService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Accounts for the account dashboard
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter accounts",
            Description = "Filters the account dashboard by email, full name, username or student code (contains), by faculty, by role name, by academic status (enrolled/onLeave/withdrawn/graduated), by link status (notLinked/emailSent/linked) and by account status (active/deactivated/banned). Empty fields are ignored; sorting is done by the web app.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchAccountRequest request)
        {
            var response = await _accountService.SearchAsync(request);
            return Ok(response);
        }

        // Create a single Account
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create account",
            Description = "Creates one account (email, full name, username, optional faculty, one or more role names) for special cases such as late enrolment or staff. " +
                "A Student account also needs a unique studentCode and an academicStatus (enrolled/onLeave/withdrawn/graduated), which create its student profile; other accounts must leave both empty. " +
                "The account has no password and linkStatus = notLinked; send it the account-linking email afterwards. Email and username must be unique and the faculty must be active.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- (details of the failing rows or file)\n- Role '{roleName}' does not exist.\n- Faculty '{FacultyCode}' is archived.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Faculty not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- An account with email '{email}' already exists.\n- An account with username '{userName}' already exists.\n- A student with student code '{studentCode}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            var response = await _accountService.CreateAsync(request);
            return Ok(response);
        }

        // Update an Account's details
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update account",
            Description = "Updates an account's email, full name, username and faculty. Changing the email unlinks the Google account (linkStatus back to notLinked), so the linking email has to be sent again. " +
                "Roles and status have their own endpoints below.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Faculty '{FacultyCode}' is archived.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.\n- Faculty not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- An account with email '{email}' already exists.\n- An account with username '{userName}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAccountRequest request)
        {
            var response = await _accountService.UpdateAsync(id, request);
            return Ok(response);
        }

        // Import Accounts from an Excel file
        [HttpPost("import")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Import accounts from Excel or CSV (.xlsx/.csv)",
            Description = "Uploads an .xlsx file whose first sheet has the header row email, fullName, userName, facultyCode, role, studentCode, academicStatus (role may hold several role names separated by commas). " +
                "Student rows fill all 7 columns (studentCode unique, academicStatus enrolled/onLeave/withdrawn/graduated) and get a student profile; every other row fills only the first 5 and leaves studentCode and academicStatus empty. " +
                "Every row is validated (required fields, email format, duplicate email/username/studentCode in the file or the system, faculty code exists and is active, role exists, student fields filled only for Student rows). " +
                "All-or-nothing: if any row is invalid nothing is created and isSuccess = false with the list of invalid rows; otherwise every account is created and their ids are returned, ready for sending the linking emails.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- A non-empty .xlsx or .csv file is required.\n- Only .xlsx and .csv files can be imported.\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Import([FromForm] ImportAccountRequest request)
        {
            var response = await _accountService.ImportAsync(request);
            return Ok(response);
        }

        // Preview an account import file
        [HttpPost("import/preview")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Preview an account import",
            Description = "Reads every row of an .xlsx or .csv file (same columns as the import) and runs the same checks as the import, without writing anything. " +
                "Returns the header row, the rows (up to 2000) and the problems per row number, so the web app can show an editable preview and send the corrected rows back as a .csv file.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- A non-empty .xlsx or .csv file is required.\n- Only .xlsx and .csv files can be imported.\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> PreviewImport([FromForm] ImportAccountRequest request)
        {
            var response = await _accountService.PreviewImportAsync(request);
            return Ok(response);
        }

        // Search the staff directory
        [HttpGet("staff/search")]
        [Authorize(Roles = "System Administrator,Data Protection Steward,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
        [SwaggerOperation(Summary = "[All staff roles] Search the staff directory",
            Description = "Finds active staff accounts by fullName or email (contains), optionally holding one of roleNames (comma separated, e.g. 'Academic Advisor,Wellbeing Advisor'). " +
                "Returns directory data only (accountId, full name, email, roles), at most 50 rows, so staff screens can pick a person instead of typing an Id. Students are never listed.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> SearchStaff([FromQuery] SearchStaffAccountRequest request)
        {
            var response = await _accountService.SearchStaffAsync(request);
            return Ok(response);
        }

        // Send or resend account-linking emails
        [HttpPost("linking-emails")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Send account-linking emails",
            Description = "Emails each given account a one-time linking link (valid for 7 days; a new email replaces the old link) and sets linkStatus = emailSent. " +
                "Accounts that are already linked, not active or not found are skipped; accounts whose email could not be sent are listed as failed.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> SendLinkingEmails([FromBody] SendLinkingEmailRequest request)
        {
            var response = await _accountService.SendLinkingEmailsAsync(request);
            return Ok(response);
        }

        // Activate an Account
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Activate account",
            Description = "Sets an account's status back to active, for example to restore a deactivated account.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            var response = await _accountService.ActivateAsync(id);
            return Ok(response);
        }

        // Deactivate an Account
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Deactivate account",
            Description = "Sets an account's status to deactivated: sign-in is refused and every active session ends immediately. An admin cannot deactivate their own account.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You cannot deactivate your own account.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.DeactivateAsync(actorId, id);
            return Ok(response);
        }

        // Ban an Account
        [HttpPut("{id}/ban")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Ban account",
            Description = "Sets an account's status to banned, typically for a policy violation: sign-in is refused and every active session ends immediately. An admin cannot ban their own account.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You cannot ban your own account.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Ban([FromRoute] Guid id)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _accountService.BanAsync(actorId, id);
            return Ok(response);
        }

        // Unban an Account
        [HttpPut("{id}/unban")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Unban account",
            Description = "Reverses a ban, setting the account's status back to active.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Unban([FromRoute] Guid id)
        {
            var response = await _accountService.UnbanAsync(id);
            return Ok(response);
        }

        // Get all account-role assignments (ACCOUNT_ROLE), with optional pagination
        [HttpGet("roles")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all account roles with optional pagination",
            Description = "Retrieves every account-role assignment with the account's full name, the role name and when it was assigned. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
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
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get account role",
            Description = "Retrieves one account-role assignment. Returns 404 when the account does not have that role.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- This account does not have this role.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAccountRoleById([FromRoute] Guid accountId, [FromRoute] Guid roleId)
        {
            var response = await _accountService.GetAccountRoleByIdAsync(accountId, roleId);
            return Ok(response);
        }

        // Search account-role assignments (ACCOUNT_ROLE) by account or role
        [HttpGet("roles/search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search account roles",
            Description = "Filters account-role assignments by account (accountId) and/or role (roleId), e.g. to list every Programme Head. Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> SearchAccountRole([FromQuery] SearchAccountRoleRequest request)
        {
            var response = await _accountService.SearchAccountRoleAsync(request);
            return Ok(response);
        }

        // Give an Account a role (ACCOUNT_ROLE)
        [HttpPost("roles")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Add a role to an account",
            Description = "Gives an account one more role. An account can hold several roles; the same role cannot be added twice. The new role is in the JWT from the account's next sign-in. " +
                "Giving the Student role to an account without a student profile also needs studentCode, which creates the profile (academicStatus is optional and defaults to enrolled).")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.\n- Role not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This account already has the role '{RoleName}'.\n- A student with student code '{studentCode}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateAccountRole([FromBody] CreateAccountRoleRequest request)
        {
            var response = await _accountService.CreateAccountRoleAsync(request);
            return Ok(response);
        }

        // Change an Account's roles in one step
        [HttpPut("{id}/roles")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Change the roles of an account",
            Description = "Sets the account's roles to exactly roleNames (roles not listed are removed, new ones are added). Adding the Student role to an account without a student profile needs studentCode; " +
                "the profile starts as enrolled. The last System Administrator role in the system cannot be removed. The change is in the JWT from the account's next sign-in.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- An account needs at least one role.\n- The account already has exactly these roles.\n- A student code is required to give this account the Student role.\n- Student code must be under 50 characters.\n- Role '{roleName}' does not exist.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- The last System Administrator role cannot be removed.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A student with student code '{studentCode}' already exists.", typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateAccountRoles([FromRoute] Guid id, [FromBody] UpdateAccountRolesRequest request)
        {
            var response = await _accountService.UpdateAccountRolesAsync(id, request);
            return Ok(response);
        }

        // Remove a role from an Account (ACCOUNT_ROLE)
        [HttpDelete("{accountId}/roles/{roleId}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Remove a role from an account",
            Description = "Removes one role from an account. The last System Administrator role in the system cannot be removed, so the app always keeps an admin.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- The last System Administrator role cannot be removed.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- This account does not have this role.", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteAccountRole([FromRoute] Guid accountId, [FromRoute] Guid roleId)
        {
            var response = await _accountService.DeleteAccountRoleAsync(accountId, roleId);
            return Ok(response);
        }
    }
}
