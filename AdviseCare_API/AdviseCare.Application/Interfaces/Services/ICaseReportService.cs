using AdviseCare.Application.Contracts.Requests.Report;
using AdviseCare.Application.Contracts.Responses.Report;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseReportService
    {
        Task<List<CaseReportRowResponse>> GetCasesAsync(CaseReportRequest request);
        Task<List<CaseloadClosureRowResponse>> GetCaseloadClosureAsync(CaseloadClosureRequest request);
        Task<ReportFileResponse> ExportCasesAsync(Guid programmeHeadId, CaseReportExportRequest request);
        Task<ReportFileResponse> ExportCaseloadClosureAsync(Guid programmeHeadId, CaseloadClosureExportRequest request);
    }
}
