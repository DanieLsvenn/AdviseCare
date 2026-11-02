using System.Globalization;

namespace AdviseCare.Application.DTOs
{
    // Checks that a value fits its data attribute's data type (used by the student data import and by correction requests)
    public static class DataValueValidator
    {
        public static bool IsValid(string DataType, string Value)
        {
            return DataType switch
            {
                "numeric" => decimal.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                "boolean" => bool.TryParse(Value, out _),
                "categorical" => !string.IsNullOrWhiteSpace(Value),
                _ => false
            };
        }
    }
}
