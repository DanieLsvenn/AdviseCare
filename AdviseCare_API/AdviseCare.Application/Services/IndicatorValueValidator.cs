using System.Globalization;

namespace AdviseCare.Application.Services;

internal static class IndicatorValueValidator
{
    public static bool IsValid(string dataType, string value) => dataType switch
    {
        "numeric" => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        "boolean" => bool.TryParse(value, out _),
        "categorical" => !string.IsNullOrWhiteSpace(value),
        _ => false
    };
}
