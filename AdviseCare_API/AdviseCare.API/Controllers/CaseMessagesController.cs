using AdviseCare.Application.Contracts.Requests.CaseMessage;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Case messages are append-only: there is no update or delete
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head")]
    public class CaseMessagesController(ICaseMessageService caseMessageService) : ControllerBase
    {
        private readonly ICaseMessageService _caseMessageService = caseMessageService;

        // Get all my Case messages, with optional pagination
        [HttpGet]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Get all my case messages with optional pagination",
            Description = "Lists the messages the signed-in account wrote on any case, newest first. Pass page and pageSize together to page the results.")]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _caseMessageService.GetAllWithPaginationAsync(
                    authorId,
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _caseMessageService.GetAll(authorId));
        }

        // Get a Case message by Id
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get case message by Id",
            Description = "Visible to the student of the case, its current advisor, the Programme Head, and anyone who sent or received a referral of the case.")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.GetByIdAsync(callerId, id);
            return Ok(response);
        }

        // Search Case messages of a case
        [HttpGet("search")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Get the message thread of a case",
            Description = "caseId is required. Returns every message of the case, oldest first, with its author and authorRole (student/advisor). " +
                "Same readers as GET /api/CaseMessages/{id}.")]
        public async Task<IActionResult> Search([FromQuery] SearchCaseMessageRequest request)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.SearchAsync(callerId, request);
            return Ok(response);
        }

        // Create a Case message
        [HttpPost]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Write a message on a case",
            Description = "Only the student of the case and its current advisor can write (1-1000 characters), and not on a closed case; a student can write once an advisor is assigned. " +
                "Messages cannot be edited or deleted. The other side is notified. Private advisor records stay in case notes.")]
        public async Task<IActionResult> Create([FromBody] CreateCaseMessageRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Create a Case message with files
        [HttpPost("with-files")]
        [Authorize(Roles = "Student,Academic Advisor,Wellbeing Advisor")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(60 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
        [SwaggerOperation(Summary = "[Student, Academic Advisor, Wellbeing Advisor] Write a message with files on a case",
            Description = "multipart/form-data: caseId, messageText (1-1000 characters) and up to 5 files (10 MB each; PDF, Word, Excel, PowerPoint, text, CSV, PNG, JPG, GIF, WEBP). " +
                "Same rules as POST /api/CaseMessages; the message and its files are saved together and cannot be edited or deleted.")]
        public async Task<IActionResult> CreateWithFiles([FromForm] CreateCaseMessageWithFilesRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.CreateWithFilesAsync(creatorId, request);
            return Ok(response);
        }

        // Download a file of a Case message
        [HttpGet("attachments/{id}")]
        [SwaggerOperation(Summary = "[Student, advisors, Student Welfare Officer, Programme Head] Download a file sent with a case message",
            Description = "Same readers as the thread (the student of the case, its current advisor, the Programme Head, and anyone who sent or received a referral of it); others get 404.")]
        public async Task<IActionResult> DownloadAttachment([FromRoute] Guid id)
        {
            var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _caseMessageService.DownloadAttachmentAsync(callerId, id);
            return File(response.Content, response.ContentType, response.FileName);
        }
    }
}
