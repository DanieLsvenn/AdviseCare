namespace AdviseCare.Application.Contracts.Responses.Report
{
    public class CaseReportRowResponse
    {
        public Guid CaseId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid? AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // Whole days from opening to closing, closed cases only
        public double? DaysToClose { get; set; }
    }

    public class CaseloadClosureRowResponse
    {
        public Guid AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int OpenCases { get; set; }
        public int ClosedCases { get; set; }
        // Average days from opening to closing, over the closed cases only; null when the advisor closed none
        public double? AverageDaysToClose { get; set; }
    }

    public class ReportFileResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
