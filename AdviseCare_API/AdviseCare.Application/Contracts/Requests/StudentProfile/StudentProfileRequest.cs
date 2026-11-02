using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.StudentProfile
{
    public class SearchStudentProfileRequest
    {
        public Guid? FacultyId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = string.Empty;
    }

    public class UpdateStudentProfileRequest
    {
        [Required(ErrorMessage = "Student code is required."), MaxLength(50,
            ErrorMessage = "Student code must be under 50 characters.")]
        public string StudentCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Academic status is required."), RegularExpression("^(enrolled|onLeave|withdrawn|graduated)$",
            ErrorMessage = "Academic status must be enrolled, onLeave, withdrawn or graduated.")]
        public string AcademicStatus { get; set; } = "enrolled";
    }
}
