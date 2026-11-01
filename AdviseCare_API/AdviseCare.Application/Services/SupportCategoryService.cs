using AdviseCare.Application.Contracts.Requests.SupportCategory;
using AdviseCare.Application.Contracts.Responses.SupportCategory;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D1: support categories (Admin) and the attributes a student may share with a request (D2.2)
    public class SupportCategoryService(
        IGenericRepository<Supportcategory> categoryRepository,
        IGenericRepository<Consentpurpose> purposeRepository,
        IGenericRepository<Consent> consentRepository,
        IGenericRepository<Case> caseRepository,
        IActivityLogService activityLogService) : ISupportCategoryService
    {
        private static readonly string[] CategoryStatuses = { CaseRules.Active, CaseRules.Archived };

        private readonly IGenericRepository<Supportcategory> _categoryRepository = categoryRepository;
        private readonly IGenericRepository<Consentpurpose> _purposeRepository = purposeRepository;
        private readonly IGenericRepository<Consent> _consentRepository = consentRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IActivityLogService _activityLogService = activityLogService;

        public async Task<List<SupportCategoryResponse>> GetAllAsync(int? page, int? pageSize)
        {
            var categories = await _categoryRepository.GetAllAsyncIncludeOrderBy(
                c => c.CategoryName, true, c => c.ConsentPurpose, c => c.CreatedByNavigation);
            IEnumerable<Supportcategory> result = categories.OrderBy(c => c.SupportType).ThenBy(c => c.CategoryName);

            if (page.HasValue || pageSize.HasValue)
            {
                if (page is null or < 1 || pageSize is null or < 1)
                    throw new ValidationException("Page and page size must both be given and be at least 1.");
                result = result.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return result.Select(ToResponse).ToList();
        }

        public async Task<List<SupportCategoryResponse>> SearchAsync(SearchSupportCategoryRequest request)
        {
            var supportType = CaseWorkflowHelper.OptionalFilter(request.SupportType, CaseRules.SupportTypes, "Support type");
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, CategoryStatuses, "Status");
            var name = request.CategoryName?.Trim() ?? string.Empty;

            var categories = await _categoryRepository.SearchAsyncIncludeOrderBy(
                c => (supportType == null || c.SupportType == supportType) &&
                     (status == null || c.Status == status) &&
                     (!request.ConsentPurposeId.HasValue || c.ConsentPurposeId == request.ConsentPurposeId.Value) &&
                     (name == string.Empty || c.CategoryName.Contains(name)),
                c => c.CategoryName,
                true,
                c => c.ConsentPurpose,
                c => c.CreatedByNavigation);

            return categories.Select(ToResponse).ToList();
        }

        public async Task<SupportCategoryResponse> GetByIdAsync(Guid supportCategoryId)
        {
            var category = await _categoryRepository.GetByIdAsyncInclude(
                supportCategoryId, c => c.ConsentPurpose, c => c.CreatedByNavigation)
                ?? throw new NotFoundException("Support category not found.");
            return ToResponse(category);
        }

        public async Task<ShareableAttributesResponse> GetShareableAttributesAsync(Guid supportCategoryId, Guid accountId, bool isStudent)
        {
            var category = await _categoryRepository.GetByIdAsync(supportCategoryId)
                ?? throw new NotFoundException("Support category not found.");
            var purpose = await _purposeRepository.GetByIdAsyncInclude(category.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("The consent purpose of this category was not found.");

            var canShare = purpose.Status == CaseRules.Active;
            var attributes = canShare
                ? purpose.DataAttributes.Where(a => a.Status == CaseRules.Active).OrderBy(a => a.AttributeName).ToList()
                : new List<Dataattribute>();

            var sharedIds = new HashSet<Guid>();
            if (isStudent && attributes.Count > 0)
            {
                var now = DateTime.UtcNow;
                var consents = await _consentRepository.SearchAsync(c =>
                    c.StudentId == accountId &&
                    c.ConsentPurposeId == purpose.ConsentPurposeId &&
                    c.Status == "granted" &&
                    (c.ExpiresAt == null || c.ExpiresAt > now));
                sharedIds = consents.Select(c => c.DataAttributeId).ToHashSet();
            }

            return new ShareableAttributesResponse
            {
                SupportCategoryId = category.SupportCategoryId,
                CategoryName = category.CategoryName,
                ConsentPurposeId = purpose.ConsentPurposeId,
                PurposeName = purpose.PurposeName,
                CanShare = canShare,
                Attributes = attributes.Select(a => new ShareableAttributeItem
                {
                    DataAttributeId = a.DataAttributeId,
                    AttributeName = a.AttributeName,
                    AttributeDescription = a.AttributeDescription ?? string.Empty,
                    AlreadyShared = sharedIds.Contains(a.DataAttributeId)
                }).ToList()
            };
        }

        public async Task<SupportCategoryResponse> CreateAsync(Guid createdBy, CreateSupportCategoryRequest request)
        {
            var (name, description, supportType, purpose) = await ValidateAsync(request, null);

            var category = new Supportcategory
            {
                SupportCategoryId = Guid.NewGuid(),
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = createdBy,
                CategoryName = name,
                Description = description,
                SupportType = supportType,
                Status = CaseRules.Active
            };
            await _categoryRepository.CreateAsync(category);
            await _activityLogService.LogAsync(createdBy, "supportCategoryCreated",
                $"Created support category {name} ({supportType}) governed by {purpose.PurposeName}");

            return await GetByIdAsync(category.SupportCategoryId);
        }

        public async Task<SupportCategoryResponse> UpdateAsync(Guid supportCategoryId, UpdateSupportCategoryRequest request)
        {
            var category = await _categoryRepository.GetByIdAsync(supportCategoryId)
                ?? throw new NotFoundException("Support category not found.");

            var status = request.Status?.Trim() ?? string.Empty;
            if (!CategoryStatuses.Contains(status))
                throw new ValidationException("Status must be active or archived.");

            var (name, description, supportType, purpose) = await ValidateAsync(request, supportCategoryId);

            // A category's type decides whether a subject is allowed and which advisor role handles its cases,
            // so it cannot change once cases use it (archive it and create a new one instead)
            if (supportType != category.SupportType &&
                await _caseRepository.ExistsAsync(c => c.SupportCategoryId == supportCategoryId))
                throw new ConflictException("The support type cannot change because cases already use this category. Archive it and create a new category instead.");

            var archived = status == CaseRules.Archived && category.Status != CaseRules.Archived;
            category.ConsentPurposeId = purpose.ConsentPurposeId;
            category.CategoryName = name;
            category.Description = description;
            category.SupportType = supportType;
            category.Status = status;
            await _categoryRepository.UpdateAsync(category);

            await _activityLogService.LogCurrentAccountAsync("supportCategoryUpdated",
                archived ? $"Archived support category {name}" : $"Updated support category {name} ({supportType}, {status}) governed by {purpose.PurposeName}");

            return await GetByIdAsync(supportCategoryId);
        }

        private async Task<(string Name, string? Description, string SupportType, Consentpurpose Purpose)> ValidateAsync(
            CreateSupportCategoryRequest request, Guid? currentId)
        {
            var name = CaseWorkflowHelper.RequireText(request.CategoryName, "Category name is required.");
            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            var supportType = request.SupportType?.Trim() ?? string.Empty;
            if (!CaseRules.SupportTypes.Contains(supportType))
                throw new ValidationException("Support type must be academic or wellbeing.");

            var purpose = await _purposeRepository.GetByIdAsync(request.ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");
            if (purpose.Status == "retired")
                throw new ValidationException($"'{purpose.PurposeName}' is retired and cannot govern a support category.");

            // Names are unique within a support type (the student picks the category by name)
            if (await _categoryRepository.ExistsAsync(c =>
                    c.SupportType == supportType &&
                    c.CategoryName == name &&
                    (!currentId.HasValue || c.SupportCategoryId != currentId.Value)))
                throw new ConflictException($"A {supportType} support category named '{name}' already exists.");

            return (name, description, supportType, purpose);
        }

        private static SupportCategoryResponse ToResponse(Supportcategory category) => new()
        {
            SupportCategoryId = category.SupportCategoryId,
            ConsentPurposeId = category.ConsentPurposeId,
            PurposeName = category.ConsentPurpose?.PurposeName ?? string.Empty,
            PurposeStatus = category.ConsentPurpose?.Status ?? string.Empty,
            CreatedBy = category.CreatedBy,
            CreatedByName = category.CreatedByNavigation?.FullName ?? string.Empty,
            CategoryName = category.CategoryName,
            Description = category.Description ?? string.Empty,
            SupportType = category.SupportType,
            Status = category.Status
        };
    }
}
