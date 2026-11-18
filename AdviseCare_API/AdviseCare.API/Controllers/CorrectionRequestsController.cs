using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Contracts.Responses;
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

        // Get all Correction requests in the advisor's reviewer pool, with optional pagination
        [HttpGet]
        [Authorize(Roles = AdvisorRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Get all correction requests with optional pagination",
            Description = "Lists the correction requests in the advisor's reviewer pool, newest first: every request whose consent purpose has an active access policy for one of the advisor's roles (no case is needed). The value on record and the proposed value are shown only when the student still shares the attribute under that purpose; each request shown with its values is written to the access log. " +
                "Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Correction request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
            Description = "Retrieves one correction request of the advisor's reviewer pool (values shown only when the student still shares the attribute under the request's purpose, and logged).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Your role has no active access policy for this request's consent purpose, so it is not in your review pool.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Correction request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
            Description = "Filters the correction requests of the advisor's reviewer pool by student, consent purpose and status (pending/approved/rejected/withdrawn). Empty fields are ignored.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
            Description = "Disputes one of the student's current values (studentDataId) under a consent purpose (consentPurposeId) with an explanation and the proposed value, which must fit the data attribute's data type. No case is needed: submitting also grants consent on that one attribute under the purpose (an existing consent is kept), and the purpose must be active, cover the attribute and have an advisor role with an active access policy. " +
                "Only the latest value of an attribute and term can be disputed, and only one pending request per value is allowed. The value on record and the proposed value are stored encrypted.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The proposed value is not valid for the {DataType} data attribute '{AttributeName}'.\n- '{PurposeName}' is the statistics purpose; choose an advising purpose for a correction.\n- '{PurposeName}' does not cover this data attribute; choose a purpose that does.\n- The expiry date must be in the future.\n- Some data attributes are not covered by '{PurposeName}'.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only students can grant consent.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student data row not found.\n- Consent purpose not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A pending correction request already exists for this value.\n- '{PurposeName}' is not active, so a correction cannot be reviewed under it.\n- No advisor role has an active access policy for '{PurposeName}', so nobody could review the correction.\n- A missing or erased value cannot be corrected.\n- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).\n- Only the latest value of an attribute and term can be corrected.\n- '{PurposeName}' is not active, so consent cannot be given for it.\n- These data attributes are archived and cannot be shared any more: {string.Join(", typeof(ErrorResponse))]
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
                "Any advisor of the reviewer pool may review (the first decision wins), and the student must still share the attribute under the request's purpose. The student is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- The student no longer shares this data attribute under the request's consent purpose, so it cannot be reviewed.\n- Your role has no active access policy for this request's consent purpose, so it is not in your review pool.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student data row not found.\n- Correction request not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The value changed after the correction request was submitted.\n- A missing or erased value cannot be corrected.\n- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).\n- Only the latest value of an attribute and term can be corrected.\n- Only a pending correction request can be reviewed.\n- The value was erased, so the request can no longer be reviewed.", typeof(ErrorResponse))]
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
            Description = "Records the reviewer (the first advisor of the pool to decide) and the decision notes (required), and notifies the student.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- The student no longer shares this data attribute under the request's consent purpose, so it cannot be reviewed.\n- Your role has no active access policy for this request's consent purpose, so it is not in your review pool.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Correction request not found.\n- Account not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending correction request can be reviewed.\n- The value was erased, so the request can no longer be reviewed.\n- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
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
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Correction request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending correction request can be withdrawn.", typeof(ErrorResponse))]
        public async Task<IActionResult> Withdraw([FromRoute] Guid id)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _correctionRequestService.WithdrawAsync(studentId, id);
            return Ok(response);
        }
    }
}
