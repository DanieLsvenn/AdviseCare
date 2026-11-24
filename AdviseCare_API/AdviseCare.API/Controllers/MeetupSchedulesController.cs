using AdviseCare.Application.Contracts.Requests.MeetupRequest;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Agreed meet-ups (one per accepted meet-up request); they are cancelled or marked, never deleted
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class MeetupSchedulesController(IMeetupRequestService meetupRequestService) : ControllerBase
    {
        private readonly IMeetupRequestService _meetupRequestService = meetupRequestService;

        // Get all my Meetup schedules, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Get all my meet-ups with optional pagination",
            Description = "Lists the scheduled, completed, missed and cancelled meet-ups of the signed-in student or advisor, soonest first. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _meetupRequestService.GetAllScheduleWithPaginationAsync(
                    callerId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _meetupRequestService.GetAllScheduleAsync(callerId));
        }

        // Get a Meetup schedule by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get meet-up by Id",
            Description = "Same readers as its request. Times are UTC; localDate, localStartTime and localEndTime are Vietnam time.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.GetScheduleByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Meetup schedules
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Search meet-ups (the advisor's schedule)",
            Description = "With caseId: the meet-ups of that case (same readers as the case). Without caseId: the signed-in student's or advisor's own meet-ups, used by the advisor's schedule screen (e.g. fromDate and toDate = one week). Optional status (scheduled/completed/missed/cancelled) and fromDate/toDate (Vietnam dates, inclusive). Soonest first.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The end date cannot be before the start date.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Case not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchMeetupScheduleRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.SearchScheduleAsync(callerId, request);
            return Ok(response);
        }

        // Cancel a Meetup schedule
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Cancel a meet-up",
            Description = "The student or the advisor of the meet-up cancels it with a reason (required, max 1000 characters) before it starts. The other side is notified.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Cancel reason is required.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the student and the advisor of the meet-up can cancel it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a scheduled meet-up can be cancelled.\n- The meet-up has already started. The advisor marks it as completed or missed instead.", typeof(ErrorResponse))]
        public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelMeetupScheduleRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.CancelScheduleAsync(callerId, id, request);
            return Ok(response);
        }

        // Mark a Meetup schedule as completed
        [HttpPut("{id}/complete")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Mark a meet-up as completed",
            Description = "Only the advisor of the meet-up, once it has started.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of the meet-up can mark it as completed or missed.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a scheduled meet-up can be marked as completed or missed.\n- The meet-up has not started yet.", typeof(ErrorResponse))]
        public async Task<IActionResult> Complete([FromRoute] Guid id)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.CompleteScheduleAsync(advisorId, id);
            return Ok(response);
        }

        // Mark a Meetup schedule as missed
        [HttpPut("{id}/missed")]
        [Authorize(Roles = "Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor] Mark a meet-up as missed",
            Description = "Only the advisor of the meet-up, once it has started. The student is notified.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of the meet-up can mark it as completed or missed.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Meet-up not found.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- Only a scheduled meet-up can be marked as completed or missed.\n- The meet-up has not started yet.", typeof(ErrorResponse))]
        public async Task<IActionResult> MarkMissed([FromRoute] Guid id)
        {
            var advisorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _meetupRequestService.MarkMissedScheduleAsync(advisorId, id);
            return Ok(response);
        }
    }
}
