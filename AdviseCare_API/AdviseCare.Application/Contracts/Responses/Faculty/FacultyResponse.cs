namespace AdviseCare.Application.Contracts.Responses.Faculty
{
    public class GetAllFacultyResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetAllFacultyWithPaginationResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class GetFacultyByIdResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class SearchFacultyResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class CreateFacultyResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class UpdateFacultyResponse
    {
        public Guid FacultyId { get; set; } = Guid.Empty;
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }
}
