using AdviseCare.Application.Contracts.Requests.Intervention;
using AdviseCare.Application.Contracts.Responses.Intervention;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IInterventionService
    {
        Task<InterventionResponse> CreateAsync(Guid officerId, CreateInterventionRequest request);
        Task<List<InterventionResponse>> SearchAsync(SearchInterventionRequest request);
        Task<InterventionResponse> UpdateAsync(Guid officerId, Guid interventionId, UpdateInterventionRequest request);
        Task<InterventionResponse> RecordOutcomeAsync(Guid officerId, Guid interventionId, RecordInterventionOutcomeRequest request);
        Task<InterventionResponse> CloseAsync(Guid officerId, Guid interventionId, CloseInterventionRequest request);
    }
}
