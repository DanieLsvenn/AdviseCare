namespace AdviseCare.Application.Interfaces.Excel
{
    public interface IExcelWriter
    {
        // Writes one worksheet: the header row, then one row per item; returns the .xlsx file content
        byte[] WriteRows(string sheetName, List<string> headers, List<List<string>> rows);
    }
}
