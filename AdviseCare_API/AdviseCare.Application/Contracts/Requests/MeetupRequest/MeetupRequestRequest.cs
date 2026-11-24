using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.MeetupRequest
{
    public class SearchMeetupRequestRequest
    {
        public Guid? CaseId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        [RegularExpression("^(|pending|accepted|declined|cancelled)$",
            ErrorMessage = "Status must be pending, accepted, declined or cancelled.")]
        public string Status { get; set; } = string.Empty;
    }

    // Meet-up date and start time are Vietnam time (UTC+7): Monday to Friday, 08:30-11:30 or 13:30-18:00, 15-minute steps
    public class CreateMeetupRequestRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Location is required."), StringLength(200,
            ErrorMessage = "Location cannot exceed 200 characters.")]
        public string Location { get; set; } = string.Empty;
        [Required(ErrorMessage = "Reason is required."), StringLength(1000,
            ErrorMessage = "Reason cannot exceed 1000 characters.")]
        public string Reason { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "Room cannot exceed 50 characters.")]
        public string? Room { get; set; }
        [Required(ErrorMessage = "Start time is required."), RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$",
            ErrorMessage = "Start time must be in HH:mm format, e.g. 14:00.")]
        public string StartTime { get; set; } = string.Empty;
        [Required(ErrorMessage = "Duration is required."), Range(15, 270,
            ErrorMessage = "Duration must be between 15 and 270 minutes.")]
        public int DurationMinutes { get; set; }
        [Required(ErrorMessage = "Meet-up date is required.")]
        public DateOnly MeetupDate { get; set; }
    }

    // Change date: the side that must answer proposes another date and time, and the request goes back to the other side
    public class UpdateMeetupRequestRequest
    {
        [Required(ErrorMessage = "Location is required."), StringLength(200,
            ErrorMessage = "Location cannot exceed 200 characters.")]
        public string Location { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "Room cannot exceed 50 characters.")]
        public string? Room { get; set; }
        [Required(ErrorMessage = "Start time is required."), RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$",
            ErrorMessage = "Start time must be in HH:mm format, e.g. 14:00.")]
        public string StartTime { get; set; } = string.Empty;
        [Required(ErrorMessage = "Duration is required."), Range(15, 270,
            ErrorMessage = "Duration must be between 15 and 270 minutes.")]
        public int DurationMinutes { get; set; }
        [Required(ErrorMessage = "Meet-up date is required.")]
        public DateOnly MeetupDate { get; set; }
    }

    public class DeclineMeetupRequestRequest
    {
        [Required(ErrorMessage = "Decline reason is required."), StringLength(1000,
            ErrorMessage = "Decline reason cannot exceed 1000 characters.")]
        public string DeclineReason { get; set; } = string.Empty;
    }

    public class SearchMeetupScheduleRequest
    {
        public Guid? CaseId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        [RegularExpression("^(|scheduled|completed|missed|cancelled)$",
            ErrorMessage = "Status must be scheduled, completed, missed or cancelled.")]
        public string Status { get; set; } = string.Empty;
    }

    public class CancelMeetupScheduleRequest
    {
        [Required(ErrorMessage = "Cancel reason is required."), StringLength(1000,
            ErrorMessage = "Cancel reason cannot exceed 1000 characters.")]
        public string CancelReason { get; set; } = string.Empty;
    }
}
