using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Contracts.Responses.Case;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseService
    {
        // D2 / D4.2
        Task<CaseResponse> CreateAsync(Guid callerId, CreateCaseRequest request);
        Task<List<CaseResponse>> GetMyCasesAsync(Guid studentId, SearchMyCasesRequest request);
        Task<List<CaseResponse>> SearchAsync(SearchCasesRequest request);
        Task<CaseResponse> GetByIdAsync(Guid callerId, Guid caseId);
        Task<List<CaseStageHistoryResponse>> GetStageHistoryAsync(Guid callerId, Guid caseId);

        // D3
        Task<List<CaseResponse>> GetMatchingQueueAsync();
        Task<List<SuggestedAdvisorResponse>> GetSuggestedAdvisorsAsync(Guid caseId);
        Task<CaseResponse> AssignAsync(Guid programmeHeadId, Guid caseId, AssignCaseRequest request);
        Task<CaseResponse> ReassignAsync(Guid programmeHeadId, Guid caseId, ReassignCaseRequest request);

        // D4
        Task<AssignedCasesResponse> GetAssignedAsync(Guid advisorId, SearchAssignedCasesRequest request);
        Task<CaseResponse> ChangeStageAsync(Guid advisorId, Guid caseId, ChangeCaseStageRequest request);
        Task<CaseResponse> ReopenAsync(Guid advisorId, Guid caseId, ReopenCaseRequest request);
        Task<CaseResponse> ChangePriorityAsync(Guid callerId, Guid caseId, ChangeCasePriorityRequest request);
    }
}
