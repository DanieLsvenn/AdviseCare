using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AdviseCare.Application.Contracts.Requests.StudentIndicator
{
    public class PreviewStudentIndicatorImportRequest
    {
        [Required(ErrorMessage = "An .xlsx or .csv file is required.")]
        public IFormFile? File { get; set; }
    }

    public class ValidateStudentIndicatorImportRequest
    {
        [Required(ErrorMessage = "An .xlsx or .csv file is required.")]
        public IFormFile? File { get; set; }

        [Required(ErrorMessage = "Column mapping is required.")]
        public string ColumnMapping { get; set; } = string.Empty;
    }
}
