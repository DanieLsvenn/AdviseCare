using AdviseCare.Application.Contracts.Requests.CaseNote;
using AdviseCare.Application.Contracts.Responses.CaseNote;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseNoteService
    {
        Task<IEnumerable<GetAllCaseNoteResponse>> GetAll(Guid AuthorId);
        Task<IEnumerable<GetAllCaseNoteWithPaginationResponse>> GetAllWithPaginationAsync(Guid AuthorId, int page, int pageSize);
        Task<GetCaseNoteByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseNoteId);
        Task<List<SearchCaseNoteResponse>> SearchAsync(Guid CallerId, SearchCaseNoteRequest request);
        Task<CreateCaseNoteResponse> CreateAsync(Guid CreatorId, CreateCaseNoteRequest request);
        Task<UpdateCaseNoteResponse> UpdateAsync(Guid AuthorId, Guid CaseNoteId, UpdateCaseNoteRequest request);
        Task<FinalizeCaseNoteResponse> FinalizeAsync(Guid AuthorId, Guid CaseNoteId);
    }
}
