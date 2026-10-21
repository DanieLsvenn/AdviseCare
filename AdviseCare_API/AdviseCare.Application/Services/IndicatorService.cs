using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Contracts.Responses.Indicator;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AdviseCare.Application.Services
{
    public class IndicatorService : IIndicatorService
    {
        private readonly IGenericRepository<Indicator> _indicatorRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;

        public IndicatorService(
            IGenericRepository<Indicator> indicatorRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository)
        {
            _indicatorRepository = indicatorRepository;
            _dataAttributeRepository = dataAttributeRepository;
        }

        // Get every active (non-archived) Indicator together with its DataAttribute
        public async Task<IEnumerable<GetAllIndicatorResponse>> GetAllAsync()
        {
            var indicators = await _indicatorRepository.SearchAsyncInclude(i => i.Status != "archived", i => i.DataAttribute);

            return indicators.Select(i => new GetAllIndicatorResponse
            {
                IndicatorId = i.IndicatorId,
                DataAttributeId = i.DataAttributeId,
                IndicatorName = i.IndicatorName,
                Description = i.Description ?? string.Empty,
                DataType = i.DataType,
                Status = i.Status,
                AttributeName = i.DataAttribute.AttributeName,
                AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty
            });
        }

        // Get a page of active (non-archived) Indicators together with their DataAttribute, ordered by name
        public async Task<IEnumerable<GetAllIndicatorWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            var indicators = await _indicatorRepository.GetQueryable()
                .Include(i => i.DataAttribute)
                .Where(i => i.Status != "archived")
                .OrderBy(i => i.IndicatorName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return indicators.Select(i => new GetAllIndicatorWithPaginationResponse
            {
                IndicatorId = i.IndicatorId,
                DataAttributeId = i.DataAttributeId,
                IndicatorName = i.IndicatorName,
                Description = i.Description ?? string.Empty,
                DataType = i.DataType,
                Status = i.Status,
                AttributeName = i.DataAttribute.AttributeName,
                AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty
            });
        }

        // Get one Indicator (active or archived) by id together with its DataAttribute
        public async Task<GetIndicatorByIdResponse> GetByIdAsync(Guid id)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(id, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            return new GetIndicatorByIdResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = indicator.DataAttributeId,
                IndicatorName = indicator.IndicatorName,
                Description = indicator.Description ?? string.Empty,
                DataType = indicator.DataType,
                Status = indicator.Status,
                AttributeName = indicator.DataAttribute.AttributeName,
                AttributeDescription = indicator.DataAttribute.AttributeDescription ?? string.Empty
            };
        }

        // Search Indicators by name/attribute name, excluding archived ones unless IncludeArchived is set
        public async Task<List<SearchIndicatorResponse>> SearchAsync(SearchIndicatorRequest request)
        {
            var query = request.SearchQuery.Trim();
            var indicators = await _indicatorRepository.SearchAsyncInclude(
                i => (request.IncludeArchived || i.Status != "archived")
                    && (string.IsNullOrEmpty(query) || i.IndicatorName.Contains(query) || i.DataAttribute.AttributeName.Contains(query)),
                i => i.DataAttribute);

            return indicators.Select(i => new SearchIndicatorResponse
            {
                IndicatorId = i.IndicatorId,
                DataAttributeId = i.DataAttributeId,
                IndicatorName = i.IndicatorName,
                Description = i.Description ?? string.Empty,
                DataType = i.DataType,
                Status = i.Status,
                AttributeName = i.DataAttribute.AttributeName,
                AttributeDescription = i.DataAttribute.AttributeDescription ?? string.Empty
            }).ToList();
        }

        // Create an Indicator together with its DataAttribute in one transaction (1:1, created as a pair)
        public async Task<CreateIndicatorResponse> CreateAsync(CreateIndicatorRequest request)
        {
            if (await _indicatorRepository.ExistsAsync(i => i.IndicatorName == request.IndicatorName))
                throw new ConflictException($"An indicator named '{request.IndicatorName}' already exists.");
            if (await _dataAttributeRepository.ExistsAsync(a => a.AttributeName == request.AttributeName))
                throw new ConflictException($"A data attribute named '{request.AttributeName}' already exists.");

            var dataAttribute = new Dataattribute
            {
                DataAttributeId = Guid.NewGuid(),
                AttributeName = request.AttributeName,
                AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription,
                Status = "active"
            };
            var indicator = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                DataAttributeId = dataAttribute.DataAttributeId,
                IndicatorName = request.IndicatorName,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
                DataType = request.DataType,
                Status = "active"
            };

            _dataAttributeRepository.PrepareCreate(dataAttribute);
            _indicatorRepository.PrepareCreate(indicator);
            await _indicatorRepository.SaveAsync();

            return new CreateIndicatorResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = dataAttribute.DataAttributeId,
                IndicatorName = indicator.IndicatorName,
                Description = indicator.Description ?? string.Empty,
                DataType = indicator.DataType,
                Status = indicator.Status,
                AttributeName = dataAttribute.AttributeName,
                AttributeDescription = dataAttribute.AttributeDescription ?? string.Empty
            };
        }

        // Edit the student-facing descriptions of an Indicator/DataAttribute pair (name and data type are not editable)
        public async Task<UpdateIndicatorResponse> UpdateAsync(Guid id, UpdateIndicatorRequest request)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(id, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            if (indicator.Status == "archived")
                throw new ConflictException("Archived indicators cannot be edited.");

            indicator.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
            indicator.DataAttribute.AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription;

            _indicatorRepository.PrepareUpdate(indicator);
            _dataAttributeRepository.PrepareUpdate(indicator.DataAttribute);
            await _indicatorRepository.SaveAsync();

            return new UpdateIndicatorResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = indicator.DataAttributeId,
                IndicatorName = indicator.IndicatorName,
                Description = indicator.Description ?? string.Empty,
                DataType = indicator.DataType,
                Status = indicator.Status,
                AttributeName = indicator.DataAttribute.AttributeName,
                AttributeDescription = indicator.DataAttribute.AttributeDescription ?? string.Empty
            };
        }

        // Archive an Indicator/DataAttribute pair that is no longer used (one-way: archived records cannot be brought back)
        public async Task<ArchiveIndicatorResponse> ArchiveAsync(Guid id)
        {
            var indicator = await _indicatorRepository.GetByIdAsyncInclude(id, i => i.DataAttribute)
                ?? throw new NotFoundException("Indicator not found.");

            if (indicator.Status == "archived")
                throw new ConflictException("This indicator is already archived.");

            indicator.Status = "archived";
            indicator.DataAttribute.Status = "archived";

            _indicatorRepository.PrepareUpdate(indicator);
            _dataAttributeRepository.PrepareUpdate(indicator.DataAttribute);
            await _indicatorRepository.SaveAsync();

            return new ArchiveIndicatorResponse
            {
                IndicatorId = indicator.IndicatorId,
                DataAttributeId = indicator.DataAttributeId,
                Status = indicator.Status,
                Message = "Indicator and its data attribute have been archived."
            };
        }
    }
}
