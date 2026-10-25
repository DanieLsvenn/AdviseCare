using AdviseCare.Application.DTOs;
using AdviseCare.Application.Interfaces.Excel;
using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace AdviseCare.Infrastructure.Excel
{
    public class CsvReader : ICsvReader
    {
        public List<ExcelRow> ReadRows(Stream content)
        {
            using var parser = new TextFieldParser(content, Encoding.UTF8, detectEncoding: true, leaveOpen: true)
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };

            if (parser.EndOfData)
                return new List<ExcelRow>();

            var headers = parser.ReadFields()
                ?? throw new InvalidDataException("The CSV file has no header row.");
            var normalizedHeaders = headers.Select(header => header.Trim()).ToArray();
            if (normalizedHeaders.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("CSV column headers cannot be empty.");
            if (normalizedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedHeaders.Length)
                throw new InvalidDataException("CSV column headers must be unique.");

            var rows = new List<ExcelRow>();
            while (!parser.EndOfData)
            {
                var rowNumber = checked((int)parser.LineNumber);
                string[]? fields;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException exception)
                {
                    throw new InvalidDataException($"Malformed CSV data near line {exception.LineNumber}.", exception);
                }

                if (fields == null || fields.All(string.IsNullOrWhiteSpace))
                    continue;
                if (fields.Length > normalizedHeaders.Length)
                    throw new InvalidDataException($"CSV row {rowNumber} contains more values than the header row.");

                var row = new ExcelRow { RowNumber = rowNumber };
                for (var index = 0; index < normalizedHeaders.Length; index++)
                    row.Values[normalizedHeaders[index]] = index < fields.Length ? fields[index].Trim() : string.Empty;
                rows.Add(row);
            }

            return rows;
        }
    }
}
