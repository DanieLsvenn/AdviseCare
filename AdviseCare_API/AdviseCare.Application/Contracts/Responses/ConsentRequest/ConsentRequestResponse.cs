namespace AdviseCare.Application.Contracts.Responses.ConsentRequest
{
    public class GetAllConsentRequestResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetAllConsentRequestWithPaginationResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetConsentRequestByIdResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class SearchConsentRequestResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class CreateConsentRequestResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class UpdateConsentRequestResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetMyConsentRequestResponse
    {
        public Guid ConsentRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string PurposeStated { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Status { get; set; } = "pending";
    }
}
