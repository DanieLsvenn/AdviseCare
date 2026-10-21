using AdviseCare.Application.Contracts.Requests.AcademicTerm;
using AdviseCare.Application.Contracts.Responses.AcademicTerm;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAcademicTermService
    {
        Task<IEnumerable<GetAllAcademicTermResponse>> GetAll();
        Task<IEnumerable<GetAllAcademicTermWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetAcademicTermByIdResponse> GetByIdAsync(Guid TermId);
        Task<List<SearchAcademicTermResponse>> SearchAsync(SearchAcademicTermRequest request);
        Task<CreateAcademicTermResponse> CreateAsync(CreateAcademicTermRequest request);
        Task<UpdateAcademicTermResponse> UpdateAsync(Guid TermId, UpdateAcademicTermRequest request);
    }
}
