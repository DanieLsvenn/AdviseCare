using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
using AdviseCare.Application.Contracts.Responses.CaseChangeRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseChangeRequestService
    {
        Task<IEnumerable<GetAllCaseChangeRequestResponse>> GetAll(Guid CallerId);
        Task<IEnumerable<GetAllCaseChangeRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize);
        Task<GetCaseChangeRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseChangeRequestId);
        Task<List<SearchCaseChangeRequestResponse>> SearchAsync(Guid CallerId, SearchCaseChangeRequestRequest request);
        Task<CreateCaseChangeRequestResponse> CreateAsync(Guid CreatorId, CreateCaseChangeRequestRequest request);
        Task<ApproveCaseChangeRequestResponse> ApproveAsync(Guid ProgrammeHeadId, Guid CaseChangeRequestId, ApproveCaseChangeRequestRequest request);
        Task<RejectCaseChangeRequestResponse> RejectAsync(Guid ProgrammeHeadId, Guid CaseChangeRequestId, RejectCaseChangeRequestRequest request);
    }
}
