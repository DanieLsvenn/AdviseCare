using System.Globalization;
using System.Text;
using AdviseCare.Application.Interfaces.Excel;

namespace AdviseCare.Infrastructure.Excel
{
    // Minimal dependency-free PDF writer for the Programme Head's report exports (D6).
    // Uses the built-in Courier font (fixed width, so columns line up without measuring text) on landscape A4.
    // The standard PDF fonts only cover Latin-1, so Vietnamese diacritics are removed (Nguyễn -> Nguyen).
    // If the team later adds a PDF library (e.g. QuestPDF), only this class has to change.
    public class SimplePdfWriter : IPdfWriter
    {
        private const double PageWidth = 842;   // A4 landscape, points
        private const double PageHeight = 595;
        private const double Margin = 36;
        private const double MaxFontSize = 8;
        private const double MinFontSize = 6;
        private const double GlyphWidth = 0.6; // Courier glyphs are 600/1000 em wide
        private const string ColumnGap = "  ";

        public byte[] WriteTable(string title, string subtitle, List<string> headers, List<List<string>> rows)
        {
            // Use a smaller font (down to 6 pt) for wide tables before cutting any text
            var natural = NaturalWidths(headers, rows);
            var needed = natural.Sum() + ColumnGap.Length * (natural.Length - 1);
            var fontSize = Math.Clamp((PageWidth - 2 * Margin) / (GlyphWidth * Math.Max(needed, 1)), MinFontSize, MaxFontSize);
            fontSize = Math.Floor(fontSize * 4) / 4;
            var lineHeight = fontSize * 1.375;
            var maxChars = (int)((PageWidth - 2 * Margin) / (fontSize * GlyphWidth));
            var widths = FitWidths(natural, maxChars);

            var headerLine = FormatRow(headers, widths);
            var ruleLine = new string('-', Math.Min(headerLine.Length, maxChars));
            var bodyLines = rows.Select(r => FormatRow(r, widths)).ToList();
            if (bodyLines.Count == 0)
                bodyLines.Add("No data for the selected filters.");

            // Lines that fit under the title block on each page
            var linesPerPage = (int)((PageHeight - 2 * Margin - 4 * lineHeight - 18) / lineHeight);
            var pages = new List<List<string>>();
            for (var i = 0; i < bodyLines.Count; i += linesPerPage)
                pages.Add(bodyLines.Skip(i).Take(linesPerPage).ToList());

            var contents = pages.Select((lines, index) =>
                BuildPageContent(title, $"{subtitle}   Page {index + 1}/{pages.Count}", headerLine, ruleLine, lines, fontSize, lineHeight)).ToList();
            return BuildDocument(contents);
        }

        private static int[] NaturalWidths(List<string> headers, List<List<string>> rows)
        {
            var widths = new int[headers.Count];
            for (var c = 0; c < headers.Count; c++)
            {
                var longest = rows.Select(r => c < r.Count ? Clean(r[c]).Length : 0).DefaultIfEmpty(0).Max();
                widths[c] = Math.Max(Clean(headers[c]).Length, Math.Min(longest, 40));
            }
            return widths;
        }

        private static int[] FitWidths(int[] natural, int maxChars)
        {
            var widths = (int[])natural.Clone();
            // Shrink the widest columns until the row fits the page
            var available = maxChars - ColumnGap.Length * (widths.Length - 1);
            while (widths.Sum() > available)
            {
                var widest = Array.IndexOf(widths, widths.Max());
                if (widths[widest] <= 4) break;
                widths[widest]--;
            }
            return widths;
        }

        private static string FormatRow(List<string> cells, int[] widths)
        {
            var parts = new List<string>();
            for (var c = 0; c < widths.Length; c++)
            {
                var text = c < cells.Count ? Clean(cells[c]) : string.Empty;
                if (text.Length > widths[c])
                    text = text[..Math.Max(widths[c] - 1, 0)] + "~";
                parts.Add(text.PadRight(widths[c]));
            }
            return string.Join(ColumnGap, parts).TrimEnd();
        }

        // Removes diacritics and anything outside printable ASCII (the standard fonts cannot draw it)
        private static string Clean(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var normalized = value.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (ch is '\r' or '\n' or '\t') builder.Append(' ');
                else builder.Append(ch >= 32 && ch <= 126 ? ch : '?');
            }
            return builder.ToString();
        }

        private static string Escape(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        private static string BuildPageContent(string title, string subtitle, string headerLine, string ruleLine, List<string> lines,
            double fontSize, double lineHeight)
        {
            var inv = CultureInfo.InvariantCulture;
            var builder = new StringBuilder();
            var y = PageHeight - Margin - 12;

            builder.Append("BT\n/F2 12 Tf\n");
            builder.AppendFormat(inv, "{0:0.##} {1:0.##} Td\n", Margin, y);
            builder.Append('(').Append(Escape(Clean(title))).Append(") Tj\nET\n");

            y -= 18;
            builder.Append("BT\n/F1 ").Append(fontSize.ToString(inv)).Append(" Tf\n");
            builder.AppendFormat(inv, "{0:0.##} {1:0.##} Td\n", Margin, y);
            builder.AppendFormat(inv, "{0:0.##} TL\n", lineHeight);
            builder.Append('(').Append(Escape(Clean(subtitle))).Append(") Tj\nT*\nT*\n");
            builder.Append('(').Append(Escape(headerLine)).Append(") Tj\nT*\n");
            builder.Append('(').Append(Escape(ruleLine)).Append(") Tj\n");
            foreach (var line in lines)
                builder.Append("T*\n(").Append(Escape(line)).Append(") Tj\n");
            builder.Append("ET\n");
            return builder.ToString();
        }

        // Objects: 1 catalog, 2 page tree, 3 Courier, 4 Courier-Bold, then a page + content stream pair per page
        private static byte[] BuildDocument(List<string> pageContents)
        {
            var inv = CultureInfo.InvariantCulture;
            var objects = new List<string>();
            var pageIds = Enumerable.Range(0, pageContents.Count).Select(i => 5 + i * 2).ToList();

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Count} >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>");
            for (var i = 0; i < pageContents.Count; i++)
            {
                var contentId = pageIds[i] + 1;
                objects.Add(string.Format(inv,
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0} {1}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {2} 0 R >>",
                    PageWidth, PageHeight, contentId));
                var stream = pageContents[i];
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
            }

            var output = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
                output.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(output.ToString());
            output.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
            output.Append("0000000000 65535 f \n");
            foreach (var offset in offsets)
                output.Append(offset.ToString("D10", inv)).Append(" 00000 n \n");
            output.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
            output.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

            return Encoding.ASCII.GetBytes(output.ToString());
        }
    }
}
