using AdviseCare.Application.Interfaces.Excel;
using ClosedXML.Excel;

namespace AdviseCare.Infrastructure.Excel
{
    // Writes .xlsx files with ClosedXML (MIT licence); used by exports such as the processing record
    public class ExcelWriter : IExcelWriter
    {
        public byte[] WriteRows(string sheetName, List<string> headers, List<List<string>> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            for (var column = 0; column < headers.Count; column++)
                worksheet.Cell(1, column + 1).Value = headers[column];
            worksheet.Row(1).Style.Font.Bold = true;

            for (var row = 0; row < rows.Count; row++)
                for (var column = 0; column < rows[row].Count; column++)
                    worksheet.Cell(row + 2, column + 1).Value = rows[row][column];

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
