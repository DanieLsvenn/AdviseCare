using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Contracts.Responses.Indicator;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class IndicatorService : IIndicatorService
    {
        private readonly IGenericRepository<Indicator> _indicatorRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IActivityLogService _activityLogService;

        public IndicatorService(
            IGenericRepository<Indicator> indicatorRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IActivityLogService activityLogService)
        {
            _indicatorRepository = indicatorRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _activityLogService = activityLogService;
        }

        // Get all Indicators
        public async Task<IEnumerable<GetAllIndicatorResponse>> GetAll()
        {
            var indicators = await _indicatorRepository.GetAllAsyncIncludeOrderBy(i => i.IndicatorName, true, i => i.DataAttribute);

            return indicators.Select(i => new GetAllIndicatorResponse
            {
                IndicatorId = i.IndicatorId,
                DataAttributeId = i.DataAttributeId,
                AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = i.DataAttribute.AttributeName,
                Description = i.Description ?? string.Empty,
                IndicatorName = i.IndicatorName,
                DataType = i.DataType,
                Status = i.Status
            });
        }

        // Get all Indicators with pagination
        public async Task<IEnumerable<GetAllIndicatorWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var indicators = await _indicatorRepository.GetAllAsyncIncludeOrderBy(i => i.IndicatorName, true, i => i.DataAttribute);

            return indicators
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new GetAllIndicatorWithPaginationResponse
                {
                    IndicatorId = i.IndicatorId,
                    DataAttributeId = i.DataAttributeId,
                    AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty,
                    AttributeName = i.DataAttribute.AttributeName,
                    Description = i.Description ?? string.Empty,
                    IndicatorName = i.IndicatorName,
                    DataType = i.DataType,
                    Status = i.Status
                });
        }

        // Get Indicator by Id
        public async Task<GetIndicatorByIdResponse> GetByIdAsync(Guid IndicatorId)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(IndicatorId, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            return new GetIndicatorByIdResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = indicator.DataAttributeId,
                AttributeDescription = indicator.DataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = indicator.DataAttribute.AttributeName,
                Description = indicator.Description ?? string.Empty,
                IndicatorName = indicator.IndicatorName,
                DataType = indicator.DataType,
                Status = indicator.Status
            };
        }

        // Search Indicators by attribute name, indicator name, data type or status
        public async Task<List<SearchIndicatorResponse>> SearchAsync(SearchIndicatorRequest request)
        {
            var indicators = await _indicatorRepository.SearchAsyncIncludeOrderBy(i =>
                (string.IsNullOrWhiteSpace(request.AttributeName) || i.DataAttribute.AttributeName.Contains(request.AttributeName)) &&
                (string.IsNullOrWhiteSpace(request.IndicatorName) || i.IndicatorName.Contains(request.IndicatorName)) &&
                (string.IsNullOrWhiteSpace(request.DataType) || i.DataType == request.DataType) &&
                (string.IsNullOrWhiteSpace(request.Status) || i.Status == request.Status),
                i => i.IndicatorName,
                true,
                i => i.DataAttribute);

            return indicators.Select(i => new SearchIndicatorResponse
            {
                IndicatorId = i.IndicatorId,
                DataAttributeId = i.DataAttributeId,
                AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = i.DataAttribute.AttributeName,
                Description = i.Description ?? string.Empty,
                IndicatorName = i.IndicatorName,
                DataType = i.DataType,
                Status = i.Status
            }).ToList();
        }

        // Create Indicator (together with its Data attribute, 1:1, saved in one transaction)
        public async Task<CreateIndicatorResponse> CreateAsync(CreateIndicatorRequest request)
        {
            var indicatorName = request.IndicatorName.Trim();
            var attributeName = request.AttributeName.Trim();

            if (await _indicatorRepository.ExistsAsync(i => i.IndicatorName == indicatorName))
                throw new ConflictException($"An indicator named '{indicatorName}' already exists.");
            if (await _dataAttributeRepository.ExistsAsync(a => a.AttributeName == attributeName))
                throw new ConflictException($"A data attribute named '{attributeName}' already exists.");

            var dataAttribute = new Dataattribute
            {
                DataAttributeId = Guid.NewGuid(),
                AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription.Trim(),
                AttributeName = attributeName,
                Status = "active"
            };
            var indicator = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                DataAttributeId = dataAttribute.DataAttributeId,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IndicatorName = indicatorName,
                DataType = request.DataType,
                Status = "active"
            };

            _dataAttributeRepository.PrepareCreate(dataAttribute);
            _indicatorRepository.PrepareCreate(indicator);
            await _indicatorRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("indicatorCreated", $"Created indicator {indicator.IndicatorName} with data attribute {dataAttribute.AttributeName}");

            return new CreateIndicatorResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = dataAttribute.DataAttributeId,
                AttributeDescription = dataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = dataAttribute.AttributeName,
                Description = indicator.Description ?? string.Empty,
                IndicatorName = indicator.IndicatorName,
                DataType = indicator.DataType,
                Status = indicator.Status
            };
        }

        // Update Indicator (only the descriptions shown to students; names and data type stay as created)
        public async Task<UpdateIndicatorResponse> UpdateAsync(Guid IndicatorId, UpdateIndicatorRequest request)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(IndicatorId, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            if (indicator.Status == "archived")
                throw new ConflictException("An archived indicator cannot be edited.");

            indicator.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            indicator.DataAttribute.AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription.Trim();
            await _indicatorRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("indicatorUpdated", $"Updated the descriptions of indicator {indicator.IndicatorName}");

            return new UpdateIndicatorResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = indicator.DataAttributeId,
                AttributeDescription = indicator.DataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = indicator.DataAttribute.AttributeName,
                Description = indicator.Description ?? string.Empty,
                IndicatorName = indicator.IndicatorName,
                DataType = indicator.DataType,
                Status = indicator.Status
            };
        }

        // Archive Indicator (and its Data attribute); one-way, existing values and consents are kept
        public async Task<bool> ArchiveAsync(Guid IndicatorId)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(IndicatorId, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            if (indicator.Status == "archived")
                throw new ConflictException("This indicator is already archived.");

            indicator.Status = "archived";
            indicator.DataAttribute.Status = "archived";
            await _indicatorRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("indicatorArchived", $"Archived indicator {indicator.IndicatorName} and data attribute {indicator.DataAttribute.AttributeName}");
            return true;
        }
    }
}
