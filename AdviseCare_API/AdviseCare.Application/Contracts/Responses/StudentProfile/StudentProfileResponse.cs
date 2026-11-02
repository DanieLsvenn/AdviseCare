namespace AdviseCare.Application.Contracts.Responses.StudentProfile
{
    public class GetAllStudentProfileResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }

    public class GetAllStudentProfileWithPaginationResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }

    public class GetStudentProfileByIdResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }

    public class SearchStudentProfileResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }

    public class UpdateStudentProfileResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }

    public class GetMyStudentProfileResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }
}
