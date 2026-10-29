namespace AdviseCare.Application.Contracts.Responses.ExternalReferral
{
    public class GetAllExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class GetAllExternalReferralWithPaginationResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class GetExternalReferralByIdResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class SearchExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class CreateExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class UpdateExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }

    public class CloseExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public Guid ReferredBy { get; set; } = Guid.Empty;
        public string ExternalServiceName { get; set; } = string.Empty;
        public string FollowUpOutcome { get; set; } = string.Empty;
        public string ReferredByFullName { get; set; } = string.Empty;
        public DateTime? FollowUpDueAt { get; set; }
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; } = false;
        public string Status { get; set; } = "sent";
    }
}
