using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
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
    public class CorrectionRequestsController(ICorrectionRequestService correctionRequestService) : ControllerBase
    {
        private const string AdvisorRoles = "Academic Advisor,Wellbeing Advisor";
        private readonly ICorrectionRequestService _correctionRequestService = correctionRequestService;

        // Get all Correction requests of the advisor's case students, with optional pagination
        [HttpGet]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get all correction requests with optional pagination",
            Description = "Lists the correction requests of the students the advisor has an open case with, newest first. The value on record and the proposed value are shown only when the access guard allows the data attribute (consent or emergency access); each request shown with its values is written to the access log. " +
                "Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _correctionRequestService.GetAllWithPaginationAsync(
                    advisorId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _correctionRequestService.GetAll(advisorId));
        }

        // Get all of the signed-in student's own Correction requests
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get own correction requests",
            Description = "Lists the signed-in student's own correction requests, newest first, with their values and the advisor's decision.")]
        public async Task<IActionResult> GetMyAll()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.GetMyAllAsync(studentId);
            return Ok(response);
        }

        // Get one of the signed-in student's own Correction requests
        [HttpGet("me/{id}")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get own correction request by Id",
            Description = "Retrieves one of the signed-in student's own correction requests with its status and the advisor's decision notes.")]
        public async Task<IActionResult> GetMyById([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.GetMyByIdAsync(studentId, id);
            return Ok(response);
        }

        // Get a Correction request by Id
        [HttpGet("{id}")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get correction request by Id",
            Description = "Retrieves one correction request of a student the advisor has an open case with (values shown only when the access guard allows, and logged).")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.GetByIdAsync(advisorId, id);
            return Ok(response);
        }

        // Search Correction requests
        [HttpGet("search")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Search/Filter correction requests",
            Description = "Filters the correction requests of the advisor's case students by student and status (pending/approved/rejected/withdrawn). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchCorrectionRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.SearchAsync(advisorId, request);
            return Ok(response);
        }

        // Create a Correction request
        [HttpPost]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Request a correction",
            Description = "Disputes one of the student's current values (studentDataId) with an explanation and the proposed value, which must fit the data attribute's data type. " +
                "Only the latest value of an attribute and term can be disputed, and only one pending request per value is allowed. The value on record and the proposed value are stored encrypted.")]
        public async Task<IActionResult> Create([FromBody] CreateCorrectionRequestRequest request)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.CreateAsync(studentId, request);
            return Ok(response);
        }

        // Approve a Correction request
        [HttpPut("{id}/approve")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Approve a correction request",
            Description = "Inserts a new 'correction' student data row with the proposed value and the original's valid-from date; the original row stays as history. " +
                "Only the advisor of one of the student's open cases may review, and the student must have shared the attribute with them. The student is notified.")]
        public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ReviewCorrectionRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.ApproveAsync(advisorId, id, request);
            return Ok(response);
        }

        // Reject a Correction request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Reject a correction request",
            Description = "Records the reviewer and the decision notes (required), and notifies the student.")]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] ReviewCorrectionRequestRequest request)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.RejectAsync(advisorId, id, request);
            return Ok(response);
        }

        // Withdraw a Correction request
        [HttpPut("{id}/withdraw")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Withdraw a correction request",
            Description = "Cancels one of the student's own pending correction requests.")]
        public async Task<IActionResult> Withdraw([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.WithdrawAsync(studentId, id);
            return Ok(response);
        }
    }
}
