namespace AdviseCare.Application.Contracts.Responses.Report
{
    public class GetCaseReportResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public double? DaysToClose { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class ExportCaseReportResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class GetCaseloadClosureReportResponse
    {
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public double? AverageDaysToClose { get; set; }
        public int ClosedCases { get; set; } = 0;
        public int OpenCases { get; set; } = 0;
        public int TotalCases { get; set; } = 0;
    }

    public class ExportCaseloadClosureReportResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
