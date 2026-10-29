using AdviseCare.Application.Contracts.Requests.Intervention;
using AdviseCare.Application.Contracts.Responses.Intervention;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Interventions: what the Welfare Officer does for a referred case (loan arrangement, financial aid, banking paperwork...); editable by its officer until closed
    public class InterventionService : IInterventionService
    {
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public InterventionService(
            IGenericRepository<Intervention> interventionRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Referral> referralRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _interventionRepository = interventionRepository;
            _caseRepository = caseRepository;
            _referralRepository = referralRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Interventions
        public async Task<IEnumerable<GetAllInterventionResponse>> GetAll()
        {
            var interventions = await GetInterventionsAsync(i => true);

            return interventions.Select(i => new GetAllInterventionResponse
            {
                InterventionId = i.InterventionId,
                CaseId = i.CaseId,
                OfficerId = i.OfficerId,
                StudentId = i.Case.StudentId,
                Notes = i.Notes ?? string.Empty,
                OfficerFullName = i.Officer.FullName,
                Outcome = i.Outcome ?? string.Empty,
                InterventionDate = i.InterventionDate,
                InterventionType = i.InterventionType,
                IsClosed = i.IsClosed,
                Status = i.Status
            });
        }

        // Get all Interventions with pagination
        public async Task<IEnumerable<GetAllInterventionWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var interventions = await GetInterventionsAsync(i => true);

            return interventions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new GetAllInterventionWithPaginationResponse
                {
                    InterventionId = i.InterventionId,
                    CaseId = i.CaseId,
                    OfficerId = i.OfficerId,
                    StudentId = i.Case.StudentId,
                    Notes = i.Notes ?? string.Empty,
                    OfficerFullName = i.Officer.FullName,
                    Outcome = i.Outcome ?? string.Empty,
                    InterventionDate = i.InterventionDate,
                    InterventionType = i.InterventionType,
                    IsClosed = i.IsClosed,
                    Status = i.Status
                });
        }

        // Get Intervention by Id
        public async Task<GetInterventionByIdResponse> GetByIdAsync(Guid InterventionId)
        {
            var intervention = (await GetInterventionsAsync(i => i.InterventionId == InterventionId)).FirstOrDefault()
                ?? throw new NotFoundException("Intervention not found.");

            return new GetInterventionByIdResponse
            {
                InterventionId = intervention.InterventionId,
                CaseId = intervention.CaseId,
                OfficerId = intervention.OfficerId,
                StudentId = intervention.Case.StudentId,
                Notes = intervention.Notes ?? string.Empty,
                OfficerFullName = intervention.Officer.FullName,
                Outcome = intervention.Outcome ?? string.Empty,
                InterventionDate = intervention.InterventionDate,
                InterventionType = intervention.InterventionType,
                IsClosed = intervention.IsClosed,
                Status = intervention.Status
            };
        }

        // Search Interventions by case, student (the student's intervention history) or status
        public async Task<List<SearchInterventionResponse>> SearchAsync(SearchInterventionRequest request)
        {
            var interventions = await GetInterventionsAsync(i =>
                (request.CaseId == null || i.CaseId == request.CaseId) &&
                (request.StudentId == null || i.Case.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || i.Status == request.Status));

            return interventions.Select(i => new SearchInterventionResponse
            {
                InterventionId = i.InterventionId,
                CaseId = i.CaseId,
                OfficerId = i.OfficerId,
                StudentId = i.Case.StudentId,
                Notes = i.Notes ?? string.Empty,
                OfficerFullName = i.Officer.FullName,
                Outcome = i.Outcome ?? string.Empty,
                InterventionDate = i.InterventionDate,
                InterventionType = i.InterventionType,
                IsClosed = i.IsClosed,
                Status = i.Status
            }).ToList();
        }

        // Create Intervention: the case must be open and referred to (and accepted by) this Welfare Officer
        public async Task<CreateInterventionResponse> CreateAsync(Guid CreatorId, CreateInterventionRequest request)
        {
            await EnsureWelfareCaseAsync(CreatorId, request.CaseId);

            var intervention = new Intervention
            {
                InterventionId = Guid.NewGuid(),
                CaseId = request.CaseId,
                OfficerId = CreatorId,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                InterventionDate = request.InterventionDate.ToUniversalTime(),
                InterventionType = request.InterventionType,
                IsClosed = false,
                Status = request.Status
            };
            await _interventionRepository.CreateAsync(intervention);

            await _activityLogService.LogAsync(CreatorId, "interventionLogged", $"Logged a {intervention.InterventionType} intervention for case {intervention.CaseId}");

            var created = (await GetInterventionsAsync(i => i.InterventionId == intervention.InterventionId)).Single();
            return new CreateInterventionResponse
            {
                InterventionId = created.InterventionId,
                CaseId = created.CaseId,
                OfficerId = created.OfficerId,
                StudentId = created.Case.StudentId,
                Notes = created.Notes ?? string.Empty,
                OfficerFullName = created.Officer.FullName,
                Outcome = created.Outcome ?? string.Empty,
                InterventionDate = created.InterventionDate,
                InterventionType = created.InterventionType,
                IsClosed = created.IsClosed,
                Status = created.Status
            };
        }

        // Update Intervention (only its officer, only while open): notes, outcome so far, date, type and status
        public async Task<UpdateInterventionResponse> UpdateAsync(Guid OfficerId, Guid InterventionId, UpdateInterventionRequest request)
        {
            var intervention = await GetOwnOpenInterventionAsync(OfficerId, InterventionId);

            intervention.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            intervention.Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim();
            intervention.InterventionDate = request.InterventionDate.ToUniversalTime();
            intervention.InterventionType = request.InterventionType;
            intervention.Status = request.Status;
            await _interventionRepository.SaveAsync();

            await _activityLogService.LogAsync(OfficerId, "interventionUpdated", $"Updated the intervention of case {intervention.CaseId} ({intervention.Status})");

            return new UpdateInterventionResponse
            {
                InterventionId = intervention.InterventionId,
                CaseId = intervention.CaseId,
                OfficerId = intervention.OfficerId,
                StudentId = intervention.Case.StudentId,
                Notes = intervention.Notes ?? string.Empty,
                OfficerFullName = intervention.Officer.FullName,
                Outcome = intervention.Outcome ?? string.Empty,
                InterventionDate = intervention.InterventionDate,
                InterventionType = intervention.InterventionType,
                IsClosed = intervention.IsClosed,
                Status = intervention.Status
            };
        }

        // Close Intervention with its outcome (status completed); the case's advisor is notified to continue follow-up and closure
        public async Task<CloseInterventionResponse> CloseAsync(Guid OfficerId, Guid InterventionId, CloseInterventionRequest request)
        {
            var intervention = await GetOwnOpenInterventionAsync(OfficerId, InterventionId);

            intervention.Outcome = request.Outcome.Trim();
            intervention.IsClosed = true;
            intervention.Status = "completed";
            await _interventionRepository.SaveAsync();

            if (intervention.Case.AdvisorId.HasValue)
                await _notificationService.NotifyAsync(intervention.Case.AdvisorId.Value, "Welfare intervention completed",
                    "The Welfare Officer completed the intervention for your case; you can continue the follow-up.", $"/cases/{intervention.CaseId}");
            await _activityLogService.LogAsync(OfficerId, "interventionUpdated", $"Closed the intervention of case {intervention.CaseId}");

            return new CloseInterventionResponse
            {
                InterventionId = intervention.InterventionId,
                CaseId = intervention.CaseId,
                OfficerId = intervention.OfficerId,
                StudentId = intervention.Case.StudentId,
                Notes = intervention.Notes ?? string.Empty,
                OfficerFullName = intervention.Officer.FullName,
                Outcome = intervention.Outcome,
                InterventionDate = intervention.InterventionDate,
                InterventionType = intervention.InterventionType,
                IsClosed = intervention.IsClosed,
                Status = intervention.Status
            };
        }

        // Helper: the officer's own intervention that is not closed yet
        private async Task<Intervention> GetOwnOpenInterventionAsync(Guid OfficerId, Guid InterventionId)
        {
            var intervention = (await GetInterventionsAsync(i => i.InterventionId == InterventionId)).FirstOrDefault()
                ?? throw new NotFoundException("Intervention not found.");
            if (intervention.OfficerId != OfficerId)
                throw new ForbiddenException("Only the Welfare Officer who logged the intervention can change it.");
            if (intervention.IsClosed)
                throw new ConflictException("A closed intervention cannot be changed.");
            return intervention;
        }

        // Helper: the Welfare Officer may only act on an open case that was referred to them and that they accepted (WF2 8.2)
        private async Task EnsureWelfareCaseAsync(Guid OfficerId, Guid CaseId)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (!await _referralRepository.ExistsAsync(r => r.CaseId == CaseId && r.ToAccountId == OfficerId && r.Status == "accepted"))
                throw new ForbiddenException("This case has not been referred to you (or you have not accepted the referral yet).");
        }

        // Helper: load Interventions with their case and officer, newest first
        private async Task<List<Intervention>> GetInterventionsAsync(Expression<Func<Intervention, bool>> predicate)
        {
            return await _interventionRepository.SearchAsyncIncludeOrderBy(predicate, i => i.InterventionDate, false,
                i => i.Case, i => i.Officer);
        }
    }
}
