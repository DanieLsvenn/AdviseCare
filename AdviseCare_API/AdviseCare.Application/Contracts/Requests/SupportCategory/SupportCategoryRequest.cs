using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.SupportCategory
{
    public class SearchSupportCategoryRequest
    {
        public Guid? ConsentPurposeId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSupportCategoryRequest
    {
        [Required(ErrorMessage = "Consent purpose is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Category name is required."), MaxLength(255,
            ErrorMessage = "Category name must be under 255 characters.")]
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Support type is required."), RegularExpression("^(academic|wellbeing)$",
            ErrorMessage = "Support type must be academic or wellbeing.")]
        public string SupportType { get; set; } = "academic";
    }

    public class UpdateSupportCategoryRequest
    {
        [Required(ErrorMessage = "Consent purpose is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Category name is required."), MaxLength(255,
            ErrorMessage = "Category name must be under 255 characters.")]
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Support type is required."), RegularExpression("^(academic|wellbeing)$",
            ErrorMessage = "Support type must be academic or wellbeing.")]
        public string SupportType { get; set; } = "academic";
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(active|archived)$",
            ErrorMessage = "Status must be active or archived.")]
        public string Status { get; set; } = "active";
    }
}
