using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Contracts.Responses.CorrectionRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICorrectionRequestService
    {
        Task<IEnumerable<GetAllCorrectionRequestResponse>> GetAll(Guid AdvisorId);
        Task<IEnumerable<GetAllCorrectionRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid AdvisorId, int page, int pageSize);
        Task<GetCorrectionRequestByIdResponse> GetByIdAsync(Guid AdvisorId, Guid CorrectionRequestId);
        Task<List<SearchCorrectionRequestResponse>> SearchAsync(Guid AdvisorId, SearchCorrectionRequestRequest request);
        Task<CreateCorrectionRequestResponse> CreateAsync(Guid CreatorId, CreateCorrectionRequestRequest request);
        Task<ApproveCorrectionRequestResponse> ApproveAsync(Guid AdvisorId, Guid CorrectionRequestId, ReviewCorrectionRequestRequest request);
        Task<RejectCorrectionRequestResponse> RejectAsync(Guid AdvisorId, Guid CorrectionRequestId, ReviewCorrectionRequestRequest request);
        Task<bool> WithdrawAsync(Guid StudentId, Guid CorrectionRequestId);
        Task<List<GetMyCorrectionRequestResponse>> GetMyAllAsync(Guid StudentId);
        Task<GetMyCorrectionRequestResponse> GetMyByIdAsync(Guid StudentId, Guid CorrectionRequestId);
    }
}
