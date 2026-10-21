using AdviseCare.Application.Contracts.Requests.Faculty;
using AdviseCare.Application.Contracts.Responses.Faculty;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IFacultyService
    {
        Task<IEnumerable<GetAllFacultyResponse>> GetAll();
        Task<IEnumerable<GetAllFacultyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetFacultyByIdResponse> GetByIdAsync(Guid FacultyId);
        Task<List<SearchFacultyResponse>> SearchAsync(SearchFacultyRequest request);
        Task<CreateFacultyResponse> CreateAsync(CreateFacultyRequest request);
        Task<UpdateFacultyResponse> UpdateAsync(Guid FacultyId, UpdateFacultyRequest request);
    }
}
