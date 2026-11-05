using AdviseCare.Application.Contracts.Requests.CaseAdvisorUpdate;
using AdviseCare.Application.Contracts.Responses.CaseAdvisorUpdate;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseAdvisorUpdateService
    {
        Task<List<CaseAdvisorUpdateResponse>> GetForCaseAsync(Guid CallerId, Guid CaseId);
        Task<CaseAdvisorUpdateResponse> CreateAsync(Guid AuthorId, Guid CaseId, CreateCaseAdvisorUpdateRequest request);
    }
}
