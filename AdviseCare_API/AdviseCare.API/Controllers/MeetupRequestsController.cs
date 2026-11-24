using AdviseCare.Application.Contracts.Requests.MeetupRequest;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Meet-up requests on a case between its student and its advisor (requests are answered, never deleted)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class MeetupRequestsController(IMeetupRequestService meetupRequestService) : ControllerBase
    {
        private readonly IMeetupRequestService _meetupRequestService = meetupRequestService;

        // Get all my Meetup requests, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Get all my meet-up requests with optional pagination",
            Description = "Lists the meet-up requests of the signed-in student (on their cases) or advisor (made with them), newest first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _meetupRequestService.GetAllWithPaginationAsync(
                    callerId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _meetupRequestService.GetAll(callerId));
        }

        // Get a Meetup request by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get meet-up request by Id",
            Description = "Visible to the student of the case, the advisor of the request, the Programme Head, and anyone who sent or received a referral of the case. Times are UTC; localDate, localStartTime and localEndTime are Vietnam time. awaitingResponseFrom is the account that must answer a pending request.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up request not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Meetup requests
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Search meet-up requests",
            Description = "With caseId: the requests of that case (same readers as the case). Without caseId: the signed-in student's or advisor's own requests. Optional status (pending/accepted/declined/cancelled) and fromDate/toDate (Vietnam dates, inclusive, on the proposed start). Newest first.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The end date cannot be before the start date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchMeetupRequestRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Meetup request
        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Request a meet-up on a case",
            Description = "The student of the case or its current advisor proposes a meet-up: meetupDate (yyyy-MM-dd), startTime (HH:mm) and durationMinutes, all Vietnam time: Monday to Friday, inside 08:30-11:30 or 13:30-18:00, 15-minute steps, at least 15 minutes, in the future; plus location (required), room (optional) and reason. " +
                "The case must not be closed and must be in stage contacted or followUp, with no other pending request; the time must not overlap a scheduled meet-up of the advisor or the student. The other side is notified and answers it.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Reason is required.\n- Location is required.\n- Start time must be in HH:mm format, e.g. 14:00.\n- Meet-ups can only be booked from Monday to Friday.\n- The start time must be on a 15-minute step (e.g. 14:00, 14:15, 14:30).\n- The duration must be a multiple of 15 minutes, at least 15 minutes.\n- A meet-up must start and end inside 08:30-11:30 or 13:30-18:00 (Vietnam time).\n- The meet-up time must be in the future.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the student of the case and its current advisor can request a meet-up.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.\n- Meet-up request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- The case is closed.\n- The case has no advisor yet.\n- Meet-ups can be arranged once the case is in stage contacted or followUp.\n- This case already has a meet-up request waiting for an answer. Answer or cancel it first.\n- The advisor already has a meet-up at {time}. Choose another time.\n- The student already has a meet-up at {time}. Choose another time.", typeof(ErrorResponse))]
        public async Task<IActionResult> Create([FromBody] CreateMeetupRequestRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Update a Meetup request (change the date)
        [HttpPut("{id}")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Change the date of a meet-up request",
            Description = "Only the side that must answer (awaitingResponseFrom) can change the date: it proposes another meetupDate, startTime, durationMinutes, location and room (same time rules as POST). The same request goes back to the other side (proposedBy changes, rescheduleCount + 1); there is no limit on how many times.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Location is required.\n- Start time must be in HH:mm format, e.g. 14:00.\n- Meet-ups can only be booked from Monday to Friday.\n- The start time must be on a 15-minute step (e.g. 14:00, 14:15, 14:30).\n- The duration must be a multiple of 15 minutes, at least 15 minutes.\n- A meet-up must start and end inside 08:30-11:30 or 13:30-18:00 (Vietnam time).\n- The meet-up time must be in the future.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You proposed this date; the other side must answer. You can cancel the request instead.\n- Only the student of the case and the advisor of the request can answer it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This meet-up request has already been answered or cancelled.\n- The case is closed.\n- The case has no advisor yet.\n- Meet-ups can be arranged once the case is in stage contacted or followUp.\n- The advisor already has a meet-up at {time}. Choose another time.\n- The student already has a meet-up at {time}. Choose another time.", typeof(ErrorResponse))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMeetupRequestRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.UpdateAsync(callerId, id, request);
            return Ok(response);
        }

        // Accept a Meetup request
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Accept a meet-up request",
            Description = "Only the side that must answer can accept. The meet-up is scheduled (GET /api/MeetupSchedules) and a case in stage contacted moves to followUp with a stage history row, in one save. The other side is notified.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You proposed this date; the other side must answer. You can cancel the request instead.\n- Only the student of the case and the advisor of the request can answer it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This meet-up request has already been answered or cancelled.\n- The proposed time has already passed. Change the date instead.\n- The case is closed.\n- The case has no advisor yet.\n- Meet-ups can be arranged once the case is in stage contacted or followUp.\n- The advisor already has a meet-up at {time}. Choose another time.\n- The student already has a meet-up at {time}. Choose another time.", typeof(ErrorResponse))]
        public async Task<IActionResult> Accept([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.AcceptAsync(callerId, id);
            return Ok(response);
        }

        // Decline a Meetup request
        [HttpPut("{id}/decline")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Decline a meet-up request",
            Description = "Only the side that must answer can decline, with a reason (required, max 1000 characters). The other side is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Decline reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- You proposed this date; the other side must answer. You can cancel the request instead.\n- Only the student of the case and the advisor of the request can answer it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- This meet-up request has already been answered or cancelled.\n- The case is closed.\n- The case has no advisor yet.\n- Meet-ups can be arranged once the case is in stage contacted or followUp.", typeof(ErrorResponse))]
        public async Task<IActionResult> Decline([FromRoute] Guid id, [FromBody] DeclineMeetupRequestRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.DeclineAsync(callerId, id, request);
            return Ok(response);
        }

        // Cancel a Meetup request
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Cancel my meet-up request",
            Description = "The side that proposed the current date withdraws a pending request. The other side is notified.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the side that proposed the current date can cancel the request. You can decline it instead.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up request not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a pending meet-up request can be cancelled.", typeof(ErrorResponse))]
        public async Task<IActionResult> Cancel([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.CancelAsync(callerId, id);
            return Ok(response);
        }
    }
}
