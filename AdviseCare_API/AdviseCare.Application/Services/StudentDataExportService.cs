using AdviseCare.Application.Contracts.Requests.AccessLog;
using AdviseCare.Application.Contracts.Responses.StudentIndicator;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Text.Json;

namespace AdviseCare.Application.Services;

public sealed class StudentDataExportService(
    IGenericRepository<Studentprofile> studentProfileRepository,
    IGenericRepository<Case> caseRepository,
    IStudentIndicatorProfileService profileService,
    IAccessLogService accessLogService,
    IActivityLogService activityLogService) : IStudentDataExportService
{
    private readonly IGenericRepository<Studentprofile> _studentProfileRepository = studentProfileRepository;
    private readonly IGenericRepository<Case> _caseRepository = caseRepository;
    private readonly IStudentIndicatorProfileService _profileService = profileService;
    private readonly IAccessLogService _accessLogService = accessLogService;
    private readonly IActivityLogService _activityLogService = activityLogService;

    public async Task<StudentDataExportFile> ExportOwnDataAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var studentProfile = await _studentProfileRepository.FirstOrDefaultAsync(
            profile => profile.StudentId == studentId,
            profile => profile.Student)
            ?? throw new InvalidDataException("The student's profile has not been provisioned.");

        var profile = await _profileService.GetOwnProfileAsync(studentId, null, cancellationToken);
        var cases = await _caseRepository.SearchAsyncInclude(
            record => record.StudentId == studentId,
            record => record.SupportCategory,
            record => record.Subject);
        var accessLogs = await _accessLogService.GetMyAccessLogsAsync(studentId, new SearchMyAccessLogRequest());
        var document = new StudentDataExportDocument(
            DateTimeOffset.UtcNow,
            new StudentDataExportIdentity(
                studentProfile.StudentId,
                studentProfile.StudentCode,
                studentProfile.Student.FullName,
                studentProfile.Student.Email,
                studentProfile.Student.UserName,
                studentProfile.Student.FacultyId,
                studentProfile.AcademicStatus),
            profile,
            cases
                .OrderByDescending(record => record.OpenedAt)
                .Select(record => new StudentCaseExportResponse(
                    record.CaseId,
                    record.RequestReason,
                    record.OpenedAt,
                    record.ClosedAt,
                    record.CurrentStage,
                    record.Priority,
                    record.Status,
                    record.SupportCategory.CategoryName,
                    record.Subject?.SubjectCode,
                    record.Subject?.SubjectName))
                .ToList(),
            accessLogs);
        var content = JsonSerializer.SerializeToUtf8Bytes(document, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
        var fileName = $"advise-care-data-export-{DateTimeOffset.UtcNow:yyyyMMdd}.json";

        await _activityLogService.LogCurrentAccountAsync(
            "dataExported",
            $"Exported own profile, {profile.Indicators.Count} indicators, {cases.Count} cases, and {accessLogs.Count} access log entries as JSON.");

        return new StudentDataExportFile(content, "application/json", fileName);
    }
}
