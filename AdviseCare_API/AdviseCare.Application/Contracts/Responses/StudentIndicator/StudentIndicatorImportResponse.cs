namespace AdviseCare.Application.Contracts.Responses.StudentIndicator
{
    public class PreviewStudentIndicatorImportResponse
    {
        public List<string> Headers { get; set; } = new();
        public List<StudentIndicatorImportPreviewRow> SampleRows { get; set; } = new();
    }

    public class StudentIndicatorImportPreviewRow
    {
        public int RowNumber { get; set; }
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ValidateStudentIndicatorImportResponse
    {
        public bool IsValid { get; set; }
        public int ValidRowCount { get; set; }
        public List<StudentIndicatorImportRowError> Errors { get; set; } = new();
    }

    public class StudentIndicatorImportRowError
    {
        public int RowNumber { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}
