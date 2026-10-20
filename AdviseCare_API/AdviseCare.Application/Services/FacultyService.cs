using AdviseCare.Application.Contracts.Requests.Faculty;
using AdviseCare.Application.Contracts.Responses.Faculty;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class FacultyService : IFacultyService
    {
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IActivityLogService _activityLogService;

        public FacultyService(
            IGenericRepository<Faculty> facultyRepository,
            IActivityLogService activityLogService)
        {
            _facultyRepository = facultyRepository;
            _activityLogService = activityLogService;
        }

        // Get all Faculties
        public async Task<IEnumerable<GetAllFacultyResponse>> GetAll()
        {
            var faculties = await _facultyRepository.GetAllAsyncOrderBy(f => f.FacultyCode);

            return faculties.Select(f => new GetAllFacultyResponse
            {
                FacultyId = f.FacultyId,
                FacultyCode = f.FacultyCode,
                FacultyName = f.FacultyName,
                Status = f.Status
            });
        }

        // Get all Faculties with pagination
        public async Task<IEnumerable<GetAllFacultyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var faculties = await _facultyRepository.GetAllAsyncOrderBy(f => f.FacultyCode);

            return faculties
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new GetAllFacultyWithPaginationResponse
                {
                    FacultyId = f.FacultyId,
                    FacultyCode = f.FacultyCode,
                    FacultyName = f.FacultyName,
                    Status = f.Status
                });
        }

        // Get Faculty by Id
        public async Task<GetFacultyByIdResponse> GetByIdAsync(Guid FacultyId)
        {
            var faculty = await _facultyRepository.GetByIdAsync(FacultyId)
                ?? throw new NotFoundException("Faculty not found.");

            return new GetFacultyByIdResponse
            {
                FacultyId = faculty.FacultyId,
                FacultyCode = faculty.FacultyCode,
                FacultyName = faculty.FacultyName,
                Status = faculty.Status
            };
        }

        // Search Faculties by code, name or status
        public async Task<List<SearchFacultyResponse>> SearchAsync(SearchFacultyRequest request)
        {
            var faculties = await _facultyRepository.SearchAsyncOrderBy(f =>
                (string.IsNullOrWhiteSpace(request.FacultyCode) || f.FacultyCode.Contains(request.FacultyCode)) &&
                (string.IsNullOrWhiteSpace(request.FacultyName) || f.FacultyName.Contains(request.FacultyName)) &&
                (string.IsNullOrWhiteSpace(request.Status) || f.Status == request.Status),
                f => f.FacultyCode);

            return faculties.Select(f => new SearchFacultyResponse
            {
                FacultyId = f.FacultyId,
                FacultyCode = f.FacultyCode,
                FacultyName = f.FacultyName,
                Status = f.Status
            }).ToList();
        }

        // Create Faculty
        public async Task<CreateFacultyResponse> CreateAsync(CreateFacultyRequest request)
        {
            if (await _facultyRepository.ExistsAsync(f => f.FacultyCode == request.FacultyCode.Trim()))
                throw new ConflictException($"A faculty with code '{request.FacultyCode}' already exists.");

            var faculty = new Faculty
            {
                FacultyId = Guid.NewGuid(),
                FacultyCode = request.FacultyCode.Trim(),
                FacultyName = request.FacultyName.Trim(),
                Status = "active"
            };
            await _facultyRepository.CreateAsync(faculty);

            await _activityLogService.LogCurrentAccountAsync("facultyCreated", $"Created faculty {faculty.FacultyCode} - {faculty.FacultyName}");

            return new CreateFacultyResponse
            {
                FacultyId = faculty.FacultyId,
                FacultyCode = faculty.FacultyCode,
                FacultyName = faculty.FacultyName,
                Status = faculty.Status
            };
        }

        // Update Faculty
        public async Task<UpdateFacultyResponse> UpdateAsync(Guid FacultyId, UpdateFacultyRequest request)
        {
            var faculty = await _facultyRepository.GetByIdAsync(FacultyId)
                ?? throw new NotFoundException("Faculty not found.");

            if (await _facultyRepository.ExistsAsync(f => f.FacultyCode == request.FacultyCode.Trim() && f.FacultyId != FacultyId))
                throw new ConflictException($"A faculty with code '{request.FacultyCode}' already exists.");

            faculty.FacultyCode = request.FacultyCode.Trim();
            faculty.FacultyName = request.FacultyName.Trim();
            faculty.Status = request.Status.Trim();
            await _facultyRepository.UpdateAsync(faculty);

            await _activityLogService.LogCurrentAccountAsync("facultyUpdated", $"Updated faculty {faculty.FacultyCode} - {faculty.FacultyName} ({faculty.Status})");

            return new UpdateFacultyResponse
            {
                FacultyId = faculty.FacultyId,
                FacultyCode = faculty.FacultyCode,
                FacultyName = faculty.FacultyName,
                Status = faculty.Status
            };
        }
    }
}
