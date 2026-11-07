using AdviseCare.Application.Contracts.Requests.CaseMessage;
using AdviseCare.Application.Contracts.Responses.CaseMessage;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseMessageService
    {
        Task<IEnumerable<GetAllCaseMessageResponse>> GetAll(Guid AuthorId);
        Task<IEnumerable<GetAllCaseMessageWithPaginationResponse>> GetAllWithPaginationAsync(Guid AuthorId, int page, int pageSize);
        Task<GetCaseMessageByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseMessageId);
        Task<List<SearchCaseMessageResponse>> SearchAsync(Guid CallerId, SearchCaseMessageRequest request);
        Task<CreateCaseMessageResponse> CreateAsync(Guid CreatorId, CreateCaseMessageRequest request);
    }
}
