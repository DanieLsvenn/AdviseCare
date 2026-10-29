using AdviseCare.Application.Contracts.Requests.StudentData;
using AdviseCare.Application.Contracts.Responses.StudentData;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IStudentDataService
    {
        Task<IEnumerable<GetAllStudentDataResponse>> GetAll();
        Task<IEnumerable<GetAllStudentDataWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetStudentDataByIdResponse> GetByIdAsync(Guid StudentDataId);
        Task<List<SearchStudentDataResponse>> SearchAsync(SearchStudentDataRequest request);
        Task<CreateStudentDataResponse> CreateAsync(Guid CreatorId, CreateStudentDataRequest request);
        Task<PreviewStudentDataResponse> PreviewAsync(PreviewStudentDataRequest request);
        Task<ValidateStudentDataResponse> ValidateAsync(CreateStudentDataRequest request);
        Task<GetStudentDataOfStudentResponse> GetStudentDataAsync(Guid AccessorId, Guid StudentId, Guid? CaseId, Guid? TermId);
        Task<GetStudentDataAsOfResponse> GetAsOfAsync(Guid AccessorId, Guid StudentId, GetStudentDataAsOfRequest request);
        Task<CompareStudentDataResponse> CompareAsync(Guid AccessorId, Guid StudentId, CompareStudentDataRequest request);
        Task<GetMyStudentDataResponse> GetMyDataAsync(Guid StudentId, Guid? TermId);
        Task<ExportMyStudentDataResponse> ExportMyDataAsync(Guid StudentId);
    }
}
