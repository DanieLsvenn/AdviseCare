using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Faculty
{
    public class SearchFacultyRequest
    {
        public string FacultyCode { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateFacultyRequest
    {
        [Required(ErrorMessage = "Faculty code is required."), MaxLength(255,
            ErrorMessage = "Faculty code must be under 255 characters.")]
        public string FacultyCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Faculty name is required."), MaxLength(255,
            ErrorMessage = "Faculty name must be under 255 characters.")]
        public string FacultyName { get; set; } = string.Empty;
    }

    public class UpdateFacultyRequest
    {
        [Required(ErrorMessage = "Faculty code is required."), MaxLength(255,
            ErrorMessage = "Faculty code must be under 255 characters.")]
        public string FacultyCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Faculty name is required."), MaxLength(255,
            ErrorMessage = "Faculty name must be under 255 characters.")]
        public string FacultyName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(active|archived)$",
            ErrorMessage = "Status must be active or archived.")]
        public string Status { get; set; } = "active";
    }
}
