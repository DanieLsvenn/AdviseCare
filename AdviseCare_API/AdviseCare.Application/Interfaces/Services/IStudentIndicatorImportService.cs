using AdviseCare.Application.Contracts.Requests.StudentIndicator;
using AdviseCare.Application.Contracts.Responses.StudentIndicator;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IStudentIndicatorImportService
    {
        Task<PreviewStudentIndicatorImportResponse> PreviewAsync(PreviewStudentIndicatorImportRequest request);
        Task<ValidateStudentIndicatorImportResponse> ValidateAsync(ValidateStudentIndicatorImportRequest request);
    }
}
