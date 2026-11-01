using AdviseCare.Application.Contracts.Requests.AssignmentChangeRequest;
using AdviseCare.Application.Contracts.Responses.AssignmentChangeRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAssignmentChangeRequestService
    {
        Task<AssignmentChangeRequestResponse> CreateAsync(Guid requestedBy, CreateAssignmentChangeRequestRequest request);
        Task<List<AssignmentChangeRequestResponse>> SearchAsync(Guid callerId, SearchAssignmentChangeRequestRequest request);
        Task<AssignmentChangeRequestResponse> GetByIdAsync(Guid callerId, Guid assignmentChangeRequestId);
        Task<AssignmentChangeRequestResponse> ApproveAsync(Guid programmeHeadId, Guid assignmentChangeRequestId);
        Task<AssignmentChangeRequestResponse> RejectAsync(Guid programmeHeadId, Guid assignmentChangeRequestId, RejectAssignmentChangeRequestRequest request);
    }
}
