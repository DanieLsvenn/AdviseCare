using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.TranscriptImportBatch
{
    public class SearchTranscriptImportBatchRequest
    {
        public Guid? ImportedBy { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateTranscriptImportBatchRequest
    {
        // Transcript PDFs (grade transcripts and attendance files, one student per file, mixed freely); at most 50 per request
        [Required(ErrorMessage = "At least one PDF file is required.")]
        public List<IFormFile> Files { get; set; } = new();
    }

    public class AddTranscriptImportFilesRequest
    {
        [Required(ErrorMessage = "At least one PDF file is required.")]
        public List<IFormFile> Files { get; set; } = new();
    }
}