namespace AdviseCare.Application.Contracts.Responses.AcademicTerm
{
    public class GetAllAcademicTermResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }

    public class GetAllAcademicTermWithPaginationResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }

    public class GetAcademicTermByIdResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }

    public class SearchAcademicTermResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }

    public class CreateAcademicTermResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }

    public class UpdateAcademicTermResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string Status { get; set; } = "upcoming";
    }
}
