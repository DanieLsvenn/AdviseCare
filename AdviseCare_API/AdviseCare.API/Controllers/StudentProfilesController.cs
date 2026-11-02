using AdviseCare.Application.Contracts.Requests.StudentProfile;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Student profiles: the directory data (email, full name, username, faculty, student code, academic status) that every staff role may read at any time, with no consent and no access log
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentProfilesController(IStudentProfileService studentProfileService) : ControllerBase
    {
        private const string StaffRoles = "System Administrator,Data Protection Steward,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head";
        private readonly IStudentProfileService _studentProfileService = studentProfileService;

        // Get all Student profiles, with optional pagination
        [HttpGet]
        [Authorize(Roles = StaffRoles)]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward, Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get all student profiles with optional pagination",
            Description = "Retrieves every student's profile ordered by student code: email, full name, username, faculty, student code and academic status. " +
                "This directory data needs no consent and is not written to the access log. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _studentProfileService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _studentProfileService.GetAll());
        }

        // Get the signed-in student's own profile
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get own student profile",
            Description = "Retrieves the signed-in student's own profile (email, full name, username, faculty, student code, academic status).")]
        public async Task<IActionResult> GetMyProfile()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentProfileService.GetMyProfileAsync(studentId);
            return Ok(response);
        }

        // Get a Student profile by the student's account Id
        [HttpGet("{studentId}")]
        [Authorize(Roles = StaffRoles)]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward, Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get student profile by student Id",
            Description = "Retrieves one student's profile by the student's account Id. No consent is needed and no access log is written.")]
        public async Task<IActionResult> GetById([FromRoute] Guid studentId)
        {
            var response = await _studentProfileService.GetByIdAsync(studentId);
            return Ok(response);
        }

        // Search Student profiles
        [HttpGet("search")]
        [Authorize(Roles = StaffRoles)]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward, Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Search/Filter student profiles",
            Description = "Filters student profiles by faculty, full name or student code (contains) and academic status (enrolled/onLeave/withdrawn/graduated). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchStudentProfileRequest request)
        {
            var response = await _studentProfileService.SearchAsync(request);
            return Ok(response);
        }

        // Update a Student profile
        [HttpPut("{studentId}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update student profile",
            Description = "Updates the student code and academic status (enrolled/onLeave/withdrawn/graduated) of a Student account, e.g. when the student goes on leave or graduates. " +
                "The student code must stay unique. Returns 404 for an account without a student profile (staff accounts have none). The profile itself is created with the account.")]
        public async Task<IActionResult> Update([FromRoute] Guid studentId, [FromBody] UpdateStudentProfileRequest request)
        {
            var response = await _studentProfileService.UpdateAsync(studentId, request);
            return Ok(response);
        }
    }
}
