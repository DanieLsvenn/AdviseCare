using AdviseCare.Application.Contracts.Requests.Account;
using AdviseCare.Application.Contracts.Responses.Account;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<IEnumerable<GetAllAccountResponse>> GetAll();
        Task<IEnumerable<GetAllAccountWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetAccountByIdResponse> GetByIdAsync(Guid AccountId);
        Task<List<SearchAccountResponse>> SearchAsync(SearchAccountRequest request);
        Task<CreateAccountResponse> CreateAsync(CreateAccountRequest request);
        Task<UpdateAccountResponse> UpdateAsync(Guid AccountId, UpdateAccountRequest request);
        Task<ImportAccountResponse> ImportAsync(ImportAccountRequest request);
        Task<SendLinkingEmailResponse> SendLinkingEmailsAsync(SendLinkingEmailRequest request);
        Task<bool> ActivateAsync(Guid AccountId);
        Task<bool> DeactivateAsync(Guid ActorId, Guid AccountId);
        Task<bool> BanAsync(Guid ActorId, Guid AccountId);
        Task<bool> UnbanAsync(Guid AccountId);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
        Task<LoginResponse> LinkGoogleAccountAsync(LinkGoogleAccountRequest request);
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ChangePasswordResponse> ChangePasswordAsync(Guid AccountId, Guid CurrentSessionId, ChangePasswordRequest request);
        Task<GetMyProfileResponse> GetMyProfileAsync(Guid AccountId);
        Task<UpdateMyProfileResponse> UpdateMyProfileAsync(Guid AccountId, UpdateMyProfileRequest request);
        Task<UploadAvatarResponse> UploadAvatarAsync(Guid AccountId, UploadAvatarRequest request);
        Task<IEnumerable<GetAllAccountRoleResponse>> GetAllAccountRoleAsync();
        Task<IEnumerable<GetAllAccountRoleWithPaginationResponse>> GetAllAccountRoleWithPaginationAsync(int page, int pageSize);
        Task<GetAccountRoleByIdResponse> GetAccountRoleByIdAsync(Guid AccountId, Guid RoleId);
        Task<List<SearchAccountRoleResponse>> SearchAccountRoleAsync(SearchAccountRoleRequest request);
        Task<CreateAccountRoleResponse> CreateAccountRoleAsync(CreateAccountRoleRequest request);
        Task<bool> DeleteAccountRoleAsync(Guid AccountId, Guid RoleId);
    }
}
