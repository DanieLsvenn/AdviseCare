using AdviseCare.Application.Contracts.Requests.ErasureRequest;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Data Protection Steward")]
    public class ErasureRequestsController(IErasureRequestService erasureRequestService) : ControllerBase
    {
        private readonly IErasureRequestService _erasureRequestService = erasureRequestService;

        // Get all Erasure requests, with optional pagination
        [HttpGet]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get all erasure requests with optional pagination",
            Description = "Retrieves every erasure request, newest first, with the student, attribute, purpose of the withdrawn consent, key status and who executed it. " +
                "Requests are queued automatically when a student withdraws the last granted consent on an attribute that has stored values. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _erasureRequestService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _erasureRequestService.GetAll());
        }

        // Get an Erasure request by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Get erasure request by Id",
            Description = "Retrieves one erasure request.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _erasureRequestService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Erasure requests by consent, executing steward, student or status
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Search/Filter erasure requests",
            Description = "Filters erasure requests by consent, executing steward (executedBy), student and status (pending = the erasure queue, completed = the completion log). Empty fields are ignored.")]
        public async Task<IActionResult> Search([FromQuery] SearchErasureRequestRequest request)
        {
            var response = await _erasureRequestService.SearchAsync(request);
            return Ok(response);
        }

        // Execute an Erasure request
        [HttpPut("{id}/execute")]
        [SwaggerOperation(Summary = "[Data Protection Steward] Execute erasure",
            Description = "Verifies that the consent is still withdrawn and that the student has not shared the attribute again under another purpose, then destroys the student's key for that attribute. " +
                "The values become unreadable, while consents, consent history and access log stay for audit. The student is notified.")]
        public async Task<IActionResult> Execute([FromRoute] Guid id)
        {
            var stewardId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _erasureRequestService.ExecuteAsync(stewardId, id);
            return Ok(response);
        }
    }
}
