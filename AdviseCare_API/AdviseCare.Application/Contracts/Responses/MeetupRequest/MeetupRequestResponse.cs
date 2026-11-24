namespace AdviseCare.Application.Contracts.Responses.MeetupRequest
{
    public class GetAllMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetAllMeetupRequestWithPaginationResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetMeetupRequestByIdResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SearchMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AcceptMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DeclineMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CancelMeetupRequestResponse
    {
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid AwaitingResponseFrom { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ProposedBy { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? RespondedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProposedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int RescheduleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ProposedEndAt { get; set; }
        public DateTime ProposedStartAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetAllMeetupScheduleResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetAllMeetupScheduleWithPaginationResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetMeetupScheduleByIdResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SearchMeetupScheduleResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CancelMeetupScheduleResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CompleteMeetupScheduleResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class MarkMissedMeetupScheduleResponse
    {
        public Guid MeetupScheduleId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CancelledBy { get; set; }
        public Guid MeetupRequestId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public string CancelledByFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocalDate { get; set; } = string.Empty;
        public string LocalEndTime { get; set; } = string.Empty;
        public string LocalStartTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Room { get; set; }
        public string StudentFullName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public DateTime ScheduledStartAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
