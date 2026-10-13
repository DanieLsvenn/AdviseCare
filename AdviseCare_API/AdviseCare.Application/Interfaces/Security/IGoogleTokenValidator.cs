using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Security
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfo> ValidateAsync(string idToken);
    }
}
