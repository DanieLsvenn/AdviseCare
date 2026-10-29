using AdviseCare.Application.Contracts.Requests.StudentProfile;
using AdviseCare.Application.Contracts.Responses.StudentProfile;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IStudentProfileService
    {
        Task<IEnumerable<GetAllStudentProfileResponse>> GetAll();
        Task<IEnumerable<GetAllStudentProfileWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetStudentProfileByIdResponse> GetByIdAsync(Guid StudentId);
        Task<List<SearchStudentProfileResponse>> SearchAsync(SearchStudentProfileRequest request);
        Task<UpdateStudentProfileResponse> UpdateAsync(Guid StudentId, UpdateStudentProfileRequest request);
        Task<GetMyStudentProfileResponse> GetMyProfileAsync(Guid StudentId);
    }
}
