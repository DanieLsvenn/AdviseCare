using AdviseCare.Application.Contracts.Requests.SupportGrant;
using AdviseCare.Application.Contracts.Responses.SupportGrant;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISupportGrantService
    {
        Task<SupportGrantResponse> CreateAsync(Guid officerId, CreateSupportGrantRequest request);
        Task<List<SupportGrantResponse>> SearchAsync(Guid officerId, SearchSupportGrantRequest request);
    }
}
