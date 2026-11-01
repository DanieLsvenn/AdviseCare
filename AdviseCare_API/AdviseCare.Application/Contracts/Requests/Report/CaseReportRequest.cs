using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Report
{
    // Cases opened between From and To (both dates inclusive, UTC)
    public class CaseReportRequest
    {
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }

        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }

        public Guid? AdvisorId { get; set; }
        public string SupportType { get; set; } = string.Empty;
        public Guid? SupportCategoryId { get; set; }
        public Guid? SubjectId { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CaseReportExportRequest : CaseReportRequest
    {
        // xlsx (default), csv or pdf
        public string Format { get; set; } = "xlsx";
    }

    public class CaseloadClosureRequest
    {
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }

        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }

        public Guid? AdvisorId { get; set; }
        public string SupportType { get; set; } = string.Empty;
    }

    public class CaseloadClosureExportRequest : CaseloadClosureRequest
    {
        // xlsx (default), csv or pdf
        public string Format { get; set; } = "xlsx";
    }
}
