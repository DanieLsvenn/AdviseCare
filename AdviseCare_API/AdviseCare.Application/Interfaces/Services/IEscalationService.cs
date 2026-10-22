using AdviseCare.Application.Contracts.Requests.Escalation;
using AdviseCare.Application.Contracts.Responses.Escalation;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IEscalationService
    {
        Task<EscalationResponse> CreateAsync(Guid escalatedBy, CreateEscalationRequest request);
        Task<List<EscalationResponse>> SearchAsync(Guid accountId, bool isProgrammeHead, SearchEscalationRequest request);
        Task<EscalationResponse> GetByIdAsync(Guid accountId, Guid escalationId);
        Task<EscalationResponse> RespondAsync(Guid programmeHeadId, Guid escalationId, RespondToEscalationRequest request);
    }
}
