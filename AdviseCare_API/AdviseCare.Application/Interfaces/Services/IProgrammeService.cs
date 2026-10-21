using AdviseCare.Application.Contracts.Requests.Programme;
using AdviseCare.Application.Contracts.Responses.Programme;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IProgrammeService
    {
        Task<IEnumerable<GetAllProgrammeResponse>> GetAll();
        Task<IEnumerable<GetAllProgrammeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetProgrammeByIdResponse> GetByIdAsync(Guid ProgrammeId);
        Task<List<SearchProgrammeResponse>> SearchAsync(SearchProgrammeRequest request);
        Task<CreateProgrammeResponse> CreateAsync(CreateProgrammeRequest request);
        Task<UpdateProgrammeResponse> UpdateAsync(Guid ProgrammeId, UpdateProgrammeRequest request);
    }
}
