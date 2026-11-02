using AdviseCare.Application.Contracts.Requests.SupportCategory;
using AdviseCare.Application.Contracts.Responses.SupportCategory;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Support categories: what a student picks when asking for help (academic or wellbeing), each governed by one consent purpose
    // that decides which data attributes the student may share with the request
    public class SupportCategoryService : ISupportCategoryService
    {
        private readonly IGenericRepository<Supportcategory> _supportCategoryRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IActivityLogService _activityLogService;

        public SupportCategoryService(
            IGenericRepository<Supportcategory> supportCategoryRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Consent> consentRepository,
            IGenericRepository<Case> caseRepository,
            IActivityLogService activityLogService)
        {
            _supportCategoryRepository = supportCategoryRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _consentRepository = consentRepository;
            _caseRepository = caseRepository;
            _activityLogService = activityLogService;
        }

        // Get all Support categories, ordered by support type then name
        public async Task<IEnumerable<GetAllSupportCategoryResponse>> GetAll()
        {
            var categories = await GetCategoriesAsync();

            return categories.Select(c => new GetAllSupportCategoryResponse
            {
                SupportCategoryId = c.SupportCategoryId,
                ConsentPurposeId = c.ConsentPurposeId,
                CreatedBy = c.CreatedBy,
                CategoryName = c.CategoryName,
                CreatedByFullName = c.CreatedByNavigation?.FullName ?? string.Empty,
                Description = c.Description ?? string.Empty,
                PurposeName = c.ConsentPurpose?.PurposeName ?? string.Empty,
                PurposeStatus = c.ConsentPurpose?.Status ?? string.Empty,
                SupportType = c.SupportType,
                Status = c.Status
            });
        }

        // Get all Support categories with pagination
        public async Task<IEnumerable<GetAllSupportCategoryWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var categories = await GetCategoriesAsync();

            return categories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new GetAllSupportCategoryWithPaginationResponse
                {
                    SupportCategoryId = c.SupportCategoryId,
                    ConsentPurposeId = c.ConsentPurposeId,
                    CreatedBy = c.CreatedBy,
                    CategoryName = c.CategoryName,
                    CreatedByFullName = c.CreatedByNavigation?.FullName ?? string.Empty,
                    Description = c.Description ?? string.Empty,
                    PurposeName = c.ConsentPurpose?.PurposeName ?? string.Empty,
                    PurposeStatus = c.ConsentPurpose?.Status ?? string.Empty,
                    SupportType = c.SupportType,
                    Status = c.Status
                });
        }

        // Get Support category by Id, with its governing consent purpose
        public async Task<GetSupportCategoryByIdResponse> GetByIdAsync(Guid SupportCategoryId)
        {
            var category = await _supportCategoryRepository.GetByIdAsyncInclude(SupportCategoryId, c => c.ConsentPurpose, c => c.CreatedByNavigation)
                ?? throw new NotFoundException("Support category not found.");

            return new GetSupportCategoryByIdResponse
            {
                SupportCategoryId = category.SupportCategoryId,
                ConsentPurposeId = category.ConsentPurposeId,
                CreatedBy = category.CreatedBy,
                CategoryName = category.CategoryName,
                CreatedByFullName = category.CreatedByNavigation?.FullName ?? string.Empty,
                Description = category.Description ?? string.Empty,
                PurposeName = category.ConsentPurpose?.PurposeName ?? string.Empty,
                PurposeStatus = category.ConsentPurpose?.Status ?? string.Empty,
                SupportType = category.SupportType,
                Status = category.Status
            };
        }

        // Search Support categories by name (contains), governing purpose, support type and status
        public async Task<List<SearchSupportCategoryResponse>> SearchAsync(SearchSupportCategoryRequest request)
        {
            var categories = (await _supportCategoryRepository.SearchAsyncIncludeOrderBy(c =>
                    (request.ConsentPurposeId == null || c.ConsentPurposeId == request.ConsentPurposeId) &&
                    (string.IsNullOrWhiteSpace(request.CategoryName) || c.CategoryName.Contains(request.CategoryName)) &&
                    (string.IsNullOrWhiteSpace(request.SupportType) || c.SupportType == request.SupportType) &&
                    (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status),
                    c => c.CategoryName, true, c => c.ConsentPurpose, c => c.CreatedByNavigation))
                .OrderBy(c => c.SupportType)
                .ThenBy(c => c.CategoryName);

            return categories.Select(c => new SearchSupportCategoryResponse
            {
                SupportCategoryId = c.SupportCategoryId,
                ConsentPurposeId = c.ConsentPurposeId,
                CreatedBy = c.CreatedBy,
                CategoryName = c.CategoryName,
                CreatedByFullName = c.CreatedByNavigation?.FullName ?? string.Empty,
                Description = c.Description ?? string.Empty,
                PurposeName = c.ConsentPurpose?.PurposeName ?? string.Empty,
                PurposeStatus = c.ConsentPurpose?.Status ?? string.Empty,
                SupportType = c.SupportType,
                Status = c.Status
            }).ToList();
        }

        // Create Support category (active), governed by a consent purpose that is not retired
        public async Task<CreateSupportCategoryResponse> CreateAsync(Guid CreatorId, CreateSupportCategoryRequest request)
        {
            var categoryName = request.CategoryName.Trim();
            var purpose = await GetGoverningPurposeAsync(request.ConsentPurposeId);
            await EnsureUniqueNameAsync(categoryName, request.SupportType, null);

            var category = new Supportcategory
            {
                SupportCategoryId = Guid.NewGuid(),
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = CreatorId,
                CategoryName = categoryName,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SupportType = request.SupportType,
                Status = "active"
            };
            await _supportCategoryRepository.CreateAsync(category);

            await _activityLogService.LogAsync(CreatorId, "supportCategoryCreated", $"Created support category {category.CategoryName} ({category.SupportType}) governed by {purpose.PurposeName}");

            var created = await _supportCategoryRepository.GetByIdAsyncInclude(category.SupportCategoryId, c => c.ConsentPurpose, c => c.CreatedByNavigation)
                ?? throw new NotFoundException("Support category not found.");

            return new CreateSupportCategoryResponse
            {
                SupportCategoryId = created.SupportCategoryId,
                ConsentPurposeId = created.ConsentPurposeId,
                CreatedBy = created.CreatedBy,
                CategoryName = created.CategoryName,
                CreatedByFullName = created.CreatedByNavigation?.FullName ?? string.Empty,
                Description = created.Description ?? string.Empty,
                PurposeName = created.ConsentPurpose?.PurposeName ?? string.Empty,
                PurposeStatus = created.ConsentPurpose?.Status ?? string.Empty,
                SupportType = created.SupportType,
                Status = created.Status
            };
        }

        // Update Support category (archiving hides it from new requests; existing cases keep it)
        public async Task<UpdateSupportCategoryResponse> UpdateAsync(Guid SupportCategoryId, UpdateSupportCategoryRequest request)
        {
            var category = await _supportCategoryRepository.GetByIdAsync(SupportCategoryId)
                ?? throw new NotFoundException("Support category not found.");

            var categoryName = request.CategoryName.Trim();
            var purpose = await GetGoverningPurposeAsync(request.ConsentPurposeId);
            await EnsureUniqueNameAsync(categoryName, request.SupportType, SupportCategoryId);

            // The support type decides which advisor role handles the category's cases, so it cannot change once cases use it
            if (request.SupportType != category.SupportType && await _caseRepository.ExistsAsync(c => c.SupportCategoryId == SupportCategoryId))
                throw new ConflictException("The support type cannot change because cases already use this category. Archive it and create a new category instead.");

            var isArchiving = request.Status == "archived" && category.Status != "archived";
            category.ConsentPurposeId = purpose.ConsentPurposeId;
            category.CategoryName = categoryName;
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            category.SupportType = request.SupportType;
            category.Status = request.Status;
            await _supportCategoryRepository.UpdateAsync(category);

            await _activityLogService.LogCurrentAccountAsync("supportCategoryUpdated", isArchiving
                ? $"Archived support category {category.CategoryName}"
                : $"Updated support category {category.CategoryName} ({category.SupportType}, {category.Status}) governed by {purpose.PurposeName}");

            var updated = await _supportCategoryRepository.GetByIdAsyncInclude(SupportCategoryId, c => c.ConsentPurpose, c => c.CreatedByNavigation)
                ?? throw new NotFoundException("Support category not found.");

            return new UpdateSupportCategoryResponse
            {
                SupportCategoryId = updated.SupportCategoryId,
                ConsentPurposeId = updated.ConsentPurposeId,
                CreatedBy = updated.CreatedBy,
                CategoryName = updated.CategoryName,
                CreatedByFullName = updated.CreatedByNavigation?.FullName ?? string.Empty,
                Description = updated.Description ?? string.Empty,
                PurposeName = updated.ConsentPurpose?.PurposeName ?? string.Empty,
                PurposeStatus = updated.ConsentPurpose?.Status ?? string.Empty,
                SupportType = updated.SupportType,
                Status = updated.Status
            };
        }

        // Get the data attributes a student may share with a request in this category: the active attributes of its consent purpose
        // (none when the purpose is not active); for a student, the attributes already granted under that purpose are marked
        public async Task<GetSupportCategoryShareableAttributesResponse> GetShareableAttributesAsync(Guid AccountId, bool IsStudent, Guid SupportCategoryId)
        {
            var category = await _supportCategoryRepository.GetByIdAsync(SupportCategoryId)
                ?? throw new NotFoundException("Support category not found.");
            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(category.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("The consent purpose of this category was not found.");

            var canShare = purpose.Status == "active";
            var attributes = canShare
                ? purpose.DataAttributes.Where(a => a.Status == "active").OrderBy(a => a.AttributeName).ToList()
                : new List<Dataattribute>();

            var sharedIds = new HashSet<Guid>();
            if (IsStudent && attributes.Count > 0)
            {
                var now = DateTime.UtcNow;
                sharedIds = (await _consentRepository.SearchAsync(c =>
                        c.StudentId == AccountId &&
                        c.ConsentPurposeId == purpose.ConsentPurposeId &&
                        c.Status == "granted" &&
                        (c.ExpiresAt == null || c.ExpiresAt > now)))
                    .Select(c => c.DataAttributeId)
                    .ToHashSet();
            }

            return new GetSupportCategoryShareableAttributesResponse
            {
                SupportCategoryId = category.SupportCategoryId,
                ConsentPurposeId = purpose.ConsentPurposeId,
                CategoryName = category.CategoryName,
                PurposeName = purpose.PurposeName,
                Attributes = attributes.Select(a => new SupportCategoryShareableAttributeResponse
                {
                    DataAttributeId = a.DataAttributeId,
                    AttributeDescription = a.AttributeDescription ?? string.Empty,
                    AttributeName = a.AttributeName,
                    AlreadyShared = sharedIds.Contains(a.DataAttributeId)
                }).ToList(),
                CanShare = canShare
            };
        }

        // Helper: every category with its purpose and creator, ordered by support type then name
        private async Task<List<Supportcategory>> GetCategoriesAsync()
        {
            var categories = await _supportCategoryRepository.GetAllAsyncIncludeOrderBy(c => c.CategoryName, true, c => c.ConsentPurpose, c => c.CreatedByNavigation);
            return categories.OrderBy(c => c.SupportType).ThenBy(c => c.CategoryName).ToList();
        }

        // Helper: the governing consent purpose must exist and must not be retired
        private async Task<Consentpurpose> GetGoverningPurposeAsync(Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsync(ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");
            if (purpose.Status == "retired")
                throw new ValidationException($"'{purpose.PurposeName}' is retired and cannot govern a support category.");
            return purpose;
        }

        // Helper: category names are unique within a support type (the student picks the category by name)
        private async Task EnsureUniqueNameAsync(string CategoryName, string SupportType, Guid? CurrentId)
        {
            if (await _supportCategoryRepository.ExistsAsync(c =>
                    c.SupportType == SupportType &&
                    c.CategoryName == CategoryName &&
                    (CurrentId == null || c.SupportCategoryId != CurrentId)))
                throw new ConflictException($"A {SupportType} support category named '{CategoryName}' already exists.");
        }
    }
}
