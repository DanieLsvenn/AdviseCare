using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Excel
{
    public interface ICsvReader
    {
        // Reads a .csv file: the header row gives the column names, every other non-empty row becomes one ExcelRow
        List<ExcelRow> ReadRows(Stream content);
    }
}
