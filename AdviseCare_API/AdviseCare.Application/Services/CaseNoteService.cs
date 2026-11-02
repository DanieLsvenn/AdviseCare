using AdviseCare.Application.Contracts.Requests.CaseNote;
using AdviseCare.Application.Contracts.Responses.CaseNote;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D4.3: case notes. A note starts as a draft that only its author sees and can edit;
    // once finalized it is visible to the staff involved in the case and can never change again.
    public class CaseNoteService(
        IGenericRepository<Casenote> noteRepository,
        IGenericRepository<Case> caseRepository,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : ICaseNoteService
    {
        private readonly IGenericRepository<Casenote> _noteRepository = noteRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        public async Task<CaseNoteResponse> CreateAsync(Guid authorId, CreateCaseNoteRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            await _workflow.EnsureCanViewCaseAsync(caseRecord, authorId, await _workflow.GetRoleNamesAsync(authorId));
            if (caseRecord.AdvisorId != authorId)
                throw new ForbiddenException("Only the case's current advisor can write case notes.");
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed. Reopen it to add notes.");

            var note = new Casenote
            {
                CaseNoteId = Guid.NewGuid(),
                AuthorId = authorId,
                CaseId = caseRecord.CaseId,
                NoteText = CaseWorkflowHelper.RequireText(request.NoteText, "Note text is required."),
                CreatedAt = DateTime.UtcNow,
                IsFinalized = false
            };
            await _noteRepository.CreateAsync(note);
            return await GetByIdAsync(authorId, note.CaseNoteId);
        }

        public async Task<CaseNoteResponse> UpdateAsync(Guid authorId, Guid caseNoteId, UpdateCaseNoteRequest request)
        {
            var note = await GetOwnDraftAsync(authorId, caseNoteId);
            note.NoteText = CaseWorkflowHelper.RequireText(request.NoteText, "Note text is required.");
            note.EditedAt = DateTime.UtcNow;
            _noteRepository.PrepareUpdate(note);
            await _noteRepository.SaveAsync();
            return ToResponse(note);
        }

        public async Task<CaseNoteResponse> FinalizeAsync(Guid authorId, Guid caseNoteId)
        {
            var note = await GetOwnDraftAsync(authorId, caseNoteId);
            note.IsFinalized = true;
            _noteRepository.PrepareUpdate(note);
            await _noteRepository.SaveAsync();

            var caseRecord = await _workflow.GetCaseWithDetailsAsync(note.CaseId);
            await _activityLogService.LogAsync(authorId, "caseNoteFinalized",
                $"Finalized a note on the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}");
            return ToResponse(note);
        }

        public async Task<CaseNoteResponse> GetByIdAsync(Guid callerId, Guid caseNoteId)
        {
            var note = await _noteRepository.GetByIdAsyncInclude(caseNoteId, n => n.Author, n => n.Case)
                ?? throw new NotFoundException("Case note not found.");
            if (note.AuthorId != callerId)
            {
                // Drafts are private to their author; finalized notes follow the case's staff visibility
                if (!note.IsFinalized ||
                    !await _workflow.CanViewCaseAsStaffAsync(note.Case, callerId, await _workflow.GetRoleNamesAsync(callerId)))
                    throw new NotFoundException("Case note not found.");
            }
            return ToResponse(note);
        }

        public async Task<List<CaseNoteResponse>> SearchAsync(Guid callerId, SearchCaseNoteRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (!await _workflow.CanViewCaseAsStaffAsync(caseRecord, callerId, await _workflow.GetRoleNamesAsync(callerId)))
                throw new NotFoundException("Case not found.");

            var notes = await _noteRepository.SearchAsyncIncludeOrderBy(
                n => n.CaseId == request.CaseId &&
                     (n.IsFinalized || n.AuthorId == callerId) &&
                     (!request.IsFinalized.HasValue || n.IsFinalized == request.IsFinalized.Value),
                n => n.CreatedAt,
                true,
                n => n.Author);
            return notes.Select(ToResponse).ToList();
        }

        private async Task<Casenote> GetOwnDraftAsync(Guid authorId, Guid caseNoteId)
        {
            var note = await _noteRepository.GetByIdAsyncInclude(caseNoteId, n => n.Author)
                ?? throw new NotFoundException("Case note not found.");
            if (note.AuthorId != authorId)
                throw new NotFoundException("Case note not found.");
            if (note.IsFinalized)
                throw new ValidationException("A finalized note cannot be changed.");
            return note;
        }

        private static CaseNoteResponse ToResponse(Casenote note) => new()
        {
            CaseNoteId = note.CaseNoteId,
            CaseId = note.CaseId,
            AuthorId = note.AuthorId,
            AuthorName = note.Author?.FullName ?? string.Empty,
            NoteText = note.NoteText,
            CreatedAt = CaseRules.AsUtc(note.CreatedAt),
            EditedAt = CaseRules.AsUtc(note.EditedAt),
            IsFinalized = note.IsFinalized
        };
    }
}
