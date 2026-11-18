using AdviseCare.Application.DTOs;
using AdviseCare.Application.Interfaces.Excel;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AdviseCare.Infrastructure.Excel
{
    // Reads a transcript PDF laid out like the university's "ACADEMIC TRANSCRIPT" (grade file) or the AdviseCare attendance file
    // (same layout, the Grade column replaced by Attendance). The table is found from the words' positions, not from plain text,
    // so cells that wrap onto several lines ("Summer202" / "6", long subject names) are joined back:
    //   1. every word with its baseline and start/end x (PdfPig) -> lines (same baseline) -> phrases (words closer than ~0.6 of the font size)
    //   2. a header row is a line starting with "No." that also has "Semester"; each header phrase starts a column (cells are left-aligned)
    //   3. a subject row starts at a line with a whole number in the "No." column and takes the lines below it until the next number;
    //      the table ends at a line whose text does not fit the columns (section title, footer, end of page)
    // Only No., Term, Semester, Subject code, Subject name, Grade/Attendance and Status are read; other columns
    // (Prerequisites, Replacement, Credits) are ignored, so they may be present or not.
    public class TranscriptPdfReader : ITranscriptPdfReader
    {
        private const double BaselineTolerance = 1.5;
        private const double ColumnTolerance = 2.0;
        private const double PhraseGapRatio = 0.6;
        private const double WrappedHeaderGapRatio = 0.8;

        private static readonly Regex StudentCodePattern = new(@"Student\s*(?:ID|code)\s*:\s*([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        private static readonly Regex CurriculumPattern = new(@"Curriculum\s*:\s*(\S+)", RegexOptions.IgnoreCase);
        private static readonly Regex FullNamePattern = new(@"Full\s*name\s*:\s*(.+?)\s+(?:Student\s*(?:ID|code)|Curriculum)\s*:", RegexOptions.IgnoreCase);
        private static readonly Regex WholeNumber = new(@"^\d{1,6}$");

        private sealed class Phrase
        {
            public double Left { get; set; }
            public double Right { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private sealed class Line
        {
            // Distance of the baseline from the top of the page
            public double Baseline { get; set; }
            public double Size { get; set; }
            public List<Phrase> Phrases { get; } = new();
            public double Top => Baseline - Size * 0.8;
            public double Bottom => Baseline + Size * 0.2;
            public string Text => string.Join(" ", Phrases.Select(p => p.Text));
        }

        private sealed class Column
        {
            public double Left { get; set; }
            public List<string> HeaderParts { get; } = new();
            public string? Key { get; set; }
        }

        public ParsedTranscript Read(byte[] content)
        {
            var result = new ParsedTranscript();
            try
            {
                using var document = PdfDocument.Open(content);
                var inCurriculum = true;
                var hasText = false;
                var foundTable = false;
                foreach (var page in document.GetPages())
                {
                    var lines = ReadLines(page);
                    if (lines.Count == 0) continue;
                    hasText = true;
                    if (page.Number == 1) ReadHeader(lines, result);
                    foundTable |= ReadTables(lines, page.Number, ref inCurriculum, result);
                }

                if (!hasText)
                    result.Errors.Add("The PDF has no text layer (is it a scanned image?).");
                else if (!foundTable)
                    result.Errors.Add("No transcript table was found (a header row with No., Semester, Subject code, Grade or Attendance, Status).");
            }
            catch (Exception)
            {
                return new ParsedTranscript { Errors = { "The file could not be read as a PDF." } };
            }
            return result;
        }

        // Helper: the page's words grouped into lines (top to bottom) and phrases (left to right)
        private static List<Line> ReadLines(Page page)
        {
            var words = new List<(double Baseline, double Size, Phrase Word)>();
            foreach (var word in page.GetWords())
            {
                if (string.IsNullOrWhiteSpace(word.Text) || word.Letters.Count == 0) continue;
                var first = word.Letters[0];
                var last = word.Letters[word.Letters.Count - 1];
                var size = word.Letters.Max(l => l.PointSize);
                if (size <= 1) size = Math.Max(word.BoundingBox.Height * 1.4, 4);
                words.Add((page.Height - first.StartBaseLine.Y, size, new Phrase
                {
                    Left = first.StartBaseLine.X,
                    Right = Math.Max(last.EndBaseLine.X, word.BoundingBox.Right),
                    Text = word.Text.Trim()
                }));
            }

            var lines = new List<Line>();
            foreach (var word in words.OrderBy(w => w.Baseline).ThenBy(w => w.Word.Left))
            {
                var line = lines.Count > 0 && Math.Abs(lines[^1].Baseline - word.Baseline) <= BaselineTolerance ? lines[^1] : null;
                if (line == null)
                {
                    line = new Line { Baseline = word.Baseline, Size = word.Size };
                    lines.Add(line);
                }
                line.Size = Math.Max(line.Size, word.Size);
                line.Phrases.Add(word.Word);
            }

            // Words closer than a space-and-a-bit belong to the same cell text
            foreach (var line in lines)
            {
                var sorted = line.Phrases.OrderBy(p => p.Left).ToList();
                line.Phrases.Clear();
                foreach (var word in sorted)
                {
                    var previous = line.Phrases.Count > 0 ? line.Phrases[^1] : null;
                    if (previous != null && word.Left - previous.Right <= line.Size * PhraseGapRatio)
                    {
                        previous.Right = Math.Max(previous.Right, word.Right);
                        previous.Text += " " + word.Text;
                    }
                    else
                    {
                        line.Phrases.Add(new Phrase { Left = word.Left, Right = word.Right, Text = word.Text });
                    }
                }
            }
            return lines;
        }

        // Helper: "Full name: ... Student ID: ... Curriculum: ..." above the first table
        private static void ReadHeader(List<Line> lines, ParsedTranscript result)
        {
            var text = string.Join(" ", lines.TakeWhile(l => !IsTableHeader(l)).Take(12).Select(l => l.Text));
            var code = StudentCodePattern.Match(text);
            if (code.Success) result.StudentCode = code.Groups[1].Value.Trim();
            var curriculum = CurriculumPattern.Match(text);
            if (curriculum.Success) result.CurriculumCode = curriculum.Groups[1].Value.Trim();
            var name = FullNamePattern.Match(text);
            if (name.Success) result.FullName = name.Groups[1].Value.Trim().Normalize(NormalizationForm.FormC);
        }

        // Helper: every table of one page; returns whether a table header was found
        private static bool ReadTables(List<Line> lines, int pageNumber, ref bool inCurriculum, ParsedTranscript result)
        {
            var foundTable = false;
            var index = 0;
            while (index < lines.Count)
            {
                var line = lines[index];
                var upper = line.Text.ToUpperInvariant();
                if (upper.Contains("OUTSIDE THE CURRICULUM")) inCurriculum = false;
                else if (upper.StartsWith("CURRICULUM SUBJECTS")) inCurriculum = true;

                if (!IsTableHeader(line))
                {
                    index++;
                    continue;
                }
                foundTable = true;

                // Columns start where the header texts start; header cells may wrap onto the next lines ("Ter" / "m")
                var columns = line.Phrases.Select(p => new Column { Left = p.Left }).ToList();
                for (var c = 0; c < columns.Count; c++)
                    columns[c].HeaderParts.Add(line.Phrases[c].Text);
                var next = index + 1;
                while (next < lines.Count
                    && !StartsRow(lines[next], columns)
                    && lines[next].Top - lines[next - 1].Bottom < lines[next].Size * WrappedHeaderGapRatio)
                {
                    foreach (var phrase in lines[next].Phrases)
                    {
                        var column = ColumnOf(phrase.Left, columns);
                        if (column >= 0) columns[column].HeaderParts.Add(phrase.Text);
                    }
                    next++;
                }
                foreach (var column in columns)
                    column.Key = KeyOf(string.Concat(column.HeaderParts));

                if (!CheckColumns(columns, pageNumber, result))
                {
                    index = next;
                    continue;
                }

                ParsedTranscriptRow? row = null;
                var cells = new Dictionary<string, List<string>>();
                while (next < lines.Count)
                {
                    var current = lines[next];
                    if (CrossesColumns(current, columns)) break;
                    if (StartsRow(current, columns))
                    {
                        AddRow(row, cells, result);
                        row = new ParsedTranscriptRow
                        {
                            Page = pageNumber,
                            IsInCurriculum = inCurriculum,
                            RowNumber = int.Parse(current.Phrases[0].Text)
                        };
                        cells = new Dictionary<string, List<string>>();
                    }
                    else if (row == null)
                    {
                        break;
                    }

                    foreach (var phrase in current.Phrases)
                    {
                        var key = columns[ColumnOf(phrase.Left, columns)].Key;
                        if (key == null) continue;
                        if (!cells.TryGetValue(key, out var parts))
                        {
                            parts = new List<string>();
                            cells[key] = parts;
                        }
                        parts.Add(phrase.Text);
                    }
                    next++;
                }
                AddRow(row, cells, result);
                index = next;
            }
            return foundTable;
        }

        private static bool IsTableHeader(Line line)
        {
            if (line.Phrases.Count < 3) return false;
            var first = Squash(line.Phrases[0].Text);
            return (first == "no" || first == "stt") && line.Phrases.Any(p => Squash(p.Text).StartsWith("semester"));
        }

        private static bool StartsRow(Line line, List<Column> columns) =>
            line.Phrases.Count > 0
            && ColumnOf(line.Phrases[0].Left, columns) == 0
            && WholeNumber.IsMatch(line.Phrases[0].Text);

        // Index of the column a text starting at x belongs to, -1 when it starts left of the table
        private static int ColumnOf(double x, List<Column> columns)
        {
            var found = -1;
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].Left <= x + ColumnTolerance) found = i;
            }
            return found;
        }

        // A text that starts outside the table or runs into the next column is not a table cell (title, footer, note)
        private static bool CrossesColumns(Line line, List<Column> columns)
        {
            foreach (var phrase in line.Phrases)
            {
                var column = ColumnOf(phrase.Left, columns);
                if (column < 0) return true;
                if (column + 1 < columns.Count && phrase.Right > columns[column + 1].Left - 1) return true;
            }
            return false;
        }

        private static string? KeyOf(string headerText)
        {
            var key = Squash(headerText);
            if (key.Contains("attendance")) return "attendance";
            return key switch
            {
                "no" or "stt" => "no",
                "term" => "term",
                "semester" => "semester",
                "subjectcode" => "subjectCode",
                "subjectname" => "subjectName",
                "grade" => "grade",
                "status" => "status",
                _ => null
            };
        }

        // Helper: the table must have Semester, Subject code and exactly one value column of the same type as the file's other tables
        private static bool CheckColumns(List<Column> columns, int pageNumber, ParsedTranscript result)
        {
            var keys = columns.Where(c => c.Key != null).Select(c => c.Key!).ToHashSet();
            var hasGrade = keys.Contains("grade");
            var hasAttendance = keys.Contains("attendance");
            if (hasGrade && hasAttendance)
            {
                AddError(result, "The table has both a Grade and an Attendance column; use one file per type.");
                return false;
            }
            if (!hasGrade && !hasAttendance)
            {
                AddError(result, $"The table on page {pageNumber} has neither a Grade nor an Attendance column.");
                return false;
            }

            var missing = new List<string>();
            if (!keys.Contains("semester")) missing.Add("Semester");
            if (!keys.Contains("subjectCode")) missing.Add("Subject code");
            if (missing.Count > 0)
            {
                AddError(result, $"The table on page {pageNumber} has no {string.Join(" or ", missing)} column.");
                return false;
            }

            var type = hasGrade ? "grade" : "attendance";
            if (result.ImportType.Length == 0)
            {
                result.ImportType = type;
            }
            else if (result.ImportType != type)
            {
                AddError(result, "The file mixes grade and attendance tables; use one file per type.");
                return false;
            }
            return true;
        }

        private static void AddRow(ParsedTranscriptRow? row, Dictionary<string, List<string>> cells, ParsedTranscript result)
        {
            if (row == null) return;
            // Codes, numbers and semesters that wrap are one token cut in two ("Summer202" / "6"); names keep their spaces
            string Joined(string key, bool spaced) =>
                cells.TryGetValue(key, out var parts) ? string.Join(spaced ? " " : string.Empty, parts).Trim() : string.Empty;

            row.CurriculumTerm = Joined("term", false);
            row.Semester = Joined("semester", false);
            row.SubjectCode = Joined("subjectCode", false);
            row.SubjectName = Joined("subjectName", true).Normalize(NormalizationForm.FormC);
            row.Value = cells.ContainsKey("grade") ? Joined("grade", false) : Joined("attendance", false);
            row.Status = Joined("status", true);
            result.Rows.Add(row);
        }

        private static void AddError(ParsedTranscript result, string message)
        {
            if (!result.Errors.Contains(message)) result.Errors.Add(message);
        }

        // Lower-case letters, digits and %, so "Ter" + "m", "Subject code" and "No." compare as "term", "subjectcode", "no"
        private static string Squash(string text) =>
            new string(text.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '%').ToArray());
    }
}
