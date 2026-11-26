using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.TakeOnRequest
{
    public class SearchTakeOnRequestRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? CaseId { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        [RegularExpression("^(pending|approved|declined|withdrawn|cancelled)$",
            ErrorMessage = "Status must be pending, approved, declined, withdrawn or cancelled.")]
        public string Status { get; set; } = string.Empty;
    }

    public class SearchMyTakeOnRequestRequest
    {
        [RegularExpression("^(pending|approved|declined|withdrawn|cancelled)$",
            ErrorMessage = "Status must be pending, approved, declined, withdrawn or cancelled.")]
        public string Status { get; set; } = string.Empty;
    }

    // Exactly one of CaseId (an unassigned student-created case) and IndicatorFlagId (an open flag)
    public class CreateTakeOnRequestRequest
    {
        public Guid? CaseId { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        [StringLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string Note { get; set; } = string.Empty;
    }

    public class UpdateTakeOnRequestRequest
    {
        [StringLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string Note { get; set; } = string.Empty;
    }

    public class DeclineTakeOnRequestRequest
    {
        [Required(ErrorMessage = "Decline reason is required."), StringLength(1000,
            ErrorMessage = "Decline reason cannot exceed 1000 characters.")]
        public string DeclineReason { get; set; } = string.Empty;
    }
}
