using AdviseCare.Application.Contracts.Requests.Programme;
using AdviseCare.Application.Contracts.Responses.Programme;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class ProgrammeService : IProgrammeService
    {
        private readonly IGenericRepository<Programme> _programmeRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IActivityLogService _activityLogService;

        public ProgrammeService(
            IGenericRepository<Programme> programmeRepository,
            IGenericRepository<Faculty> facultyRepository,
            IActivityLogService activityLogService)
        {
            _programmeRepository = programmeRepository;
            _facultyRepository = facultyRepository;
            _activityLogService = activityLogService;
        }

        // Get all Programmes
        public async Task<IEnumerable<GetAllProgrammeResponse>> GetAll()
        {
            var programmes = await _programmeRepository.GetAllAsyncOrderBy(p => p.ProgrammeCode);

            return programmes.Select(p => new GetAllProgrammeResponse
            {
                ProgrammeId = p.ProgrammeId,
                FacultyId = p.FacultyId,
                ProgrammeCode = p.ProgrammeCode,
                ProgrammeName = p.ProgrammeName,
                Status = p.Status
            });
        }

        // Get all Programmes with pagination
        public async Task<IEnumerable<GetAllProgrammeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var programmes = await _programmeRepository.GetAllAsyncOrderBy(p => p.ProgrammeCode);

            return programmes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetAllProgrammeWithPaginationResponse
                {
                    ProgrammeId = p.ProgrammeId,
                    FacultyId = p.FacultyId,
                    ProgrammeCode = p.ProgrammeCode,
                    ProgrammeName = p.ProgrammeName,
                    Status = p.Status
                });
        }

        // Get Programme by Id
        public async Task<GetProgrammeByIdResponse> GetByIdAsync(Guid ProgrammeId)
        {
            var programme = await _programmeRepository.GetByIdAsync(ProgrammeId)
                ?? throw new NotFoundException("Programme not found.");

            return new GetProgrammeByIdResponse
            {
                ProgrammeId = programme.ProgrammeId,
                FacultyId = programme.FacultyId,
                ProgrammeCode = programme.ProgrammeCode,
                ProgrammeName = programme.ProgrammeName,
                Status = programme.Status
            };
        }

        // Search Programmes by faculty, code, name or status
        public async Task<List<SearchProgrammeResponse>> SearchAsync(SearchProgrammeRequest request)
        {
            var programmes = await _programmeRepository.SearchAsyncOrderBy(p =>
                (request.FacultyId == null || p.FacultyId == request.FacultyId) &&
                (string.IsNullOrWhiteSpace(request.ProgrammeCode) || p.ProgrammeCode.Contains(request.ProgrammeCode)) &&
                (string.IsNullOrWhiteSpace(request.ProgrammeName) || p.ProgrammeName.Contains(request.ProgrammeName)) &&
                (string.IsNullOrWhiteSpace(request.Status) || p.Status == request.Status),
                p => p.ProgrammeCode);

            return programmes.Select(p => new SearchProgrammeResponse
            {
                ProgrammeId = p.ProgrammeId,
                FacultyId = p.FacultyId,
                ProgrammeCode = p.ProgrammeCode,
                ProgrammeName = p.ProgrammeName,
                Status = p.Status
            }).ToList();
        }

        // Create Programme
        public async Task<CreateProgrammeResponse> CreateAsync(CreateProgrammeRequest request)
        {
            if (await _programmeRepository.ExistsAsync(p => p.ProgrammeCode == request.ProgrammeCode.Trim()))
                throw new ConflictException($"A programme with code '{request.ProgrammeCode}' already exists.");
            if (!await _facultyRepository.ExistsAsync(f => f.FacultyId == request.FacultyId))
                throw new NotFoundException("Faculty not found.");

            var programme = new Programme
            {
                ProgrammeId = Guid.NewGuid(),
                FacultyId = request.FacultyId,
                ProgrammeCode = request.ProgrammeCode.Trim(),
                ProgrammeName = request.ProgrammeName.Trim(),
                Status = "active"
            };
            await _programmeRepository.CreateAsync(programme);

            await _activityLogService.LogCurrentAccountAsync("programmeCreated", $"Created programme {programme.ProgrammeCode} - {programme.ProgrammeName}");

            return new CreateProgrammeResponse
            {
                ProgrammeId = programme.ProgrammeId,
                FacultyId = programme.FacultyId,
                ProgrammeCode = programme.ProgrammeCode,
                ProgrammeName = programme.ProgrammeName,
                Status = programme.Status
            };
        }

        // Update Programme
        public async Task<UpdateProgrammeResponse> UpdateAsync(Guid ProgrammeId, UpdateProgrammeRequest request)
        {
            var programme = await _programmeRepository.GetByIdAsync(ProgrammeId)
                ?? throw new NotFoundException("Programme not found.");

            if (await _programmeRepository.ExistsAsync(p => p.ProgrammeCode == request.ProgrammeCode.Trim() && p.ProgrammeId != ProgrammeId))
                throw new ConflictException($"A programme with code '{request.ProgrammeCode}' already exists.");
            if (!await _facultyRepository.ExistsAsync(f => f.FacultyId == request.FacultyId))
                throw new NotFoundException("Faculty not found.");

            programme.FacultyId = request.FacultyId;
            programme.ProgrammeCode = request.ProgrammeCode.Trim();
            programme.ProgrammeName = request.ProgrammeName.Trim();
            programme.Status = request.Status.Trim();
            await _programmeRepository.UpdateAsync(programme);

            await _activityLogService.LogCurrentAccountAsync("programmeUpdated", $"Updated programme {programme.ProgrammeCode} - {programme.ProgrammeName} ({programme.Status})");

            return new UpdateProgrammeResponse
            {
                ProgrammeId = programme.ProgrammeId,
                FacultyId = programme.FacultyId,
                ProgrammeCode = programme.ProgrammeCode,
                ProgrammeName = programme.ProgrammeName,
                Status = programme.Status
            };
        }
    }
}
