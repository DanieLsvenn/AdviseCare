using AdviseCare.Application.Contracts.Requests.AcademicTerm;
using AdviseCare.Application.Contracts.Responses.AcademicTerm;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class AcademicTermService : IAcademicTermService
    {
        private readonly IGenericRepository<Academicterm> _academicTermRepository;
        private readonly IActivityLogService _activityLogService;

        public AcademicTermService(
            IGenericRepository<Academicterm> academicTermRepository,
            IActivityLogService activityLogService)
        {
            _academicTermRepository = academicTermRepository;
            _activityLogService = activityLogService;
        }

        // Get all Academic terms
        public async Task<IEnumerable<GetAllAcademicTermResponse>> GetAll()
        {
            var academicTerms = await _academicTermRepository.GetAllAsyncOrderBy(t => t.StartDate, false);

            return academicTerms.Select(t => new GetAllAcademicTermResponse
            {
                TermId = t.TermId,
                EndDate = t.EndDate,
                StartDate = t.StartDate,
                TermName = t.TermName,
                Status = t.Status
            });
        }

        // Get all Academic terms with pagination
        public async Task<IEnumerable<GetAllAcademicTermWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var academicTerms = await _academicTermRepository.GetAllAsyncOrderBy(t => t.StartDate, false);

            return academicTerms
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new GetAllAcademicTermWithPaginationResponse
                {
                    TermId = t.TermId,
                    EndDate = t.EndDate,
                    StartDate = t.StartDate,
                    TermName = t.TermName,
                    Status = t.Status
                });
        }

        // Get Academic term by Id
        public async Task<GetAcademicTermByIdResponse> GetByIdAsync(Guid TermId)
        {
            var academicTerm = await _academicTermRepository.GetByIdAsync(TermId)
                ?? throw new NotFoundException("Academic term not found.");

            return new GetAcademicTermByIdResponse
            {
                TermId = academicTerm.TermId,
                EndDate = academicTerm.EndDate,
                StartDate = academicTerm.StartDate,
                TermName = academicTerm.TermName,
                Status = academicTerm.Status
            };
        }

        // Search Academic terms by name or status
        public async Task<List<SearchAcademicTermResponse>> SearchAsync(SearchAcademicTermRequest request)
        {
            var academicTerms = await _academicTermRepository.SearchAsyncOrderBy(t =>
                (string.IsNullOrWhiteSpace(request.TermName) || t.TermName.Contains(request.TermName)) &&
                (string.IsNullOrWhiteSpace(request.Status) || t.Status == request.Status),
                t => t.StartDate, false);

            return academicTerms.Select(t => new SearchAcademicTermResponse
            {
                TermId = t.TermId,
                EndDate = t.EndDate,
                StartDate = t.StartDate,
                TermName = t.TermName,
                Status = t.Status
            }).ToList();
        }

        // Create Academic term
        public async Task<CreateAcademicTermResponse> CreateAsync(CreateAcademicTermRequest request)
        {
            if (request.StartDate == default || request.EndDate == default)
                throw new ValidationException("Start date and end date are required.");
            if (request.EndDate <= request.StartDate)
                throw new ValidationException("End date must be after start date.");

            // Only one term can be current: the previous current term moves to past
            if (request.Status == "current")
                await MoveCurrentTermToPastAsync(null);

            var academicTerm = new Academicterm
            {
                TermId = Guid.NewGuid(),
                EndDate = request.EndDate,
                StartDate = request.StartDate,
                TermName = request.TermName.Trim(),
                Status = request.Status.Trim()
            };
            await _academicTermRepository.CreateAsync(academicTerm);

            await _activityLogService.LogCurrentAccountAsync("termCreated", $"Created term {academicTerm.TermName} ({academicTerm.Status})");

            return new CreateAcademicTermResponse
            {
                TermId = academicTerm.TermId,
                EndDate = academicTerm.EndDate,
                StartDate = academicTerm.StartDate,
                TermName = academicTerm.TermName,
                Status = academicTerm.Status
            };
        }

        // Update Academic term
        public async Task<UpdateAcademicTermResponse> UpdateAsync(Guid TermId, UpdateAcademicTermRequest request)
        {
            var academicTerm = await _academicTermRepository.GetByIdAsync(TermId)
                ?? throw new NotFoundException("Academic term not found.");

            if (request.StartDate == default || request.EndDate == default)
                throw new ValidationException("Start date and end date are required.");
            if (request.EndDate <= request.StartDate)
                throw new ValidationException("End date must be after start date.");

            // Only one term can be current: the previous current term moves to past
            if (request.Status == "current" && academicTerm.Status != "current")
                await MoveCurrentTermToPastAsync(TermId);

            academicTerm.EndDate = request.EndDate;
            academicTerm.StartDate = request.StartDate;
            academicTerm.TermName = request.TermName.Trim();
            academicTerm.Status = request.Status.Trim();
            await _academicTermRepository.UpdateAsync(academicTerm);

            await _activityLogService.LogCurrentAccountAsync("termUpdated", $"Updated term {academicTerm.TermName} ({academicTerm.Status})");

            return new UpdateAcademicTermResponse
            {
                TermId = academicTerm.TermId,
                EndDate = academicTerm.EndDate,
                StartDate = academicTerm.StartDate,
                TermName = academicTerm.TermName,
                Status = academicTerm.Status
            };
        }

        // Helper: moves the current term (other than the given one) to past, so only one term is current at a time
        private async Task MoveCurrentTermToPastAsync(Guid? exceptTermId)
        {
            var currentTerms = await _academicTermRepository.SearchAsync(t => t.Status == "current" && t.TermId != exceptTermId);
            foreach (var currentTerm in currentTerms)
            {
                currentTerm.Status = "past";
                await _academicTermRepository.UpdateAsync(currentTerm);
            }
        }
    }
}
