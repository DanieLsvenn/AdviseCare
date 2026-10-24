using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AccessLog
{
    public class SearchAccessLogRequest
    {
        public Guid? AccessorId { get; set; }
        public Guid? ConsentPurposeId { get; set; }
        public Guid? RecordedUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsFlagged { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReviewAccessLogRequest
    {
        [Required(ErrorMessage = "Review status is required."), RegularExpression("^(cleared|confirmedIssue)$",
            ErrorMessage = "Review status must be cleared or confirmedIssue.")]
        public string Status { get; set; } = "cleared";
    }

    public class SearchMyAccessLogRequest
    {
        public DateTime? FromDate { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? ToDate { get; set; }
    }

    public class ExportProcessingRecordRequest
    {
        public Guid? StudentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        [Required(ErrorMessage = "Format is required."), RegularExpression("^(xlsx|csv)$",
            ErrorMessage = "Format must be xlsx or csv.")]
        public string Format { get; set; } = "xlsx";
    }
}
