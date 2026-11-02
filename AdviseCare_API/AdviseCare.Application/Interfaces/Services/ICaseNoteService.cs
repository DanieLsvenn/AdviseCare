using AdviseCare.Application.Contracts.Requests.CaseNote;
using AdviseCare.Application.Contracts.Responses.CaseNote;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseNoteService
    {
        Task<CaseNoteResponse> CreateAsync(Guid authorId, CreateCaseNoteRequest request);
        Task<CaseNoteResponse> UpdateAsync(Guid authorId, Guid caseNoteId, UpdateCaseNoteRequest request);
        Task<CaseNoteResponse> FinalizeAsync(Guid authorId, Guid caseNoteId);
        Task<CaseNoteResponse> GetByIdAsync(Guid callerId, Guid caseNoteId);
        Task<List<CaseNoteResponse>> SearchAsync(Guid callerId, SearchCaseNoteRequest request);
    }
}
