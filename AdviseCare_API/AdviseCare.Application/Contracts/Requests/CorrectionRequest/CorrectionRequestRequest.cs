using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CorrectionRequest
{
    public class SearchCorrectionRequestRequest
    {
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateCorrectionRequestRequest
    {
        [Required(ErrorMessage = "Student data row is required.")]
        public Guid StudentDataId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Explanation is required.")]
        public string Explanation { get; set; } = string.Empty;
        [Required(ErrorMessage = "Proposed value is required.")]
        public string ProposedValue { get; set; } = string.Empty;
    }

    public class ReviewCorrectionRequestRequest
    {
        [Required(ErrorMessage = "Decision notes are required.")]
        public string DecisionNotes { get; set; } = string.Empty;
    }
}
