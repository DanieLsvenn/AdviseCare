using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Contracts.Responses.Report;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<List<GetCaseReportResponse>> GetCaseReportAsync(GetCaseReportRequest request);
        Task<ExportCaseReportResponse> ExportCaseReportAsync(Guid ProgrammeHeadId, ExportCaseReportRequest request);
        Task<List<GetCaseloadClosureReportResponse>> GetCaseloadClosureReportAsync(GetCaseloadClosureReportRequest request);
        Task<ExportCaseloadClosureReportResponse> ExportCaseloadClosureReportAsync(Guid ProgrammeHeadId, ExportCaseloadClosureReportRequest request);
    }
}
