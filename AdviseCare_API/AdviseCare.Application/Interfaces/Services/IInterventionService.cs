using AdviseCare.Application.Contracts.Requests.Intervention;
using AdviseCare.Application.Contracts.Responses.Intervention;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IInterventionService
    {
        Task<IEnumerable<GetAllInterventionResponse>> GetAll();
        Task<IEnumerable<GetAllInterventionWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetInterventionByIdResponse> GetByIdAsync(Guid InterventionId);
        Task<List<SearchInterventionResponse>> SearchAsync(SearchInterventionRequest request);
        Task<CreateInterventionResponse> CreateAsync(Guid CreatorId, CreateInterventionRequest request);
        Task<UpdateInterventionResponse> UpdateAsync(Guid OfficerId, Guid InterventionId, UpdateInterventionRequest request);
        Task<CloseInterventionResponse> CloseAsync(Guid OfficerId, Guid InterventionId, CloseInterventionRequest request);
    }
}
