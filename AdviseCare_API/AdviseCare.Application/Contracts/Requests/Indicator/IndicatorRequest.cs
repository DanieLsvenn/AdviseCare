using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Indicator
{
    public class SearchIndicatorRequest
    {
        public string AttributeName { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateIndicatorRequest
    {
        public string AttributeDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = "Attribute name is required."), MaxLength(255,
            ErrorMessage = "Attribute name must be under 255 characters.")]
        public string AttributeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Indicator name is required."), MaxLength(255,
            ErrorMessage = "Indicator name must be under 255 characters.")]
        public string IndicatorName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Data type is required."), RegularExpression("^(numeric|categorical|boolean)$",
            ErrorMessage = "Data type must be numeric, categorical or boolean.")]
        public string DataType { get; set; } = "numeric";
    }

    public class UpdateIndicatorRequest
    {
        public string AttributeDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
