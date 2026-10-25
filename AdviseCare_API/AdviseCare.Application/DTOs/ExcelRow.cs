namespace AdviseCare.Application.DTOs
{
    public class ExcelRow
    {
        public int RowNumber { get; set; } = 0;
        // Values by header name; headers are matched case-insensitively for spreadsheet and CSV imports.
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
