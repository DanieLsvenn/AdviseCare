using AdviseCare.Application.Contracts.Responses.AccessLog;

namespace AdviseCare.Application.Contracts.Responses.StudentIndicator;

public sealed record StudentDataExportFile(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record StudentDataExportDocument(
    DateTimeOffset ExportedAtUtc,
    StudentDataExportIdentity Student,
    StudentIndicatorProfileResponse Profile,
    IReadOnlyList<StudentCaseExportResponse> Cases,
    IReadOnlyList<GetMyAccessLogResponse> AccessLogs);

public sealed record StudentDataExportIdentity(
    Guid StudentId,
    string StudentCode,
    string FullName,
    string Email,
    string UserName,
    Guid? ProgrammeId,
    string AcademicStatus);

public sealed record StudentCaseExportResponse(
    Guid CaseId,
    string RequestReason,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string CurrentStage,
    string Priority,
    string Status,
    string SupportCategory,
    string? SubjectCode,
    string? SubjectName);
