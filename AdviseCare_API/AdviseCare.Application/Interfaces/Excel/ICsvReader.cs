using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Excel
{
    public interface ICsvReader
    {
        List<ExcelRow> ReadRows(Stream content);
    }
}
