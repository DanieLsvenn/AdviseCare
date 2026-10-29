using AdviseCare.Application.Contracts.Responses.AccessLog;
using AdviseCare.Application.Contracts.Responses.StudentProfile;

namespace AdviseCare.Application.Contracts.Responses.StudentData
{
    public class GetAllStudentDataResponse
    {
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? DataSourceId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ProvenanceNote { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? ErasedAt { get; set; }
        public DateTime TransactionFrom { get; set; }
        public DateTime ValidFrom { get; set; }
        public bool IsMissing { get; set; } = false;
        public string Status { get; set; } = "original";
    }

    public class GetAllStudentDataWithPaginationResponse
    {
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? DataSourceId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ProvenanceNote { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? ErasedAt { get; set; }
        public DateTime TransactionFrom { get; set; }
        public DateTime ValidFrom { get; set; }
        public bool IsMissing { get; set; } = false;
        public string Status { get; set; } = "original";
    }

    public class GetStudentDataByIdResponse
    {
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? DataSourceId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ProvenanceNote { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? ErasedAt { get; set; }
        public DateTime TransactionFrom { get; set; }
        public DateTime ValidFrom { get; set; }
        public bool IsMissing { get; set; } = false;
        public string Status { get; set; } = "original";
    }

    public class SearchStudentDataResponse
    {
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? DataSourceId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ProvenanceNote { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? ErasedAt { get; set; }
        public DateTime TransactionFrom { get; set; }
        public DateTime ValidFrom { get; set; }
        public bool IsMissing { get; set; } = false;
        public string Status { get; set; } = "original";
    }

    public class CreateStudentDataResponse
    {
        public int InsertedCount { get; set; } = 0;
        public int MissingCount { get; set; } = 0;
        // Rows of archived data attributes: not imported, listed so the admin knows which ones were left out
        public List<StudentDataRowErrorResponse> RejectedRows { get; set; } = new();
        public List<StudentDataRowErrorResponse> Errors { get; set; } = new();
        public bool IsSuccess { get; set; } = false;
    }

    public class PreviewStudentDataResponse
    {
        public List<string> Headers { get; set; } = new();
        public List<StudentDataPreviewRowResponse> SampleRows { get; set; } = new();
    }

    public class StudentDataPreviewRowResponse
    {
        public int RowNumber { get; set; } = 0;
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ValidateStudentDataResponse
    {
        public int ValidRowCount { get; set; } = 0;
        public List<StudentDataRowErrorResponse> RejectedRows { get; set; } = new();
        public List<StudentDataRowErrorResponse> Errors { get; set; } = new();
        public bool IsValid { get; set; } = false;
    }

    public class StudentDataRowErrorResponse
    {
        public int RowNumber { get; set; } = 0;
        public List<string> Messages { get; set; } = new();
    }

    public class GetStudentDataOfStudentResponse
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        public List<StudentDataValueResponse> Values { get; set; } = new();
    }

    public class GetStudentDataAsOfResponse
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        public List<StudentDataValueResponse> Values { get; set; } = new();
        public DateOnly Date { get; set; }
        public string TimeAxis { get; set; } = "system";
    }

    public class CompareStudentDataResponse
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        public DateOnly DateA { get; set; }
        public DateOnly DateB { get; set; }
        public List<CompareStudentDataValueResponse> Values { get; set; } = new();
        public string TimeAxis { get; set; } = "system";
    }

    public class CompareStudentDataValueResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public StudentDataValueResponse? DateAValue { get; set; }
        public StudentDataValueResponse? DateBValue { get; set; }
    }

    public class GetMyStudentDataResponse
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? TermId { get; set; }
        public List<StudentDataValueResponse> Values { get; set; } = new();
    }

    // One value of one data attribute in one term, as shown to the reader (Value is null when not shared, missing or erased)
    public class StudentDataValueResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? DataSourceId { get; set; }
        public Guid? StudentDataId { get; set; }
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ProvenanceNote { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public DateTime? TransactionFrom { get; set; }
        public DateTime? ValidFrom { get; set; }
        public bool HasValue { get; set; } = false;
        public bool IsErased { get; set; } = false;
        public bool IsMissing { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class ExportMyStudentDataResponse
    {
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    // The JSON document of a student's own data export
    public class StudentDataExportDocumentResponse
    {
        public GetMyStudentProfileResponse Profile { get; set; } = new();
        public List<StudentDataValueResponse> Values { get; set; } = new();
        public List<StudentDataExportCaseResponse> Cases { get; set; } = new();
        public List<GetMyAccessLogResponse> AccessLogs { get; set; } = new();
        public DateTime ExportedAt { get; set; }
    }

    public class StudentDataExportCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
