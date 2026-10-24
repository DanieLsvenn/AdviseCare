using AdviseCare.Application.Contracts.Requests.ConsentPurpose;
using AdviseCare.Application.Contracts.Responses.ConsentPurpose;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class ConsentPurposeService : IConsentPurposeService
    {
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Dataattribute> _dataAttributeRepository;
        private readonly IActivityLogService _activityLogService;

        public ConsentPurposeService(
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Dataattribute> dataAttributeRepository,
            IActivityLogService activityLogService)
        {
            _consentPurposeRepository = consentPurposeRepository;
            _dataAttributeRepository = dataAttributeRepository;
            _activityLogService = activityLogService;
        }

        // Get all Consent purposes
        public async Task<IEnumerable<GetAllConsentPurposeResponse>> GetAll()
        {
            var purposes = await _consentPurposeRepository.GetAllAsyncIncludeOrderBy(p => p.PurposeName, true, p => p.CreatedByNavigation);

            return purposes.Select(p => new GetAllConsentPurposeResponse
            {
                ConsentPurposeId = p.ConsentPurposeId,
                CreatedBy = p.CreatedBy,
                CreatorFullName = p.CreatedByNavigation.FullName,
                Description = p.Description ?? string.Empty,
                PurposeName = p.PurposeName,
                Status = p.Status
            });
        }

        // Get all Consent purposes with pagination
        public async Task<IEnumerable<GetAllConsentPurposeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var purposes = await _consentPurposeRepository.GetAllAsyncIncludeOrderBy(p => p.PurposeName, true, p => p.CreatedByNavigation);

            return purposes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetAllConsentPurposeWithPaginationResponse
                {
                    ConsentPurposeId = p.ConsentPurposeId,
                    CreatedBy = p.CreatedBy,
                    CreatorFullName = p.CreatedByNavigation.FullName,
                    Description = p.Description ?? string.Empty,
                    PurposeName = p.PurposeName,
                    Status = p.Status
                });
        }

        // Get Consent purpose by Id, with the data attributes it covers
        public async Task<GetConsentPurposeByIdResponse> GetByIdAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.CreatedByNavigation, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");

            return new GetConsentPurposeByIdResponse
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = purpose.CreatedBy,
                CreatorFullName = purpose.CreatedByNavigation.FullName,
                Description = purpose.Description ?? string.Empty,
                PurposeName = purpose.PurposeName,
                DataAttributes = purpose.DataAttributes
                    .OrderBy(a => a.AttributeName)
                    .Select(a => new ConsentPurposeAttributeResponse
                    {
                        DataAttributeId = a.DataAttributeId,
                        AttributeDescription = a.AttributeDescription ?? string.Empty,
                        AttributeName = a.AttributeName,
                        Status = a.Status
                    }).ToList(),
                Status = purpose.Status
            };
        }

        // Search Consent purposes by name or status
        public async Task<List<SearchConsentPurposeResponse>> SearchAsync(SearchConsentPurposeRequest request)
        {
            var purposes = await _consentPurposeRepository.SearchAsyncIncludeOrderBy(p =>
                (string.IsNullOrWhiteSpace(request.PurposeName) || p.PurposeName.Contains(request.PurposeName)) &&
                (string.IsNullOrWhiteSpace(request.Status) || p.Status == request.Status),
                p => p.PurposeName,
                true,
                p => p.CreatedByNavigation);

            return purposes.Select(p => new SearchConsentPurposeResponse
            {
                ConsentPurposeId = p.ConsentPurposeId,
                CreatedBy = p.CreatedBy,
                CreatorFullName = p.CreatedByNavigation.FullName,
                Description = p.Description ?? string.Empty,
                PurposeName = p.PurposeName,
                Status = p.Status
            }).ToList();
        }

        // Create Consent purpose (starts inactive, activated once its attributes are attached)
        public async Task<CreateConsentPurposeResponse> CreateAsync(Guid CreatorId, CreateConsentPurposeRequest request)
        {
            var purposeName = request.PurposeName.Trim();
            if (await _consentPurposeRepository.ExistsAsync(p => p.PurposeName == purposeName))
                throw new ConflictException($"A consent purpose named '{purposeName}' already exists.");

            var purpose = new Consentpurpose
            {
                ConsentPurposeId = Guid.NewGuid(),
                CreatedBy = CreatorId,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                PurposeName = purposeName,
                Status = "inactive"
            };
            await _consentPurposeRepository.CreateAsync(purpose);

            await _activityLogService.LogCurrentAccountAsync("consentPurposeCreated", $"Created consent purpose {purpose.PurposeName}");

            var creator = await GetCreatorAsync(purpose.ConsentPurposeId);
            return new CreateConsentPurposeResponse
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = purpose.CreatedBy,
                CreatorFullName = creator,
                Description = purpose.Description ?? string.Empty,
                PurposeName = purpose.PurposeName,
                Status = purpose.Status
            };
        }

        // Update Consent purpose (name and description; a retired purpose cannot be edited)
        public async Task<UpdateConsentPurposeResponse> UpdateAsync(Guid ConsentPurposeId, UpdateConsentPurposeRequest request)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.CreatedByNavigation)
                ?? throw new NotFoundException("Consent purpose not found.");

            if (purpose.Status == "retired")
                throw new ConflictException("A retired consent purpose cannot be edited.");

            var purposeName = request.PurposeName.Trim();
            if (await _consentPurposeRepository.ExistsAsync(p => p.PurposeName == purposeName && p.ConsentPurposeId != ConsentPurposeId))
                throw new ConflictException($"A consent purpose named '{purposeName}' already exists.");

            purpose.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            purpose.PurposeName = purposeName;
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeUpdated", $"Edited consent purpose {purpose.PurposeName}");

            return new UpdateConsentPurposeResponse
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = purpose.CreatedBy,
                CreatorFullName = purpose.CreatedByNavigation.FullName,
                Description = purpose.Description ?? string.Empty,
                PurposeName = purpose.PurposeName,
                Status = purpose.Status
            };
        }

        // Activate Consent purpose (needs at least one active data attribute)
        public async Task<bool> ActivateAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");

            if (purpose.Status == "retired")
                throw new ConflictException("A retired consent purpose cannot be brought back.");
            if (purpose.Status == "active")
                throw new ConflictException("This consent purpose is already active.");
            if (!purpose.DataAttributes.Any(a => a.Status == "active"))
                throw new ValidationException("Attach at least one active data attribute before activating the purpose.");

            purpose.Status = "active";
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeUpdated", $"Activated consent purpose {purpose.PurposeName}");
            return true;
        }

        // Deactivate Consent purpose (no new consents; existing consents stay on record but give no access while inactive)
        public async Task<bool> DeactivateAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsync(ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");

            if (purpose.Status != "active")
                throw new ConflictException("Only an active consent purpose can be deactivated.");

            purpose.Status = "inactive";
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeUpdated", $"Deactivated consent purpose {purpose.PurposeName}");
            return true;
        }

        // Retire Consent purpose (one-way; it is never deleted, so historical consents keep pointing to it)
        public async Task<bool> RetireAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsync(ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");

            if (purpose.Status == "retired")
                throw new ConflictException("This consent purpose is already retired.");

            purpose.Status = "retired";
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeRetired", $"Retired consent purpose {purpose.PurposeName}");
            return true;
        }

        // Get all purpose-attribute links (PURPOSE_ATTRIBUTE)
        public async Task<IEnumerable<GetAllPurposeAttributeResponse>> GetAllPurposeAttributeAsync()
        {
            var purposes = await _consentPurposeRepository.GetAllAsyncIncludeOrderBy(p => p.PurposeName, true, p => p.DataAttributes);

            return purposes
                .SelectMany(p => p.DataAttributes.OrderBy(a => a.AttributeName).Select(a => new GetAllPurposeAttributeResponse
                {
                    ConsentPurposeId = p.ConsentPurposeId,
                    DataAttributeId = a.DataAttributeId,
                    AttributeName = a.AttributeName,
                    PurposeName = p.PurposeName,
                    AttributeStatus = a.Status,
                    PurposeStatus = p.Status
                }));
        }

        // Get all purpose-attribute links (PURPOSE_ATTRIBUTE) with pagination
        public async Task<IEnumerable<GetAllPurposeAttributeWithPaginationResponse>> GetAllPurposeAttributeWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var purposes = await _consentPurposeRepository.GetAllAsyncIncludeOrderBy(p => p.PurposeName, true, p => p.DataAttributes);

            return purposes
                .SelectMany(p => p.DataAttributes.OrderBy(a => a.AttributeName).Select(a => new GetAllPurposeAttributeWithPaginationResponse
                {
                    ConsentPurposeId = p.ConsentPurposeId,
                    DataAttributeId = a.DataAttributeId,
                    AttributeName = a.AttributeName,
                    PurposeName = p.PurposeName,
                    AttributeStatus = a.Status,
                    PurposeStatus = p.Status
                }))
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }

        // Get a purpose-attribute link (PURPOSE_ATTRIBUTE) by purpose Id and attribute Id
        public async Task<GetPurposeAttributeByIdResponse> GetPurposeAttributeByIdAsync(Guid ConsentPurposeId, Guid DataAttributeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            var attribute = purpose.DataAttributes.FirstOrDefault(a => a.DataAttributeId == DataAttributeId)
                ?? throw new NotFoundException("This data attribute is not attached to the purpose.");

            return new GetPurposeAttributeByIdResponse
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                DataAttributeId = attribute.DataAttributeId,
                AttributeName = attribute.AttributeName,
                PurposeName = purpose.PurposeName,
                AttributeStatus = attribute.Status,
                PurposeStatus = purpose.Status
            };
        }

        // Search purpose-attribute links (PURPOSE_ATTRIBUTE) by purpose or attribute
        public async Task<List<SearchPurposeAttributeResponse>> SearchPurposeAttributeAsync(SearchPurposeAttributeRequest request)
        {
            var purposes = await _consentPurposeRepository.SearchAsyncIncludeOrderBy(p =>
                request.ConsentPurposeId == null || p.ConsentPurposeId == request.ConsentPurposeId,
                p => p.PurposeName,
                true,
                p => p.DataAttributes);

            return purposes
                .SelectMany(p => p.DataAttributes
                    .Where(a => request.DataAttributeId == null || a.DataAttributeId == request.DataAttributeId)
                    .OrderBy(a => a.AttributeName)
                    .Select(a => new SearchPurposeAttributeResponse
                    {
                        ConsentPurposeId = p.ConsentPurposeId,
                        DataAttributeId = a.DataAttributeId,
                        AttributeName = a.AttributeName,
                        PurposeName = p.PurposeName,
                        AttributeStatus = a.Status,
                        PurposeStatus = p.Status
                    }))
                .ToList();
        }

        // Attach a Data attribute to a Consent purpose (PURPOSE_ATTRIBUTE)
        public async Task<CreatePurposeAttributeResponse> CreatePurposeAttributeAsync(CreatePurposeAttributeRequest request)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(request.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            var attribute = await _dataAttributeRepository.GetByIdAsync(request.DataAttributeId)
                ?? throw new NotFoundException("Data attribute not found.");

            if (purpose.Status == "retired")
                throw new ConflictException("The attributes of a retired consent purpose cannot be changed.");
            if (attribute.Status == "archived")
                throw new ConflictException("An archived data attribute cannot be attached to a purpose.");
            if (purpose.DataAttributes.Any(a => a.DataAttributeId == attribute.DataAttributeId))
                throw new ConflictException($"'{attribute.AttributeName}' is already attached to '{purpose.PurposeName}'.");

            purpose.DataAttributes.Add(attribute);
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeUpdated", $"Attached data attribute {attribute.AttributeName} to consent purpose {purpose.PurposeName}");

            return new CreatePurposeAttributeResponse
            {
                ConsentPurposeId = purpose.ConsentPurposeId,
                DataAttributeId = attribute.DataAttributeId,
                AttributeName = attribute.AttributeName,
                PurposeName = purpose.PurposeName,
                AttributeStatus = attribute.Status,
                PurposeStatus = purpose.Status
            };
        }

        // Remove a Data attribute from a Consent purpose (PURPOSE_ATTRIBUTE); consents already given stay on record but no longer give access
        public async Task<bool> DeletePurposeAttributeAsync(Guid ConsentPurposeId, Guid DataAttributeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            var attribute = purpose.DataAttributes.FirstOrDefault(a => a.DataAttributeId == DataAttributeId)
                ?? throw new NotFoundException("This data attribute is not attached to the purpose.");

            if (purpose.Status == "retired")
                throw new ConflictException("The attributes of a retired consent purpose cannot be changed.");

            purpose.DataAttributes.Remove(attribute);
            await _consentPurposeRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentPurposeUpdated", $"Removed data attribute {attribute.AttributeName} from consent purpose {purpose.PurposeName}");
            return true;
        }

        // Helper: full name of the steward who created a purpose
        private async Task<string> GetCreatorAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(ConsentPurposeId, p => p.CreatedByNavigation);
            return purpose?.CreatedByNavigation?.FullName ?? string.Empty;
        }
    }
}
