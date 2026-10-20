using AdviseCare.Application.Contracts.Requests.Subject;
using AdviseCare.Application.Contracts.Responses.Subject;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<GetAllSubjectResponse>> GetAll();
        Task<IEnumerable<GetAllSubjectWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetSubjectByIdResponse> GetByIdAsync(Guid SubjectId);
        Task<List<SearchSubjectResponse>> SearchAsync(SearchSubjectRequest request);
        Task<CreateSubjectResponse> CreateAsync(CreateSubjectRequest request);
        Task<UpdateSubjectResponse> UpdateAsync(Guid SubjectId, UpdateSubjectRequest request);
        Task<IEnumerable<GetAllAccountSubjectResponse>> GetAllAccountSubjectAsync();
        Task<IEnumerable<GetAllAccountSubjectWithPaginationResponse>> GetAllAccountSubjectWithPaginationAsync(int page, int pageSize);
        Task<GetAccountSubjectByIdResponse> GetAccountSubjectByIdAsync(Guid AccountId, Guid SubjectId);
        Task<List<SearchAccountSubjectResponse>> SearchAccountSubjectAsync(SearchAccountSubjectRequest request);
        Task<CreateAccountSubjectResponse> CreateAccountSubjectAsync(CreateAccountSubjectRequest request);
        Task<bool> DeleteAccountSubjectAsync(Guid AccountId, Guid SubjectId);
    }
}
