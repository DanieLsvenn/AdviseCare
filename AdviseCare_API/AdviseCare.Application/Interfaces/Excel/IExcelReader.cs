using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Excel
{
    public interface IExcelReader
    {
        // Reads the first worksheet: the first used row is the header, every other used row becomes one ExcelRow
        List<ExcelRow> ReadRows(Stream content);
    }
}
