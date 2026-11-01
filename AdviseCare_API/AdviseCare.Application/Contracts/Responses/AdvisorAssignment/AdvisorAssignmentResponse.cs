namespace AdviseCare.Application.Contracts.Responses.AdvisorAssignment
{
    public class AdvisorAssignmentResponse
    {
        public Guid AdvisorAssignmentId { get; set; }
        public Guid AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public Guid AssignedBy { get; set; }
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        // Cases of this advisor and student that are not closed
        public int OpenCaseCount { get; set; }
    }

    // Result of a direct reassignment (EX6 step 5) or an approved change request (EX6 step 3.1)
    public class AdvisorHandoverResponse
    {
        public AdvisorAssignmentResponse RevokedAssignment { get; set; } = new();
        public AdvisorAssignmentResponse NewAssignment { get; set; } = new();
        // Open cases of the old pair that moved to the new advisor
        public List<Guid> MovedCaseIds { get; set; } = new();
        // Open cases that stayed with the old advisor: the new advisor lacks the role for their support type, or a referral is pending
        public List<Guid> KeptCaseIds { get; set; } = new();
    }
}
