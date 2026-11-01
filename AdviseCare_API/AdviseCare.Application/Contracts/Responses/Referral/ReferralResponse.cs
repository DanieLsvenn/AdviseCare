namespace AdviseCare.Application.Contracts.Responses.Referral
{
    public class ReferralResponse
    {
        public Guid ReferralId { get; set; }
        public Guid CaseId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CasePriority { get; set; } = string.Empty;
        public Guid FromAccountId { get; set; }
        public string FromAccountName { get; set; } = string.Empty;
        public Guid ToAccountId { get; set; }
        public string ToAccountName { get; set; } = string.Empty;
        public string ReferralReason { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}
