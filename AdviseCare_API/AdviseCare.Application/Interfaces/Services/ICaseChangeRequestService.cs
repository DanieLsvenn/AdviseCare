using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
using AdviseCare.Application.Contracts.Responses.CaseChangeRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseChangeRequestService
    {
        Task<CaseChangeRequestResponse> CreateAsync(Guid requestedBy, CreateCaseChangeRequestRequest request);
        Task<List<CaseChangeRequestResponse>> SearchAsync(Guid callerId, SearchCaseChangeRequestRequest request);
        Task<CaseChangeRequestResponse> GetByIdAsync(Guid callerId, Guid caseChangeRequestId);
        Task<CaseChangeRequestResponse> ApproveAsync(Guid programmeHeadId, Guid caseChangeRequestId, ApproveCaseChangeRequestRequest request);
        Task<CaseChangeRequestResponse> RejectAsync(Guid programmeHeadId, Guid caseChangeRequestId, RejectCaseChangeRequestRequest request);
    }
}
