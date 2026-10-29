using AdviseCare.Application.Contracts.Requests.Escalation;
using AdviseCare.Application.Contracts.Responses.Escalation;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IEscalationService
    {
        Task<IEnumerable<GetAllEscalationResponse>> GetAll(Guid AccountId);
        Task<IEnumerable<GetAllEscalationWithPaginationResponse>> GetAllWithPaginationAsync(Guid AccountId, int page, int pageSize);
        Task<GetEscalationByIdResponse> GetByIdAsync(Guid AccountId, Guid EscalationId);
        Task<List<SearchEscalationResponse>> SearchAsync(Guid AccountId, SearchEscalationRequest request);
        Task<CreateEscalationResponse> CreateAsync(Guid CreatorId, CreateEscalationRequest request);
        Task<RespondEscalationResponse> RespondAsync(Guid ProgrammeHeadId, Guid EscalationId, RespondEscalationRequest request);
    }
}
