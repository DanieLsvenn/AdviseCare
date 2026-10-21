namespace AdviseCare.Application.DTOs
{
    public class ExcelRow
    {
        public int RowNumber { get; set; } = 0;
        // Cell values by header name (header names are matched case-insensitively)
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
