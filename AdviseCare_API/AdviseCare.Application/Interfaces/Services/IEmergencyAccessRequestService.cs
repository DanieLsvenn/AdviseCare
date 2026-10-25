using AdviseCare.Application.Contracts.Requests.EmergencyAccessRequest;
using AdviseCare.Application.Contracts.Responses.EmergencyAccessRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IEmergencyAccessRequestService
    {
        Task<IEnumerable<GetAllEmergencyAccessRequestResponse>> GetAll();
        Task<IEnumerable<GetAllEmergencyAccessRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetEmergencyAccessRequestByIdResponse> GetByIdAsync(Guid EmergencyAccessRequestId);
        Task<List<SearchEmergencyAccessRequestResponse>> SearchAsync(SearchEmergencyAccessRequestRequest request);
        Task<CreateEmergencyAccessRequestResponse> CreateAsync(Guid CreatorId, CreateEmergencyAccessRequestRequest request);
        Task<UpdateEmergencyAccessRequestResponse> UpdateAsync(Guid RequesterId, Guid EmergencyAccessRequestId, UpdateEmergencyAccessRequestRequest request);
        Task<bool> ApproveAsync(Guid StewardId, Guid EmergencyAccessRequestId, ApproveEmergencyAccessRequestRequest request);
        Task<bool> RejectAsync(Guid StewardId, Guid EmergencyAccessRequestId, RejectEmergencyAccessRequestRequest request);
        Task<List<GetMyEmergencyAccessRequestResponse>> GetMyRequestsAsync(Guid RequesterId);
    }
}
