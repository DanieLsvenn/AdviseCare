namespace AdviseCare.Application.Contracts.Responses.Programme
{
    public class GetAllProgrammeResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetAllProgrammeWithPaginationResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetProgrammeByIdResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class SearchProgrammeResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class CreateProgrammeResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class UpdateProgrammeResponse
    {
        public Guid ProgrammeId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }
}
