using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Programme
{
    public class SearchProgrammeRequest
    {
        public Guid? FacultyId { get; set; }
        public string ProgrammeCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateProgrammeRequest
    {
        [Required(ErrorMessage = "Faculty Id is required.")]
        public Guid FacultyId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Programme code is required."), MaxLength(255,
            ErrorMessage = "Programme code must be under 255 characters.")]
        public string ProgrammeCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Programme name is required."), MaxLength(255,
            ErrorMessage = "Programme name must be under 255 characters.")]
        public string ProgrammeName { get; set; } = string.Empty;
    }

    public class UpdateProgrammeRequest
    {
        [Required(ErrorMessage = "Faculty Id is required.")]
        public Guid FacultyId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Programme code is required."), MaxLength(255,
            ErrorMessage = "Programme code must be under 255 characters.")]
        public string ProgrammeCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Programme name is required."), MaxLength(255,
            ErrorMessage = "Programme name must be under 255 characters.")]
        public string ProgrammeName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(active|archived)$",
            ErrorMessage = "Status must be active or archived.")]
        public string Status { get; set; } = "active";
    }
}
