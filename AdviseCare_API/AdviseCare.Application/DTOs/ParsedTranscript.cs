namespace AdviseCare.Application.DTOs
{
    // What the transcript PDF reader found in one file, as text; the academic result service checks it against the database
    public class ParsedTranscript
    {
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CurriculumCode { get; set; } = string.Empty;
        // "grade" or "attendance", told by the header of the value column; empty when it could not be told
        public string ImportType { get; set; } = string.Empty;
        public List<ParsedTranscriptRow> Rows { get; set; } = new();
        // Problems with the file as a whole (not a PDF, no table, missing columns, both value columns...)
        public List<string> Errors { get; set; } = new();
    }

    // One subject row of a transcript table, every cell as printed (wrapped lines joined)
    public class ParsedTranscriptRow
    {
        // "No." column
        public int RowNumber { get; set; }
        public int Page { get; set; }
        // False for the rows of "SUBJECTS OUTSIDE THE CURRICULUM"
        public bool IsInCurriculum { get; set; } = true;
        // "Term" column (curriculum semester 0-9); empty outside the curriculum
        public string CurriculumTerm { get; set; } = string.Empty;
        // "Semester" column, e.g. Fall2024
        public string Semester { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        // "Grade" (0-10) or "Attendance" (0-100 %) column; "-" when there is none
        public string Value { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
