using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.DataAttribute
{
    public class SearchDataAttributeRequest
    {
        public string AttributeName { get; set; } = string.Empty;
        public bool? IsSubjectSpecific { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateDataAttributeRequest
    {
        public string AttributeDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = "Attribute name is required."), MaxLength(255,
            ErrorMessage = "Attribute name must be under 255 characters.")]
        public string AttributeName { get; set; } = string.Empty;
        // true: every value belongs to one subject (e.g. Attendance rate per subject); false: one value per term
        public bool IsSubjectSpecific { get; set; } = false;
        [Required(ErrorMessage = "Data type is required."), RegularExpression("^(numeric|categorical|boolean)$",
            ErrorMessage = "Data type must be numeric, categorical or boolean.")]
        public string DataType { get; set; } = "numeric";
        // How the per-subject values of one term add up to the student's summary row: sum (e.g. Absent slots) or average (e.g. Subject mark);
        // only for a per-subject numeric data attribute; empty = none
        [RegularExpression("^(sum|average)$",
            ErrorMessage = "Summary method must be sum or average.")]
        public string SummaryMethod { get; set; } = string.Empty;
    }

    public class UpdateDataAttributeRequest
    {
        public string AttributeDescription { get; set; } = string.Empty;
        // null keeps the current summary method, empty removes it, sum or average sets it (per-subject numeric data attributes only)
        [RegularExpression("^(sum|average)$",
            ErrorMessage = "Summary method must be sum or average.")]
        public string? SummaryMethod { get; set; }
    }
}
