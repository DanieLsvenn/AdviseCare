using AdviseCare.Application.Contracts.Requests.Session;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionsController(ISessionService sessionService) : ControllerBase
    {
        private readonly ISessionService _sessionService = sessionService;

        // Get all Sessions, with optional pagination
        [HttpGet]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get all sessions with optional pagination",
            Description = "Retrieves every session of every account, newest sign-in first. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _sessionService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _sessionService.GetAll());
        }

        // Get a Session by Id
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Get session by Id",
            Description = "Retrieves one session by its unique identifier.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _sessionService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Sessions by account or active state
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Search/Filter sessions",
            Description = "Filters sessions by account and by active state. Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchSessionRequest request)
        {
            var response = await _sessionService.SearchAsync(request);
            return Ok(response);
        }

        // Create a Session
        [HttpPost]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Create session",
            Description = "Creates an active session row by hand, for testing. Real sessions are opened by the sign-in endpoints in api/Auth.")]
        public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
        {
            var response = await _sessionService.CreateAsync(request);
            return Ok(response);
        }

        // Update a Session
        [HttpPut("{id}")]
        [Authorize(Roles = "System Administrator")]
        [SwaggerOperation(Summary = "[System Administrator] Update session",
            Description = "Turns a session on or off, for testing. Turning it off sets logoutAt to now, so its JWT stops working on the next request; turning it on clears logoutAt.")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSessionRequest request)
        {
            var response = await _sessionService.UpdateAsync(id, request);
            return Ok(response);
        }
    }
}
