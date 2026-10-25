using System.Text;

namespace AdviseCare.Application.Services;

internal static class CsvExportWriter
{
    public static byte[] WriteRows(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, headers);

        foreach (var row in rows)
            AppendRow(builder, row);

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();
    }

    public static string EscapeTextCell(string value)
    {
        var firstNonWhitespace = value.FirstOrDefault(character => !char.IsWhiteSpace(character));
        return firstNonWhitespace is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                builder.Append(',');

            builder.Append('"');
            builder.Append(values[index].Replace("\"", "\"\"", StringComparison.Ordinal));
            builder.Append('"');
        }

        builder.AppendLine();
    }
}
