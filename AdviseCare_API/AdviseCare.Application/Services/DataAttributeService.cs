using AdviseCare.Application.Contracts.Requests.DataAttribute;
using AdviseCare.Application.Contracts.Responses.DataAttribute;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class DataAttributeService : IDataAttributeService
    {
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IActivityLogService _activityLogService;

        public DataAttributeService(
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IActivityLogService activityLogService)
        {
            _dataAttributeRepository = dataAttributeRepository;
            _activityLogService = activityLogService;
        }

        // Get all Data attributes
        public async Task<IEnumerable<GetAllDataAttributeResponse>> GetAll()
        {
            var dataAttributes = await _dataAttributeRepository.GetAllAsyncOrderBy(a => a.AttributeName, true);

            return dataAttributes.Select(a => new GetAllDataAttributeResponse
            {
                DataAttributeId = a.DataAttributeId,
                AttributeDescription = a.AttributeDescription ?? string.Empty,
                AttributeName = a.AttributeName,
                DataType = a.DataType,
                Status = a.Status
            });
        }

        // Get all Data attributes with pagination
        public async Task<IEnumerable<GetAllDataAttributeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var dataAttributes = await _dataAttributeRepository.GetAllAsyncOrderBy(a => a.AttributeName, true);

            return dataAttributes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new GetAllDataAttributeWithPaginationResponse
                {
                    DataAttributeId = a.DataAttributeId,
                    AttributeDescription = a.AttributeDescription ?? string.Empty,
                    AttributeName = a.AttributeName,
                    DataType = a.DataType,
                    Status = a.Status
                });
        }

        // Get Data attribute by Id
        public async Task<GetDataAttributeByIdResponse> GetByIdAsync(Guid DataAttributeId)
        {
            var dataAttribute = await _dataAttributeRepository.GetByIdAsync(DataAttributeId)
                ?? throw new NotFoundException("Data attribute not found.");

            return new GetDataAttributeByIdResponse
            {
                DataAttributeId = dataAttribute.DataAttributeId,
                AttributeDescription = dataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = dataAttribute.AttributeName,
                DataType = dataAttribute.DataType,
                Status = dataAttribute.Status
            };
        }

        // Search Data attributes by name, data type or status
        public async Task<List<SearchDataAttributeResponse>> SearchAsync(SearchDataAttributeRequest request)
        {
            var dataAttributes = await _dataAttributeRepository.SearchAsyncOrderBy(a =>
                (string.IsNullOrWhiteSpace(request.AttributeName) || a.AttributeName.Contains(request.AttributeName)) &&
                (string.IsNullOrWhiteSpace(request.DataType) || a.DataType == request.DataType) &&
                (string.IsNullOrWhiteSpace(request.Status) || a.Status == request.Status),
                a => a.AttributeName, true);

            return dataAttributes.Select(a => new SearchDataAttributeResponse
            {
                DataAttributeId = a.DataAttributeId,
                AttributeDescription = a.AttributeDescription ?? string.Empty,
                AttributeName = a.AttributeName,
                DataType = a.DataType,
                Status = a.Status
            }).ToList();
        }

        // Create Data attribute (active)
        public async Task<CreateDataAttributeResponse> CreateAsync(CreateDataAttributeRequest request)
        {
            var attributeName = request.AttributeName.Trim();
            if (await _dataAttributeRepository.ExistsAsync(a => a.AttributeName == attributeName))
                throw new ConflictException($"A data attribute named '{attributeName}' already exists.");

            var dataAttribute = new Dataattribute
            {
                DataAttributeId = Guid.NewGuid(),
                AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription.Trim(),
                AttributeName = attributeName,
                DataType = request.DataType,
                Status = "active"
            };
            await _dataAttributeRepository.CreateAsync(dataAttribute);

            await _activityLogService.LogCurrentAccountAsync("dataAttributeCreated", $"Created data attribute {dataAttribute.AttributeName} ({dataAttribute.DataType})");

            return new CreateDataAttributeResponse
            {
                DataAttributeId = dataAttribute.DataAttributeId,
                AttributeDescription = dataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = dataAttribute.AttributeName,
                DataType = dataAttribute.DataType,
                Status = dataAttribute.Status
            };
        }

        // Update Data attribute (only the description; the name and data type stay as created)
        public async Task<UpdateDataAttributeResponse> UpdateAsync(Guid DataAttributeId, UpdateDataAttributeRequest request)
        {
            var dataAttribute = await _dataAttributeRepository.GetByIdAsync(DataAttributeId)
                ?? throw new NotFoundException("Data attribute not found.");

            if (dataAttribute.Status == "archived")
                throw new ConflictException("An archived data attribute cannot be edited.");

            dataAttribute.AttributeDescription = string.IsNullOrWhiteSpace(request.AttributeDescription) ? null : request.AttributeDescription.Trim();
            await _dataAttributeRepository.UpdateAsync(dataAttribute);

            await _activityLogService.LogCurrentAccountAsync("dataAttributeUpdated", $"Updated the description of data attribute {dataAttribute.AttributeName}");

            return new UpdateDataAttributeResponse
            {
                DataAttributeId = dataAttribute.DataAttributeId,
                AttributeDescription = dataAttribute.AttributeDescription ?? string.Empty,
                AttributeName = dataAttribute.AttributeName,
                DataType = dataAttribute.DataType,
                Status = dataAttribute.Status
            };
        }

        // Archive Data attribute; one-way, existing values and consents are kept
        public async Task<bool> ArchiveAsync(Guid DataAttributeId)
        {
            var dataAttribute = await _dataAttributeRepository.GetByIdAsync(DataAttributeId)
                ?? throw new NotFoundException("Data attribute not found.");

            if (dataAttribute.Status == "archived")
                throw new ConflictException("This data attribute is already archived.");

            dataAttribute.Status = "archived";
            await _dataAttributeRepository.UpdateAsync(dataAttribute);

            await _activityLogService.LogCurrentAccountAsync("dataAttributeArchived", $"Archived data attribute {dataAttribute.AttributeName}");
            return true;
        }
    }
}
