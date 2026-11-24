using AdviseCare.Application.Contracts.Requests.MeetupRequest;
using AdviseCare.Application.Contracts.Responses.MeetupRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IMeetupRequestService
    {
        Task<IEnumerable<GetAllMeetupRequestResponse>> GetAll(Guid CallerId);
        Task<IEnumerable<GetAllMeetupRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize);
        Task<GetMeetupRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid MeetupRequestId);
        Task<List<SearchMeetupRequestResponse>> SearchAsync(Guid CallerId, SearchMeetupRequestRequest request);
        Task<CreateMeetupRequestResponse> CreateAsync(Guid CreatorId, CreateMeetupRequestRequest request);
        Task<UpdateMeetupRequestResponse> UpdateAsync(Guid CallerId, Guid MeetupRequestId, UpdateMeetupRequestRequest request);
        Task<AcceptMeetupRequestResponse> AcceptAsync(Guid CallerId, Guid MeetupRequestId);
        Task<DeclineMeetupRequestResponse> DeclineAsync(Guid CallerId, Guid MeetupRequestId, DeclineMeetupRequestRequest request);
        Task<CancelMeetupRequestResponse> CancelAsync(Guid CallerId, Guid MeetupRequestId);
        Task<IEnumerable<GetAllMeetupScheduleResponse>> GetAllScheduleAsync(Guid CallerId);
        Task<IEnumerable<GetAllMeetupScheduleWithPaginationResponse>> GetAllScheduleWithPaginationAsync(Guid CallerId, int page, int pageSize);
        Task<GetMeetupScheduleByIdResponse> GetScheduleByIdAsync(Guid CallerId, Guid MeetupScheduleId);
        Task<List<SearchMeetupScheduleResponse>> SearchScheduleAsync(Guid CallerId, SearchMeetupScheduleRequest request);
        Task<CancelMeetupScheduleResponse> CancelScheduleAsync(Guid CallerId, Guid MeetupScheduleId, CancelMeetupScheduleRequest request);
        Task<CompleteMeetupScheduleResponse> CompleteScheduleAsync(Guid AdvisorId, Guid MeetupScheduleId);
        Task<MarkMissedMeetupScheduleResponse> MarkMissedScheduleAsync(Guid AdvisorId, Guid MeetupScheduleId);
        Task CancelOpenMeetupsForCaseAsync(Guid CaseId, Guid ChangedBy, string Reason);
    }
}
