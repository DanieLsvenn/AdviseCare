using AdviseCare.Application.Contracts.Responses.StudentIndicator;

namespace AdviseCare.Application.Interfaces.Services;

public interface IStudentIndicatorProfileService
{
    Task<StudentIndicatorProfileResponse> GetOwnProfileAsync(
        Guid studentId,
        Guid? termId,
        CancellationToken cancellationToken);

    Task<StudentIndicatorProfileResponse> GetProfileAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        Guid? termId,
        CancellationToken cancellationToken);

    Task<StudentIndicatorProfileResponse> GetAsOfAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        DateOnly date,
        Guid? termId,
        CancellationToken cancellationToken);

    Task<StudentIndicatorComparisonResponse> CompareAsync(
        Guid accessorId,
        Guid studentId,
        Guid caseId,
        DateOnly dateA,
        DateOnly dateB,
        Guid? termId,
        CancellationToken cancellationToken);
}
