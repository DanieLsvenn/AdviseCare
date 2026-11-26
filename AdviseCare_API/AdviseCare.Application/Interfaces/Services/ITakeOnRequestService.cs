using AdviseCare.Application.Contracts.Requests.TakeOnRequest;
using AdviseCare.Application.Contracts.Responses.TakeOnRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ITakeOnRequestService
    {
        Task<IEnumerable<GetAllTakeOnRequestResponse>> GetAll();
        Task<IEnumerable<GetAllTakeOnRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetTakeOnRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid TakeOnRequestId);
        Task<List<SearchTakeOnRequestResponse>> SearchAsync(SearchTakeOnRequestRequest request);
        Task<CreateTakeOnRequestResponse> CreateAsync(Guid CreatorId, CreateTakeOnRequestRequest request);
        Task<UpdateTakeOnRequestResponse> UpdateAsync(Guid CallerId, Guid TakeOnRequestId, UpdateTakeOnRequestRequest request);
        Task<List<GetMyTakeOnRequestResponse>> GetMyAllAsync(Guid AdvisorId, SearchMyTakeOnRequestRequest request);
        Task<ApproveTakeOnRequestResponse> ApproveAsync(Guid ProgrammeHeadId, Guid TakeOnRequestId);
        Task<DeclineTakeOnRequestResponse> DeclineAsync(Guid ProgrammeHeadId, Guid TakeOnRequestId, DeclineTakeOnRequestRequest request);
        Task<WithdrawTakeOnRequestResponse> WithdrawAsync(Guid AdvisorId, Guid TakeOnRequestId);
    }
}
