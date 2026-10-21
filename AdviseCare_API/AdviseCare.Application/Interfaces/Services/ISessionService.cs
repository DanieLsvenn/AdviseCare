using AdviseCare.Application.Contracts.Requests.Session;
using AdviseCare.Application.Contracts.Responses.Session;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISessionService
    {
        Task<IEnumerable<GetAllSessionResponse>> GetAll();
        Task<IEnumerable<GetAllSessionWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetSessionByIdResponse> GetByIdAsync(Guid SessionId);
        Task<List<SearchSessionResponse>> SearchAsync(SearchSessionRequest request);
        Task<CreateSessionResponse> CreateAsync(CreateSessionRequest request);
        Task<UpdateSessionResponse> UpdateAsync(Guid SessionId, UpdateSessionRequest request);
        Task<List<GetMySessionResponse>> GetMySessionsAsync(Guid AccountId, Guid CurrentSessionId);
        Task<LogoutResponse> LogoutAsync(Guid AccountId, Guid CurrentSessionId, Guid SessionId);
        Task<LogoutResponse> LogoutOtherSessionsAsync(Guid AccountId, Guid CurrentSessionId);
    }
}
