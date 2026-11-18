namespace AdviseCare.Application.Contracts.Responses.AcademicResult
{
    // ---------- Import (System Administrator) ----------

    public class TranscriptImportBatchResponse
    {
        public Guid ImportBatchId { get; set; } = Guid.Empty;
        public Guid ImportedById { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        // previewed / committed / cancelled
        public string Status { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int ValidFiles { get; set; }
        public int InvalidFiles { get; set; }
        public int CommittedFiles { get; set; }
        public List<TranscriptImportFileResponse> Files { get; set; } = new();
        // Subjects and terms the valid files use that AdviseCare does not have yet; created at commit
        public List<TranscriptNewSubjectResponse> NewSubjects { get; set; } = new();
        public List<string> NewTerms { get; set; } = new();
    }

    public class TranscriptImportFileResponse
    {
        public Guid ImportFileId { get; set; } = Guid.Empty;
        public string FileName { get; set; } = string.Empty;
        // grade / attendance; empty when the file could not be read
        public string ImportType { get; set; } = string.Empty;
        public Guid? StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string FullNameInFile { get; set; } = string.Empty;
        public string CurriculumCode { get; set; } = string.Empty;
        public int RowCount { get; set; }
        // Results the commit will create / update (counted at preview), and rows left out (no semester yet)
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int SkippedRowCount { get; set; }
        // valid / invalid / committed
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class TranscriptNewSubjectResponse
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int FileCount { get; set; }
        // Faculty the subject will be created under: the faculty of a student who has it; null when none of them has a faculty
        // (then the faculty chosen at commit is used)
        public Guid? FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
    }

    public class TranscriptImportBatchSummaryResponse
    {
        public Guid ImportBatchId { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int ValidFiles { get; set; }
        public int InvalidFiles { get; set; }
        public int CommittedFiles { get; set; }
    }

    public class CommitTranscriptImportResponse
    {
        public Guid ImportBatchId { get; set; } = Guid.Empty;
        public int CommittedFiles { get; set; }
        public int SkippedFiles { get; set; }
        public int CreatedResults { get; set; }
        public int UpdatedResults { get; set; }
        public List<string> CreatedSubjects { get; set; } = new();
        public List<string> CreatedTerms { get; set; } = new();
    }

    // ---------- Reading (student, advisors) ----------

    public class AcademicResultsResponse
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CurriculumCode { get; set; } = string.Empty;
        // For staff: whether the student shared GPA / Attendance rate with them; a value not shared comes back null.
        // Always true for the student's own view.
        public bool IsGradeShared { get; set; }
        public bool IsAttendanceShared { get; set; }
        // Thresholds the web app uses to mark a subject: attendance below MinAttendanceRate, grade below PassingGrade
        public decimal MinAttendanceRate { get; set; }
        public decimal PassingGrade { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public AcademicResultSummaryResponse Summary { get; set; } = new();
        // Newest term first
        public List<AcademicTermResultsResponse> Terms { get; set; } = new();
    }

    public class AcademicTermResultsResponse
    {
        public Guid TermId { get; set; } = Guid.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public AcademicResultSummaryResponse Summary { get; set; } = new();
        public List<SubjectResultResponse> Results { get; set; } = new();
    }

    public class SubjectResultResponse
    {
        public Guid StudentSubjectResultId { get; set; } = Guid.Empty;
        public Guid SubjectId { get; set; } = Guid.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int? CurriculumTerm { get; set; }
        public bool IsInCurriculum { get; set; }
        public decimal? Grade { get; set; }
        public decimal? AttendanceRate { get; set; }
        // 100 - AttendanceRate
        public decimal? AbsenceRate { get; set; }
        // passed / notPassed / attendanceFail / exempt / studying / notStarted; empty when unknown or not shared
        public string Status { get; set; } = string.Empty;
        public DateTime? GradeUpdatedAt { get; set; }
        public DateTime? AttendanceUpdatedAt { get; set; }
    }

    public class AcademicResultSummaryResponse
    {
        public int SubjectCount { get; set; }
        public int PassedCount { get; set; }
        // Subjects with a grade, their plain (not credit-weighted) average, and how many are below the passing grade
        public int GradedCount { get; set; }
        public decimal? AverageGrade { get; set; }
        public int LowGradeCount { get; set; }
        // Subjects with an attendance rate, their average, and how many are below the minimum attendance
        public int AttendanceCount { get; set; }
        public decimal? AverageAttendanceRate { get; set; }
        public int LowAttendanceCount { get; set; }
    }
}
