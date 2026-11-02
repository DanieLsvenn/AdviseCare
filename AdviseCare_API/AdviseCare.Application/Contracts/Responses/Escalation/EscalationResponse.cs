namespace AdviseCare.Application.Contracts.Responses.Escalation
{
    public class GetAllEscalationResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetAllEscalationWithPaginationResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetEscalationByIdResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class SearchEscalationResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class CreateEscalationResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class RespondEscalationResponse
    {
        public Guid EscalationId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid EscalatedBy { get; set; } = Guid.Empty;
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        public string EscalatedByFullName { get; set; } = string.Empty;
        public string EscalatedToFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = "pending";
    }
}
