namespace AdviseCare.Application.Contracts.Responses.Intervention
{
    public class GetAllInterventionResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class GetAllInterventionWithPaginationResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class GetInterventionByIdResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class SearchInterventionResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class CreateInterventionResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class UpdateInterventionResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }

    public class CloseInterventionResponse
    {
        public Guid InterventionId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid OfficerId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        public string OfficerFullName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = "loanArrangement";
        public bool IsClosed { get; set; } = false;
        public string Status { get; set; } = "scheduled";
    }
}
