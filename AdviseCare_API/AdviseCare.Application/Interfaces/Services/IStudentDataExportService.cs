using AdviseCare.Application.Contracts.Responses.StudentIndicator;

namespace AdviseCare.Application.Interfaces.Services;

public interface IStudentDataExportService
{
    Task<StudentDataExportFile> ExportOwnDataAsync(Guid studentId, CancellationToken cancellationToken);
}
