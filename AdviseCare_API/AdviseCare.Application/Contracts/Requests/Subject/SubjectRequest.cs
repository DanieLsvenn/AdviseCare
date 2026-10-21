using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Subject
{
    public class SearchSubjectRequest
    {
        public Guid? FacultyId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSubjectRequest
    {
        [Required(ErrorMessage = "Faculty Id is required.")]
        public Guid FacultyId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Subject code is required."), MaxLength(255,
            ErrorMessage = "Subject code must be under 255 characters.")]
        public string SubjectCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Subject name is required."), MaxLength(255,
            ErrorMessage = "Subject name must be under 255 characters.")]
        public string SubjectName { get; set; } = string.Empty;
    }

    public class UpdateSubjectRequest
    {
        [Required(ErrorMessage = "Faculty Id is required.")]
        public Guid FacultyId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Subject code is required."), MaxLength(255,
            ErrorMessage = "Subject code must be under 255 characters.")]
        public string SubjectCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Subject name is required."), MaxLength(255,
            ErrorMessage = "Subject name must be under 255 characters.")]
        public string SubjectName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(active|archived)$",
            ErrorMessage = "Status must be active or archived.")]
        public string Status { get; set; } = "active";
    }

    public class SearchAccountSubjectRequest
    {
        public Guid? AccountId { get; set; }
        public Guid? SubjectId { get; set; }
    }

    public class CreateAccountSubjectRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Subject Id is required.")]
        public Guid SubjectId { get; set; } = Guid.Empty;
    }
}
