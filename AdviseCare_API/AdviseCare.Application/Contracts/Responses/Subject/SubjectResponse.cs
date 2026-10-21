namespace AdviseCare.Application.Contracts.Responses.Subject
{
    public class GetAllSubjectResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetAllSubjectWithPaginationResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetSubjectByIdResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class SearchSubjectResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class CreateSubjectResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class UpdateSubjectResponse
    {
        public Guid SubjectId { get; set; } = Guid.Empty;
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetAllAccountSubjectResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class GetAllAccountSubjectWithPaginationResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class GetAccountSubjectByIdResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class SearchAccountSubjectResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class CreateAccountSubjectResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
