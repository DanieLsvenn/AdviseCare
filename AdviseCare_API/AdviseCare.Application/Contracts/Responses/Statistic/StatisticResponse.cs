namespace AdviseCare.Application.Contracts.Responses.Statistic
{
    // One cell of the welfare statistics (a support category, or a support category in one faculty); the counts are null when the cell is suppressed
    public class GetWelfareStatisticResponse
    {
        public Guid? FacultyId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public int? CaseCount { get; set; }
        public int? EscalationCount { get; set; }
        public int? ExternalReferralCount { get; set; }
        public int? InterventionCount { get; set; }
        public int? ReferredCaseCount { get; set; }
        public int? StudentCount { get; set; }
        public int? SupportGrantCount { get; set; }
        public bool IsSuppressed { get; set; } = false;
        public string SupportType { get; set; } = "academic";
    }

    public class ExportWelfareStatisticResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    // One cell of the cohort data (the whole cohort, or one faculty); the figures are null when the cell is suppressed
    public class GetCohortStatisticResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public double? AverageValue { get; set; }
        public double? MaxValue { get; set; }
        public double? MinValue { get; set; }
        public int? StudentCount { get; set; }
        public List<CohortValueCountResponse> ValueCounts { get; set; } = new();
        public bool IsSuppressed { get; set; } = false;
        public string DataType { get; set; } = "numeric";
    }

    // How many students have one value of a categorical or boolean attribute (values held by fewer than the threshold are left out)
    public class CohortValueCountResponse
    {
        public string Value { get; set; } = string.Empty;
        public int StudentCount { get; set; } = 0;
    }

    public class ExportCohortStatisticResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
