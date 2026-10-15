using AdviseCare.Application.Contracts.Requests.Auth;
using AdviseCare.Application.Contracts.Responses.Auth;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<GetSelfProfileResponse> GetSelfProfileAsync(Guid accountId);
        Task<LogoutResponse> LogoutAsync(Guid accountId, Guid sessionId);
    }
}
