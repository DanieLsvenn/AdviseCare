using AdviseCare.Application.Contracts.Requests.CaseNote;
using AdviseCare.Application.Contracts.Responses.CaseNote;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Case notes: the case's current advisor writes a draft that only they can see and edit;
    // once finalized it is visible to the staff of the case and never changes again. Students never read case notes
    public class CaseNoteService : ICaseNoteService
    {
        private readonly IGenericRepository<Casenote> _caseNoteRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly IActivityLogService _activityLogService;

        public CaseNoteService(
            IGenericRepository<Casenote> caseNoteRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Referral> referralRepository,
            IActivityLogService activityLogService)
        {
            _caseNoteRepository = caseNoteRepository;
            _caseRepository = caseRepository;
            _accountRoleRepository = accountRoleRepository;
            _referralRepository = referralRepository;
            _activityLogService = activityLogService;
        }

        // Get all Case notes written by the signed-in advisor, newest first (drafts and finalized)
        public async Task<IEnumerable<GetAllCaseNoteResponse>> GetAll(Guid AuthorId)
        {
            var notes = await _caseNoteRepository.SearchAsyncIncludeOrderBy(n => n.AuthorId == AuthorId, n => n.CreatedAt, false, n => n.Author);

            return notes.Select(n => new GetAllCaseNoteResponse
            {
                CaseNoteId = n.CaseNoteId,
                AuthorId = n.AuthorId,
                CaseId = n.CaseId,
                AuthorFullName = n.Author?.FullName ?? string.Empty,
                NoteText = n.NoteText,
                CreatedAt = n.CreatedAt,
                EditedAt = n.EditedAt,
                IsFinalized = n.IsFinalized
            });
        }

        // Get all Case notes written by the signed-in advisor with pagination
        public async Task<IEnumerable<GetAllCaseNoteWithPaginationResponse>> GetAllWithPaginationAsync(Guid AuthorId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var notes = await _caseNoteRepository.SearchAsyncIncludeOrderBy(n => n.AuthorId == AuthorId, n => n.CreatedAt, false, n => n.Author);

            return notes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new GetAllCaseNoteWithPaginationResponse
                {
                    CaseNoteId = n.CaseNoteId,
                    AuthorId = n.AuthorId,
                    CaseId = n.CaseId,
                    AuthorFullName = n.Author?.FullName ?? string.Empty,
                    NoteText = n.NoteText,
                    CreatedAt = n.CreatedAt,
                    EditedAt = n.EditedAt,
                    IsFinalized = n.IsFinalized
                });
        }

        // Get Case note by Id (a draft only for its author; a finalized note for the staff of the case)
        public async Task<GetCaseNoteByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseNoteId)
        {
            var note = await _caseNoteRepository.GetByIdAsyncInclude(CaseNoteId, n => n.Author, n => n.Case)
                ?? throw new NotFoundException("Case note not found.");
            if (note.AuthorId != CallerId && (!note.IsFinalized || !await CanViewCaseAsStaffAsync(note.Case, CallerId)))
                throw new NotFoundException("Case note not found.");

            return new GetCaseNoteByIdResponse
            {
                CaseNoteId = note.CaseNoteId,
                AuthorId = note.AuthorId,
                CaseId = note.CaseId,
                AuthorFullName = note.Author?.FullName ?? string.Empty,
                NoteText = note.NoteText,
                CreatedAt = note.CreatedAt,
                EditedAt = note.EditedAt,
                IsFinalized = note.IsFinalized
            };
        }

        // Search Case notes of one case: its finalized notes plus the caller's own drafts, oldest first (staff of the case only)
        public async Task<List<SearchCaseNoteResponse>> SearchAsync(Guid CallerId, SearchCaseNoteRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (!await CanViewCaseAsStaffAsync(caseRecord, CallerId))
                throw new NotFoundException("Case not found.");

            var notes = await _caseNoteRepository.SearchAsyncIncludeOrderBy(n =>
                n.CaseId == request.CaseId &&
                (n.IsFinalized || n.AuthorId == CallerId) &&
                (request.IsFinalized == null || n.IsFinalized == request.IsFinalized),
                n => n.CreatedAt, true, n => n.Author);

            return notes.Select(n => new SearchCaseNoteResponse
            {
                CaseNoteId = n.CaseNoteId,
                AuthorId = n.AuthorId,
                CaseId = n.CaseId,
                AuthorFullName = n.Author?.FullName ?? string.Empty,
                NoteText = n.NoteText,
                CreatedAt = n.CreatedAt,
                EditedAt = n.EditedAt,
                IsFinalized = n.IsFinalized
            }).ToList();
        }

        // Create Case note as a draft (only the case's current advisor, on a case that is not closed)
        public async Task<CreateCaseNoteResponse> CreateAsync(Guid CreatorId, CreateCaseNoteRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (caseRecord.AdvisorId != CreatorId)
            {
                if (!await CanViewCaseAsStaffAsync(caseRecord, CreatorId))
                    throw new NotFoundException("Case not found.");
                throw new ForbiddenException("Only the case's current advisor can write case notes.");
            }
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed. Reopen it to add notes.");

            var noteText = request.NoteText.Trim();
            if (noteText.Length == 0)
                throw new ValidationException("Note text is required.");

            var note = new Casenote
            {
                CaseNoteId = Guid.NewGuid(),
                AuthorId = CreatorId,
                CaseId = caseRecord.CaseId,
                NoteText = noteText,
                CreatedAt = DateTime.UtcNow,
                IsFinalized = false
            };
            await _caseNoteRepository.CreateAsync(note);

            var created = await _caseNoteRepository.GetByIdAsyncInclude(note.CaseNoteId, n => n.Author)
                ?? throw new NotFoundException("Case note not found.");

            return new CreateCaseNoteResponse
            {
                CaseNoteId = created.CaseNoteId,
                AuthorId = created.AuthorId,
                CaseId = created.CaseId,
                AuthorFullName = created.Author?.FullName ?? string.Empty,
                NoteText = created.NoteText,
                CreatedAt = created.CreatedAt,
                EditedAt = created.EditedAt,
                IsFinalized = created.IsFinalized
            };
        }

        // Update Case note: the author edits their own draft (sets editedAt); a finalized note cannot change
        public async Task<UpdateCaseNoteResponse> UpdateAsync(Guid AuthorId, Guid CaseNoteId, UpdateCaseNoteRequest request)
        {
            var note = await GetOwnDraftAsync(AuthorId, CaseNoteId);
            var noteText = request.NoteText.Trim();
            if (noteText.Length == 0)
                throw new ValidationException("Note text is required.");

            note.NoteText = noteText;
            note.EditedAt = DateTime.UtcNow;
            _caseNoteRepository.PrepareUpdate(note);
            await _caseNoteRepository.SaveAsync();

            return new UpdateCaseNoteResponse
            {
                CaseNoteId = note.CaseNoteId,
                AuthorId = note.AuthorId,
                CaseId = note.CaseId,
                AuthorFullName = note.Author?.FullName ?? string.Empty,
                NoteText = note.NoteText,
                CreatedAt = note.CreatedAt,
                EditedAt = note.EditedAt,
                IsFinalized = note.IsFinalized
            };
        }

        // Finalize Case note: the author finalizes their draft; from then on it is visible to the staff of the case and can never change
        public async Task<FinalizeCaseNoteResponse> FinalizeAsync(Guid AuthorId, Guid CaseNoteId)
        {
            var note = await GetOwnDraftAsync(AuthorId, CaseNoteId);
            note.IsFinalized = true;
            _caseNoteRepository.PrepareUpdate(note);
            await _caseNoteRepository.SaveAsync();

            var caseRecord = await _caseRepository.GetByIdAsyncInclude(note.CaseId, c => c.Student, c => c.SupportCategory)
                ?? throw new NotFoundException("Case not found.");
            await _activityLogService.LogAsync(AuthorId, "caseNoteFinalized", $"Finalized a note on the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName}");

            return new FinalizeCaseNoteResponse
            {
                CaseNoteId = note.CaseNoteId,
                AuthorId = note.AuthorId,
                CaseId = note.CaseId,
                AuthorFullName = note.Author?.FullName ?? string.Empty,
                NoteText = note.NoteText,
                CreatedAt = note.CreatedAt,
                EditedAt = note.EditedAt,
                IsFinalized = note.IsFinalized
            };
        }

        // Helper: the author's own draft note
        private async Task<Casenote> GetOwnDraftAsync(Guid AuthorId, Guid CaseNoteId)
        {
            var note = await _caseNoteRepository.GetByIdAsyncInclude(CaseNoteId, n => n.Author)
                ?? throw new NotFoundException("Case note not found.");
            if (note.AuthorId != AuthorId)
                throw new NotFoundException("Case note not found.");
            if (note.IsFinalized)
                throw new ConflictException("A finalized note cannot be changed.");
            return note;
        }

        // Helper: the staff of a case are its current advisor, the Programme Head, and anyone who sent or received a referral of it (never the student)
        private async Task<bool> CanViewCaseAsStaffAsync(Case CaseRecord, Guid CallerId)
        {
            if (CaseRecord.AdvisorId == CallerId)
                return true;
            if (CaseRecord.StudentId == CallerId)
                return false;
            if (await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == CallerId && ar.Role.RoleName == "Programme Head"))
                return true;
            return await _referralRepository.ExistsAsync(r => r.CaseId == CaseRecord.CaseId && (r.FromAccountId == CallerId || r.ToAccountId == CallerId));
        }
    }
}
