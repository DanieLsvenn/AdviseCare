using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AcademicResult
{
    // Transcript PDFs to read into an import batch (grade or attendance files, one student per file, mixed freely)
    public class UploadTranscriptsRequest
    {
        [Required(ErrorMessage = "At least one PDF file is required.")]
        public List<IFormFile> Files { get; set; } = new();
    }

    public class CommitTranscriptImportRequest
    {
        // Faculty of the subjects that are not in AdviseCare yet; required only when the batch has such subjects
        public Guid? FacultyId { get; set; }
    }

    public class GetAcademicResultsRequest
    {
        // Read through a case: only the case's advisor, only what is consented under the case category's purpose
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
    }
}
