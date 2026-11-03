using AdviseCare.Application.Contracts.Requests.Case;
using AdviseCare.Application.Contracts.Responses.Case;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseService
    {
        Task<IEnumerable<GetAllCaseResponse>> GetAll();
        Task<IEnumerable<GetAllCaseWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetCaseByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseId);
        Task<List<SearchCaseResponse>> SearchAsync(SearchCaseRequest request);
        Task<CreateCaseResponse> CreateAsync(Guid CreatorId, CreateCaseRequest request);
        Task<List<GetMyCaseResponse>> GetMyAllAsync(Guid StudentId, SearchMyCaseRequest request);
        Task<List<GetMatchingQueueCaseResponse>> GetMatchingQueueAsync();
        Task<List<GetCaseSuggestedAdvisorResponse>> GetSuggestedAdvisorsAsync(Guid CaseId);
        Task<AssignCaseResponse> AssignAsync(Guid ProgrammeHeadId, Guid CaseId, AssignCaseRequest request);
        Task<ReassignCaseResponse> ReassignAsync(Guid ProgrammeHeadId, Guid CaseId, ReassignCaseRequest request);
        Task<List<GetAssignedCaseResponse>> GetAssignedAsync(Guid AdvisorId, SearchAssignedCaseRequest request);
        Task<IEnumerable<GetAssignedCaseWithPaginationResponse>> GetAssignedWithPaginationAsync(Guid AdvisorId, SearchAssignedCaseRequest request, int page, int pageSize);
        Task<int> GetAssignedUnreadCountAsync(Guid AdvisorId);
        Task<ChangeCaseStageResponse> ChangeStageAsync(Guid AdvisorId, Guid CaseId, ChangeCaseStageRequest request);
        Task<ReopenCaseResponse> ReopenAsync(Guid AdvisorId, Guid CaseId, ReopenCaseRequest request);
        Task<ChangeCasePriorityResponse> ChangePriorityAsync(Guid CallerId, Guid CaseId, ChangeCasePriorityRequest request);
        Task<List<SearchCaseStageHistoryResponse>> SearchCaseStageHistoryAsync(Guid CallerId, SearchCaseStageHistoryRequest request);
    }
}
