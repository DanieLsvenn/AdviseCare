using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.StudentData
{
    public class SearchStudentDataRequest
    {
        public Guid? DataAttributeId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? TermId { get; set; }
        public bool? IsMissing { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateStudentDataRequest
    {
        [Required(ErrorMessage = "An .xlsx or .csv file is required.")]
        public IFormFile? File { get; set; }
        // JSON: { "studentIdentifier": "...", "dataAttribute": "...", "value": "...", "term": "...", "validFrom": "...", "subject": "..." } (file column names);
        // "subject" is optional and only needed when the file has values of per-subject data attributes (the column holds subject codes, e.g. PRN212)
        [Required(ErrorMessage = "Column mapping is required.")]
        public string ColumnMapping { get; set; } = string.Empty;
    }

    public class PreviewStudentDataRequest
    {
        [Required(ErrorMessage = "An .xlsx or .csv file is required.")]
        public IFormFile? File { get; set; }
    }

    public class PreviewTranscriptsRequest
    {
        // Transcript PDFs (grade transcripts and attendance files, one student per file, mixed freely)
        [Required(ErrorMessage = "At least one PDF file is required.")]
        public List<IFormFile> Files { get; set; } = new();
    }

    public class ImportTranscriptsRequest
    {
        [Required(ErrorMessage = "At least one PDF file is required.")]
        public List<IFormFile> Files { get; set; } = new();
    }

    public class GetStudentDataAsOfRequest
    {
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        [Required(ErrorMessage = "Date is required.")]
        public DateOnly Date { get; set; }
        [RegularExpression("^(system|valid)$",
            ErrorMessage = "Time axis must be system or valid.")]
        public string TimeAxis { get; set; } = "system";
    }

    public class CompareStudentDataRequest
    {
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        [Required(ErrorMessage = "First date is required.")]
        public DateOnly DateA { get; set; }
        [Required(ErrorMessage = "Second date is required.")]
        public DateOnly DateB { get; set; }
        [RegularExpression("^(system|valid)$",
            ErrorMessage = "Time axis must be system or valid.")]
        public string TimeAxis { get; set; } = "system";
    }
}
