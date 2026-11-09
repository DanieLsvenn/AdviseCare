using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Statistic
{
    // Welfare statistics over the records dated between From and To (both dates inclusive, UTC)
    public class GetWelfareStatisticRequest
    {
        public Guid? FacultyId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        [RegularExpression("^(|academic|wellbeing)$",
            ErrorMessage = "Support type must be academic or wellbeing.")]
        public string SupportType { get; set; } = string.Empty;
        public bool IsByFaculty { get; set; } = false;
    }

    public class ExportWelfareStatisticRequest
    {
        public Guid? FacultyId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        [RegularExpression("^(|academic|wellbeing)$",
            ErrorMessage = "Support type must be academic or wellbeing.")]
        public string SupportType { get; set; } = string.Empty;
        public bool IsByFaculty { get; set; } = false;
        [Required(ErrorMessage = "Format is required."), RegularExpression("^(xlsx|csv|pdf)$",
            ErrorMessage = "Format must be xlsx, csv or pdf.")]
        public string Format { get; set; } = "xlsx";
    }

    // Cohort data of one data attribute in one term, from the values consented to under the statistics purpose
    public class GetCohortStatisticRequest
    {
        [Required(ErrorMessage = "Data attribute Id is required.")]
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        [Required(ErrorMessage = "Term Id is required.")]
        public Guid TermId { get; set; } = Guid.Empty;
        public bool IsByFaculty { get; set; } = false;
    }

    public class ExportCohortStatisticRequest
    {
        [Required(ErrorMessage = "Data attribute Id is required.")]
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        [Required(ErrorMessage = "Term Id is required.")]
        public Guid TermId { get; set; } = Guid.Empty;
        public bool IsByFaculty { get; set; } = false;
        [Required(ErrorMessage = "Format is required."), RegularExpression("^(xlsx|csv|pdf)$",
            ErrorMessage = "Format must be xlsx, csv or pdf.")]
        public string Format { get; set; } = "xlsx";
    }
}
