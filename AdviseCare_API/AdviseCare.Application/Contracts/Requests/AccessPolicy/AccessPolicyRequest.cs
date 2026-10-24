using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AccessPolicy
{
    public class SearchAccessPolicyRequest
    {
        public Guid? ConsentPurposeId { get; set; }
        public Guid? RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateAccessPolicyRequest
    {
        [Required(ErrorMessage = "Consent purpose Id is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Role Id is required.")]
        public Guid RoleId { get; set; } = Guid.Empty;
    }

    public class UpdateAccessPolicyRequest
    {
        [Required(ErrorMessage = "Consent purpose Id is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Role Id is required.")]
        public Guid RoleId { get; set; } = Guid.Empty;
    }

    public class TestAccessPolicyRequest
    {
        [Required(ErrorMessage = "Student Id is required.")]
        public Guid StudentId { get; set; } = Guid.Empty;
    }
}
