using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.SupportCategory
{
    public class SearchSupportCategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? ConsentPurposeId { get; set; }
    }

    public class CreateSupportCategoryRequest
    {
        [Required(ErrorMessage = "Consent purpose id is required.")]
        public Guid ConsentPurposeId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(255, ErrorMessage = "Category name must be at most 255 characters.")]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Support type is required (academic or wellbeing).")]
        public string SupportType { get; set; } = string.Empty;
    }

    public class UpdateSupportCategoryRequest : CreateSupportCategoryRequest
    {
        [Required(ErrorMessage = "Status is required (active or archived).")]
        public string Status { get; set; } = string.Empty;
    }
}
