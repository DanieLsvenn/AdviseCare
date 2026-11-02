namespace AdviseCare.Application.Services
{
    // Role names, stages, statuses and priorities used by the Member D services (support requests, cases, matching, reports).
    // Values are exactly the strings stored in AdviseCareDB (NVARCHAR + CHECK constraint).
    public static class CaseRules
    {
        public const string StudentRole = "Student";
        public const string AcademicAdvisorRole = "Academic Advisor";
        public const string WellbeingAdvisorRole = "Wellbeing Advisor";
        public const string WelfareOfficerRole = "Student Welfare Officer";
        public const string ProgrammeHeadRole = "Programme Head";

        public const string Academic = "academic";
        public const string Wellbeing = "wellbeing";

        public const string StagePending = "pending";
        public const string StageAssigned = "assigned";
        public const string StageContacted = "contacted";
        public const string StageReferred = "referred";
        public const string StageFollowUp = "followUp";
        public const string StageClosed = "closed";

        public const string StatusOpen = "open";
        public const string StatusClosed = "closed";
        public const string StatusReopened = "reopened";

        public const string Active = "active";
        public const string Archived = "archived";
        public const string Revoked = "revoked";
        public const string Pending = "pending";

        public static readonly string[] SupportTypes = { Academic, Wellbeing };
        public static readonly string[] Stages = { StagePending, StageAssigned, StageContacted, StageReferred, StageFollowUp, StageClosed };
        public static readonly string[] CaseStatuses = { StatusOpen, StatusClosed, StatusReopened };
        public static readonly string[] Priorities = { "low", "medium", "high", "urgent" };
        public static readonly string[] AdvisorRoles = { AcademicAdvisorRole, WellbeingAdvisorRole };

        // Stages an advisor may move a case to with PUT /api/Cases/{id}/stage.
        // "assigned" only comes from matching, a reopen or an accepted referral; "referred" only from creating a referral; never back to "pending".
        public static readonly string[] ManualStages = { StageContacted, StageFollowUp, StageClosed };

        // Which advisor role handles which support type
        public static string AdvisorRoleFor(string supportType) =>
            supportType == Wellbeing ? WellbeingAdvisorRole : AcademicAdvisorRole;

        // urgent = 4 ... low = 1, used to order the matching queue
        public static int PriorityRank(string priority) => priority switch
        {
            "urgent" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };

        public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
        public static DateTime? AsUtc(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

        // Redirect URLs put in NOTIFICATIONS; the advisor's unread count per case is built from these
        public static string CaseUrl(Guid caseId) => $"/cases/{caseId}";
        public static string ReferralUrl(Guid referralId) => $"/referrals/{referralId}";
        public static string ChangeRequestUrl(Guid requestId) => $"/case-change-requests/{requestId}";
    }
}
