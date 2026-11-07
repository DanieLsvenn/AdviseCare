namespace AdviseCare.Application.Contracts.Responses.Referral
{
    public class GetAllReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class GetAllReferralWithPaginationResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class GetReferralByIdResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class SearchReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class CreateReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class GetMyIncomingReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class GetMyOutgoingReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class AcceptReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }

    public class ReturnReferralResponse
    {
        public Guid ReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid FromAccountId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid ToAccountId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FromAccountFullName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string ToAccountFullName { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "pending";
    }
}
