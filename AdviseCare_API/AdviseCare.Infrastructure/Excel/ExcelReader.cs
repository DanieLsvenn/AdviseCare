using AdviseCare.Application.DTOs;
using AdviseCare.Application.Interfaces.Excel;
using ClosedXML.Excel;

namespace AdviseCare.Infrastructure.Excel
{
    // Reads .xlsx files with ClosedXML (MIT licence); can be reused by every import (accounts, indicator data)
    public class ExcelReader : IExcelReader
    {
        public List<ExcelRow> ReadRows(Stream content)
        {
            using var workbook = new XLWorkbook(content);
            var worksheet = workbook.Worksheets.First();
            var usedRows = worksheet.RowsUsed().ToList();
            if (usedRows.Count == 0)
                return new List<ExcelRow>();

            // Header row: column number -> header name
            var headers = usedRows[0].CellsUsed()
                .ToDictionary(c => c.Address.ColumnNumber, c => c.GetFormattedString().Trim());

            return usedRows.Skip(1).Select(row =>
            {
                var excelRow = new ExcelRow { RowNumber = row.RowNumber() };
                foreach (var header in headers.Where(h => !string.IsNullOrEmpty(h.Value)))
                    excelRow.Values[header.Value] = row.Cell(header.Key).GetFormattedString().Trim();
                return excelRow;
            }).ToList();
        }
    }
}
