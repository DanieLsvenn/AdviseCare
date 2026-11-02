using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Report
{
    // Cases opened between From and To (both dates inclusive, UTC)
    public class GetCaseReportRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? SupportCategoryId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        public string SupportType { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ExportCaseReportRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? SupportCategoryId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        public string SupportType { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        [Required(ErrorMessage = "Format is required."), RegularExpression("^(xlsx|csv|pdf)$",
            ErrorMessage = "Format must be xlsx, csv or pdf.")]
        public string Format { get; set; } = "xlsx";
    }

    // Caseload and closure time per advisor, over the cases opened between From and To (both dates inclusive, UTC)
    public class GetCaseloadClosureReportRequest
    {
        public Guid? AdvisorId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        public string SupportType { get; set; } = string.Empty;
    }

    public class ExportCaseloadClosureReportRequest
    {
        public Guid? AdvisorId { get; set; }
        [Required(ErrorMessage = "From date is required.")]
        public DateTime? From { get; set; }
        [Required(ErrorMessage = "To date is required.")]
        public DateTime? To { get; set; }
        public string SupportType { get; set; } = string.Empty;
        [Required(ErrorMessage = "Format is required."), RegularExpression("^(xlsx|csv|pdf)$",
            ErrorMessage = "Format must be xlsx, csv or pdf.")]
        public string Format { get; set; } = "xlsx";
    }
}
