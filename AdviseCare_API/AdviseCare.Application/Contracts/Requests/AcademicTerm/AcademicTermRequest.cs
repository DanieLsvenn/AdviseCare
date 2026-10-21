using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AcademicTerm
{
    public class SearchAcademicTermRequest
    {
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateAcademicTermRequest
    {
        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }
        [Required(ErrorMessage = "Term name is required."), MaxLength(255,
            ErrorMessage = "Term name must be under 255 characters.")]
        public string TermName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(upcoming|current|past)$",
            ErrorMessage = "Status must be upcoming, current or past.")]
        public string Status { get; set; } = "upcoming";
    }

    public class UpdateAcademicTermRequest
    {
        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }
        [Required(ErrorMessage = "Term name is required."), MaxLength(255,
            ErrorMessage = "Term name must be under 255 characters.")]
        public string TermName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(upcoming|current|past)$",
            ErrorMessage = "Status must be upcoming, current or past.")]
        public string Status { get; set; } = "upcoming";
    }
}
