using AdviseCare.Application.Contracts.Requests.StudentIndicator;
using AdviseCare.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdviseCare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "System Administrator")]
    public class StudentIndicatorsController(IStudentIndicatorImportService importService) : ControllerBase
    {
        private readonly IStudentIndicatorImportService _importService = importService;

        [HttpPost("import/preview")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Preview student indicator import file",
            Description = "Reads the header and up to five sample rows from an .xlsx or .csv file. Does not write data.")]
        public async Task<IActionResult> PreviewImport([FromForm] PreviewStudentIndicatorImportRequest request)
        {
            return Ok(await _importService.PreviewAsync(request));
        }

        [HttpPost("import/validate")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "[System Administrator] Validate a student indicator import",
            Description = "Validates column mapping and every row in an .xlsx or .csv file. This endpoint does not insert or update student indicator data.")]
        public async Task<IActionResult> ValidateImport([FromForm] ValidateStudentIndicatorImportRequest request)
        {
            return Ok(await _importService.ValidateAsync(request));
        }
    }
}
