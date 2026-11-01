using AdviseCare.Application.Contracts.Requests.AdvisorAssignment;
using AdviseCare.Application.Contracts.Responses.AdvisorAssignment;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAdvisorAssignmentService
    {
        Task<List<AdvisorAssignmentResponse>> SearchAsync(Guid callerId, SearchAdvisorAssignmentRequest request);
        Task<List<AdvisorAssignmentResponse>> GetMyStudentsAsync(Guid advisorId);
        Task<AdvisorAssignmentResponse> GetByIdAsync(Guid callerId, Guid advisorAssignmentId);
        Task<AdvisorHandoverResponse> ReassignAsync(Guid programmeHeadId, Guid advisorAssignmentId, ReassignAdvisorAssignmentRequest request);
    }
}
