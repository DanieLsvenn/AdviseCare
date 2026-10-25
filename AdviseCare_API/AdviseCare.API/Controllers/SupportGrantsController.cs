using AdviseCare.Application.Contracts.Requests.SupportGrant;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student Welfare Officer")]
    public class SupportGrantsController(ISupportGrantService supportGrantService) : ControllerBase
    {
        private readonly ISupportGrantService _supportGrantService = supportGrantService;

        [HttpPost]
        [SwaggerOperation(Summary = "[Student Welfare Officer] Record a support grant for an open intervention")]
        public async Task<IActionResult> Create([FromBody] CreateSupportGrantRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _supportGrantService.CreateAsync(officerId, request));
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student Welfare Officer] List grants for an intervention")]
        public async Task<IActionResult> Search([FromQuery] SearchSupportGrantRequest request)
        {
            var officerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            return Ok(await _supportGrantService.SearchAsync(officerId, request));
        }
    }
}
