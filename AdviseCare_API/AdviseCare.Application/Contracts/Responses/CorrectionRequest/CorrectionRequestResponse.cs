namespace AdviseCare.Application.Contracts.Responses.CorrectionRequest
{
    public class GetAllCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetAllCorrectionRequestWithPaginationResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetCorrectionRequestByIdResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class SearchCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class CreateCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class ApproveCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class RejectCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetMyCorrectionRequestResponse
    {
        public Guid CorrectionRequestId { get; set; } = Guid.Empty;
        public Guid? ReviewedBy { get; set; }
        public Guid StudentDataId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? ProposedValue { get; set; }
        public string ReviewedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsErased { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string Status { get; set; } = "pending";
    }
}
