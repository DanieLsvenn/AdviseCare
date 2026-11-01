namespace AdviseCare.Application.Interfaces.Excel
{
    public interface IPdfWriter
    {
        // Writes a landscape A4 table report: title and subtitle on every page, then the header row and one line per row.
        // Returns the .pdf file content.
        byte[] WriteTable(string title, string subtitle, List<string> headers, List<List<string>> rows);
    }
}
