using AdviseCare.Application.Contracts.Requests.StudentData;
using AdviseCare.Application.Contracts.Responses;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace AdviseCare.API.Controllers
{
    // Student data (values of data attributes): import by the admin, consent-guarded reads by staff, the student's own view and export
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentDataController(IStudentDataService studentDataService) : ControllerBase
    {
        private const string ReaderRoles = "Academic Advisor,Wellbeing Advisor,Student Welfare Officer,Programme Head";
        private readonly IStudentDataService _studentDataService = studentDataService;

        // Get all Student data rows, with optional pagination (never the values)
        [HttpGet]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Get all student data rows with optional pagination",
            Description = "Lists the rows of the student data ledger, newest first: student, data attribute, term, source note, both bitemporal times, missing/erased flags and status. " +
                "The values themselves are never returned here. Pass page and pageSize together to page the results.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- Page and page size must be at least 1.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                return Ok(await _studentDataService.GetAllWithPaginationAsync(
                    page.Value,
                    pageSize.Value));
            }

            return Ok(await _studentDataService.GetAll());
        }

        // Get the signed-in student's own data
        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Get own student data",
            Description = "Returns the signed-in student's current value of every data attribute per term, with its source, missing and erased notices. No consent is needed and nothing is logged.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyData([FromQuery] Guid? termId)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.GetMyDataAsync(studentId, termId);
            return Ok(response);
        }

        // Export the signed-in student's own record
        [HttpGet("me/export")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "[Student] Export own record as JSON",
            Description = "Downloads a JSON file with the student's profile, current data, own cases and own access log, generated on the fly. The export is recorded in the activity log (dataExported).")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.\n- Your student profile has not been set up yet.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> ExportMyData()
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.ExportMyDataAsync(studentId);
            return File(response.Content, response.ContentType, response.FileName);
        }

        // Get a Student data row by Id (never the value)
        [HttpGet("{id}")]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Get student data row by Id",
            Description = "Retrieves one row of the student data ledger without its value.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student data row not found.", typeof(ErrorResponse))]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var response = await _studentDataService.GetByIdAsync(id);
            return Ok(response);
        }

        // Search Student data rows
        [HttpGet("search")]
        [Authorize(Roles = "System Administrator,Data Protection Steward")]
        [SwaggerOperation(Summary = "[System Administrator, Data Protection Steward] Search/Filter student data rows",
            Description = "Filters ledger rows by data attribute, student, term, missing flag and status (original/correction), e.g. every missing value of a term after an import. The values are never returned.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Search([FromQuery] SearchStudentDataRequest request)
        {
            var response = await _studentDataService.SearchAsync(request);
            return Ok(response);
        }

        // Import Student data from a file
        [HttpPost("import")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Import student data",
            Description = "Validates the whole .xlsx/.csv file with its column mapping, then encrypts every value with the student's key for that data attribute and inserts it as a new row; " +
                "every enrolled student without a value for an imported attribute/term gets a missing row; everything is saved together. Any invalid row fails the whole import (isSuccess = false with the invalid rows). " +
                "Rows of archived data attributes are not imported but listed in rejectedRows; the other rows still go in.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Column mapping must be a JSON object.\n- Column mapping must be valid JSON.\n- A non-empty .xlsx or .csv file is required.\n- Only .xlsx and .csv files can be imported.\n- The file could not be read as {extension} data.\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> Import([FromForm] CreateStudentDataRequest request)
        {
            var creatorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.CreateAsync(creatorId, request);
            return Ok(response);
        }

        // Preview an import file
        [HttpPost("import/preview")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Preview a student data import file",
            Description = "Reads the header row and up to five sample rows of an .xlsx or .csv file so the web app can show the column mapping. Nothing is written.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- The file has no data rows under the header row.\n- A non-empty .xlsx or .csv file is required.\n- Only .xlsx and .csv files can be imported.\n- The file could not be read as {extension} data.\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> PreviewImport([FromForm] PreviewStudentDataRequest request)
        {
            var response = await _studentDataService.PreviewAsync(request);
            return Ok(response);
        }

        // Validate an import file without writing
        [HttpPost("import/validate")]
        [Authorize(Roles = "System Administrator")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Validate a student data import",
            Description = "Checks the column mapping and every row (student by email/username/student code, data attribute, value against its data type, term, valid-from date). Nothing is written.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).\n- Column mapping must be a JSON object.\n- Column mapping must be valid JSON.\n- A non-empty .xlsx or .csv file is required.\n- Only .xlsx and .csv files can be imported.\n- The file could not be read as {extension} data.\n- (details of the failing rows or file)", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.", typeof(ErrorResponse))]
        public async Task<IActionResult> ValidateImport([FromForm] CreateStudentDataRequest request)
        {
            var response = await _studentDataService.ValidateAsync(request);
            return Ok(response);
        }

        // Get a student's current data
        [HttpGet("students/{studentId}")]
        [Authorize(Roles = ReaderRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get a student's data",
            Description = "With caseId: only the case's advisor may read, and only attributes consented under the case category's purpose are shown. " +
                "Without caseId: attributes consented under any purpose the reader's role has an active policy for, or every attribute through an approved emergency access. " +
                "Attributes not shared come back as isShared = false with no value. Every value shown is written to the access log with its exact row.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetStudentData([FromRoute] Guid studentId, [FromQuery] Guid? caseId, [FromQuery] Guid? termId)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.GetStudentDataAsync(accessorId, studentId, caseId, termId);
            return Ok(response);
        }

        // Get a student's data as of a date
        [HttpGet("students/{studentId}/as-of")]
        [Authorize(Roles = ReaderRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Get a student's data as of a date",
            Description = "timeAxis = system: what AdviseCare knew at the end of that UTC date (latest row recorded by then). " +
                "timeAxis = valid: what was true on that date by today's knowledge (latest row valid by then, a later correction winning). Same consent rules and logging as the current view.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAsOf([FromRoute] Guid studentId, [FromQuery] GetStudentDataAsOfRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.GetAsOfAsync(accessorId, studentId, request);
            return Ok(response);
        }

        // Compare a student's data on two dates
        [HttpGet("students/{studentId}/compare")]
        [Authorize(Roles = ReaderRoles)]
        [SwaggerOperation(Summary = "[Academic Advisor, Wellbeing Advisor, Student Welfare Officer, Programme Head] Compare a student's data on two dates",
            Description = "Returns both snapshots side by side per data attribute and term on the chosen time axis (system or valid). Each snapshot is authorized and logged on its own.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "- One or more validation errors occurred (the errors field lists each invalid field).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "- No valid sign-in token (missing, expired, or its session was ended).", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "- Your role cannot use this route.\n- Only the advisor of this case may read the student's data through it.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "- Student not found.\n- Case not found.\n- Case not found for this student.", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "- A stored value does not belong to this student and data attribute.\n- A stored value could not be decrypted (it was changed or moved, or its master key version is missing).", typeof(ErrorResponse))]
        public async Task<IActionResult> Compare([FromRoute] Guid studentId, [FromQuery] CompareStudentDataRequest request)
        {
            var accessorId = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)!);
            var response = await _studentDataService.CompareAsync(accessorId, studentId, request);
            return Ok(response);
        }
    }
}
