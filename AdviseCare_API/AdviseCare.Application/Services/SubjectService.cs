using AdviseCare.Application.Contracts.Requests.Subject;
using AdviseCare.Application.Contracts.Responses.Subject;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IGenericRepository<Subject> _subjectRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IGenericRepository<AccountSubject> _accountSubjectRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IActivityLogService _activityLogService;

        public SubjectService(
            IGenericRepository<Subject> subjectRepository,
            IGenericRepository<Faculty> facultyRepository,
            IGenericRepository<AccountSubject> accountSubjectRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IActivityLogService activityLogService)
        {
            _subjectRepository = subjectRepository;
            _facultyRepository = facultyRepository;
            _accountSubjectRepository = accountSubjectRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _activityLogService = activityLogService;
        }

        // Get all Subjects
        public async Task<IEnumerable<GetAllSubjectResponse>> GetAll()
        {
            var subjects = await _subjectRepository.GetAllAsyncOrderBy(s => s.SubjectCode);

            return subjects.Select(s => new GetAllSubjectResponse
            {
                SubjectId = s.SubjectId,
                FacultyId = s.FacultyId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Status = s.Status
            });
        }

        // Get all Subjects with pagination
        public async Task<IEnumerable<GetAllSubjectWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var subjects = await _subjectRepository.GetAllAsyncOrderBy(s => s.SubjectCode);

            return subjects
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new GetAllSubjectWithPaginationResponse
                {
                    SubjectId = s.SubjectId,
                    FacultyId = s.FacultyId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Status = s.Status
                });
        }

        // Get Subject by Id
        public async Task<GetSubjectByIdResponse> GetByIdAsync(Guid SubjectId)
        {
            var subject = await _subjectRepository.GetByIdAsync(SubjectId)
                ?? throw new NotFoundException("Subject not found.");

            return new GetSubjectByIdResponse
            {
                SubjectId = subject.SubjectId,
                FacultyId = subject.FacultyId,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Status = subject.Status
            };
        }

        // Search Subjects by faculty, code, name or status
        public async Task<List<SearchSubjectResponse>> SearchAsync(SearchSubjectRequest request)
        {
            var subjects = await _subjectRepository.SearchAsyncOrderBy(s =>
                (request.FacultyId == null || s.FacultyId == request.FacultyId) &&
                (string.IsNullOrWhiteSpace(request.SubjectCode) || s.SubjectCode.Contains(request.SubjectCode)) &&
                (string.IsNullOrWhiteSpace(request.SubjectName) || s.SubjectName.Contains(request.SubjectName)) &&
                (string.IsNullOrWhiteSpace(request.Status) || s.Status == request.Status),
                s => s.SubjectCode);

            return subjects.Select(s => new SearchSubjectResponse
            {
                SubjectId = s.SubjectId,
                FacultyId = s.FacultyId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Status = s.Status
            }).ToList();
        }

        // Create Subject
        public async Task<CreateSubjectResponse> CreateAsync(CreateSubjectRequest request)
        {
            if (await _subjectRepository.ExistsAsync(s => s.SubjectCode == request.SubjectCode.Trim()))
                throw new ConflictException($"A subject with code '{request.SubjectCode}' already exists.");
            if (!await _facultyRepository.ExistsAsync(f => f.FacultyId == request.FacultyId))
                throw new NotFoundException("Faculty not found.");

            var subject = new Subject
            {
                SubjectId = Guid.NewGuid(),
                FacultyId = request.FacultyId,
                SubjectCode = request.SubjectCode.Trim(),
                SubjectName = request.SubjectName.Trim(),
                Status = "active"
            };
            await _subjectRepository.CreateAsync(subject);

            await _activityLogService.LogCurrentAccountAsync("subjectCreated", $"Created subject {subject.SubjectCode} - {subject.SubjectName}");

            return new CreateSubjectResponse
            {
                SubjectId = subject.SubjectId,
                FacultyId = subject.FacultyId,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Status = subject.Status
            };
        }

        // Update Subject
        public async Task<UpdateSubjectResponse> UpdateAsync(Guid SubjectId, UpdateSubjectRequest request)
        {
            var subject = await _subjectRepository.GetByIdAsync(SubjectId)
                ?? throw new NotFoundException("Subject not found.");

            if (await _subjectRepository.ExistsAsync(s => s.SubjectCode == request.SubjectCode.Trim() && s.SubjectId != SubjectId))
                throw new ConflictException($"A subject with code '{request.SubjectCode}' already exists.");
            if (!await _facultyRepository.ExistsAsync(f => f.FacultyId == request.FacultyId))
                throw new NotFoundException("Faculty not found.");

            subject.FacultyId = request.FacultyId;
            subject.SubjectCode = request.SubjectCode.Trim();
            subject.SubjectName = request.SubjectName.Trim();
            subject.Status = request.Status.Trim();
            await _subjectRepository.UpdateAsync(subject);

            await _activityLogService.LogCurrentAccountAsync("subjectUpdated", $"Updated subject {subject.SubjectCode} - {subject.SubjectName} ({subject.Status})");

            return new UpdateSubjectResponse
            {
                SubjectId = subject.SubjectId,
                FacultyId = subject.FacultyId,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Status = subject.Status
            };
        }

        // Get all advisor-subject links (ACCOUNT_SUBJECT)
        public async Task<IEnumerable<GetAllAccountSubjectResponse>> GetAllAccountSubjectAsync()
        {
            var accountSubjects = await _accountSubjectRepository.GetAllAsyncIncludeOrderBy(a => a.AssignedAt, true, a => a.Account, a => a.Subject);

            return accountSubjects.Select(a => new GetAllAccountSubjectResponse
            {
                AccountId = a.AccountId,
                SubjectId = a.SubjectId,
                AdvisorFullName = a.Account.FullName,
                SubjectCode = a.Subject.SubjectCode,
                SubjectName = a.Subject.SubjectName,
                AssignedAt = a.AssignedAt
            });
        }

        // Get all advisor-subject links (ACCOUNT_SUBJECT) with pagination
        public async Task<IEnumerable<GetAllAccountSubjectWithPaginationResponse>> GetAllAccountSubjectWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var accountSubjects = await _accountSubjectRepository.GetAllAsyncIncludeOrderBy(a => a.AssignedAt, true, a => a.Account, a => a.Subject);

            return accountSubjects
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new GetAllAccountSubjectWithPaginationResponse
                {
                    AccountId = a.AccountId,
                    SubjectId = a.SubjectId,
                    AdvisorFullName = a.Account.FullName,
                    SubjectCode = a.Subject.SubjectCode,
                    SubjectName = a.Subject.SubjectName,
                    AssignedAt = a.AssignedAt
                });
        }

        // Get an advisor-subject link (ACCOUNT_SUBJECT) by advisor Id and subject Id
        public async Task<GetAccountSubjectByIdResponse> GetAccountSubjectByIdAsync(Guid AccountId, Guid SubjectId)
        {
            var accountSubject = await _accountSubjectRepository.FirstOrDefaultAsync(a => a.AccountId == AccountId && a.SubjectId == SubjectId, a => a.Account, a => a.Subject)
                ?? throw new NotFoundException("This advisor is not linked to this subject.");

            return new GetAccountSubjectByIdResponse
            {
                AccountId = accountSubject.AccountId,
                SubjectId = accountSubject.SubjectId,
                AdvisorFullName = accountSubject.Account.FullName,
                SubjectCode = accountSubject.Subject.SubjectCode,
                SubjectName = accountSubject.Subject.SubjectName,
                AssignedAt = accountSubject.AssignedAt
            };
        }

        // Search advisor-subject links (ACCOUNT_SUBJECT) by advisor or subject
        public async Task<List<SearchAccountSubjectResponse>> SearchAccountSubjectAsync(SearchAccountSubjectRequest request)
        {
            var accountSubjects = await _accountSubjectRepository.SearchAsyncIncludeOrderBy(a =>
                (request.AccountId == null || a.AccountId == request.AccountId) &&
                (request.SubjectId == null || a.SubjectId == request.SubjectId),
                a => a.AssignedAt, true, a => a.Account, a => a.Subject);

            return accountSubjects.Select(a => new SearchAccountSubjectResponse
            {
                AccountId = a.AccountId,
                SubjectId = a.SubjectId,
                AdvisorFullName = a.Account.FullName,
                SubjectCode = a.Subject.SubjectCode,
                SubjectName = a.Subject.SubjectName,
                AssignedAt = a.AssignedAt
            }).ToList();
        }

        // Link an Academic Advisor to a Subject they specialise in (ACCOUNT_SUBJECT)
        public async Task<CreateAccountSubjectResponse> CreateAccountSubjectAsync(CreateAccountSubjectRequest request)
        {
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId)
                ?? throw new NotFoundException("Subject not found.");
            if (subject.Status != "active")
                throw new ValidationException("Advisors can only be linked to an active subject.");

            var account = await _accountRepository.GetByIdAsync(request.AccountId)
                ?? throw new NotFoundException("Account not found.");
            var isAcademicAdvisor = await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == request.AccountId && ar.Role.RoleName == "Academic Advisor");
            if (!isAcademicAdvisor)
                throw new ValidationException("Only an account with the Academic Advisor role can be linked to a subject.");

            if (await _accountSubjectRepository.ExistsAsync(a => a.AccountId == request.AccountId && a.SubjectId == request.SubjectId))
                throw new ConflictException("This advisor is already linked to this subject.");

            var accountSubject = new AccountSubject
            {
                AccountId = request.AccountId,
                SubjectId = request.SubjectId,
                AssignedAt = DateTime.UtcNow
            };
            await _accountSubjectRepository.CreateAsync(accountSubject);

            await _activityLogService.LogCurrentAccountAsync("advisorSubjectLinked", $"Linked {account.FullName} to subject {subject.SubjectCode}");

            return new CreateAccountSubjectResponse
            {
                AccountId = accountSubject.AccountId,
                SubjectId = accountSubject.SubjectId,
                AdvisorFullName = account.FullName,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                AssignedAt = accountSubject.AssignedAt
            };
        }

        // Remove an advisor-subject link (ACCOUNT_SUBJECT)
        public async Task<bool> DeleteAccountSubjectAsync(Guid AccountId, Guid SubjectId)
        {
            var accountSubject = await _accountSubjectRepository.FirstOrDefaultAsync(a => a.AccountId == AccountId && a.SubjectId == SubjectId, a => a.Account, a => a.Subject)
                ?? throw new NotFoundException("This advisor is not linked to this subject.");

            var detail = $"Unlinked {accountSubject.Account.FullName} from subject {accountSubject.Subject.SubjectCode}";
            await _accountSubjectRepository.RemoveAsync(accountSubject);

            await _activityLogService.LogCurrentAccountAsync("advisorSubjectUnlinked", detail);
            return true;
        }
    }
}
