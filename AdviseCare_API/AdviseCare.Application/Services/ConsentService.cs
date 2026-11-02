using AdviseCare.Application.Contracts.Requests.Consent;
using AdviseCare.Application.Contracts.Responses.Consent;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    public class ConsentService : IConsentService
    {
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IGenericRepository<Consenthistory> _consentHistoryRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Attributekey> _attributeKeyRepository;
        private readonly IGenericRepository<Erasurerequest> _erasureRequestRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public ConsentService(
            IGenericRepository<Consent> consentRepository,
            IGenericRepository<Consenthistory> consentHistoryRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Attributekey> attributeKeyRepository,
            IGenericRepository<Erasurerequest> erasureRequestRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _consentRepository = consentRepository;
            _consentHistoryRepository = consentHistoryRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _attributeKeyRepository = attributeKeyRepository;
            _erasureRequestRepository = erasureRequestRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Consents
        public async Task<IEnumerable<GetAllConsentResponse>> GetAll()
        {
            var consents = await GetConsentsAsync(c => true);
            var now = DateTime.UtcNow;

            return consents.Select(c => new GetAllConsentResponse
            {
                ConsentId = c.ConsentId,
                ConsentPurposeId = c.ConsentPurposeId,
                DataAttributeId = c.DataAttributeId,
                StudentId = c.StudentId,
                AttributeName = c.DataAttribute.AttributeName,
                PurposeName = c.ConsentPurpose.PurposeName,
                StudentFullName = c.Student.FullName,
                ExpiresAt = c.ExpiresAt,
                GrantedAt = c.GrantedAt,
                WithdrawnAt = c.WithdrawnAt,
                IsExpired = IsExpired(c, now),
                Status = c.Status
            });
        }

        // Get all Consents with pagination
        public async Task<IEnumerable<GetAllConsentWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var consents = await GetConsentsAsync(c => true);
            var now = DateTime.UtcNow;

            return consents
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new GetAllConsentWithPaginationResponse
                {
                    ConsentId = c.ConsentId,
                    ConsentPurposeId = c.ConsentPurposeId,
                    DataAttributeId = c.DataAttributeId,
                    StudentId = c.StudentId,
                    AttributeName = c.DataAttribute.AttributeName,
                    PurposeName = c.ConsentPurpose.PurposeName,
                    StudentFullName = c.Student.FullName,
                    ExpiresAt = c.ExpiresAt,
                    GrantedAt = c.GrantedAt,
                    WithdrawnAt = c.WithdrawnAt,
                    IsExpired = IsExpired(c, now),
                    Status = c.Status
                });
        }

        // Get Consent by Id
        public async Task<GetConsentByIdResponse> GetByIdAsync(Guid ConsentId)
        {
            var consent = await _consentRepository.GetByIdAsyncInclude(ConsentId, c => c.ConsentPurpose, c => c.DataAttribute, c => c.Student)
                ?? throw new NotFoundException("Consent not found.");

            return new GetConsentByIdResponse
            {
                ConsentId = consent.ConsentId,
                ConsentPurposeId = consent.ConsentPurposeId,
                DataAttributeId = consent.DataAttributeId,
                StudentId = consent.StudentId,
                AttributeName = consent.DataAttribute.AttributeName,
                PurposeName = consent.ConsentPurpose.PurposeName,
                StudentFullName = consent.Student.FullName,
                ExpiresAt = consent.ExpiresAt,
                GrantedAt = consent.GrantedAt,
                WithdrawnAt = consent.WithdrawnAt,
                IsExpired = IsExpired(consent, DateTime.UtcNow),
                Status = consent.Status
            };
        }

        // Search Consents by purpose, attribute, student or status
        public async Task<List<SearchConsentResponse>> SearchAsync(SearchConsentRequest request)
        {
            var consents = await GetConsentsAsync(c =>
                (request.ConsentPurposeId == null || c.ConsentPurposeId == request.ConsentPurposeId) &&
                (request.DataAttributeId == null || c.DataAttributeId == request.DataAttributeId) &&
                (request.StudentId == null || c.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || c.Status == request.Status));
            var now = DateTime.UtcNow;

            return consents.Select(c => new SearchConsentResponse
            {
                ConsentId = c.ConsentId,
                ConsentPurposeId = c.ConsentPurposeId,
                DataAttributeId = c.DataAttributeId,
                StudentId = c.StudentId,
                AttributeName = c.DataAttribute.AttributeName,
                PurposeName = c.ConsentPurpose.PurposeName,
                StudentFullName = c.Student.FullName,
                ExpiresAt = c.ExpiresAt,
                GrantedAt = c.GrantedAt,
                WithdrawnAt = c.WithdrawnAt,
                IsExpired = IsExpired(c, now),
                Status = c.Status
            }).ToList();
        }

        // Create Consents (the student grants one or more attributes of one purpose; already granted ones are kept, expired ones are renewed)
        public async Task<List<CreateConsentResponse>> CreateAsync(Guid StudentId, CreateConsentRequest request)
        {
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == StudentId && ar.Role.RoleName == "Student"))
                throw new ForbiddenException("Only students can grant consent.");

            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(request.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            if (purpose.Status != "active")
                throw new ConflictException($"'{purpose.PurposeName}' is not active, so consent cannot be given for it.");

            var now = DateTime.UtcNow;
            if (request.ExpiresAt != null && request.ExpiresAt <= now)
                throw new ValidationException("The expiry date must be in the future.");

            var attributeIds = request.DataAttributeIds.Distinct().ToList();
            var attributes = purpose.DataAttributes.Where(a => attributeIds.Contains(a.DataAttributeId)).ToList();
            if (attributes.Count != attributeIds.Count)
                throw new ValidationException($"Some data attributes are not covered by '{purpose.PurposeName}'.");
            var archived = attributes.Where(a => a.Status != "active").Select(a => a.AttributeName).ToList();
            if (archived.Count > 0)
                throw new ConflictException($"These data attributes are archived and cannot be shared any more: {string.Join(", ", archived)}.");

            var granted = await _consentRepository.SearchAsync(c =>
                c.StudentId == StudentId &&
                c.ConsentPurposeId == purpose.ConsentPurposeId &&
                attributeIds.Contains(c.DataAttributeId) &&
                c.Status == "granted");

            var consents = new List<Consent>();
            foreach (var attribute in attributes)
            {
                var consent = granted.FirstOrDefault(c => c.DataAttributeId == attribute.DataAttributeId);
                if (consent == null)
                {
                    consent = new Consent
                    {
                        ConsentId = Guid.NewGuid(),
                        ConsentPurposeId = purpose.ConsentPurposeId,
                        DataAttributeId = attribute.DataAttributeId,
                        StudentId = StudentId,
                        ExpiresAt = request.ExpiresAt,
                        GrantedAt = now,
                        Status = "granted"
                    };
                    _consentRepository.PrepareCreate(consent);
                    _consentHistoryRepository.PrepareCreate(NewHistory(consent.ConsentId, StudentId, null, "granted", now));
                }
                else if (IsExpired(consent, now))
                {
                    // Renew an expired consent instead of adding a second granted row for the same purpose and attribute
                    consent.ExpiresAt = request.ExpiresAt;
                    consent.GrantedAt = now;
                    _consentHistoryRepository.PrepareCreate(NewHistory(consent.ConsentId, StudentId, "granted", "granted", now));
                }
                consents.Add(consent);
            }
            await _consentRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentGranted",
                $"Granted consent for {purpose.PurposeName}: {string.Join(", ", attributes.Select(a => a.AttributeName))}");

            var studentName = (await _accountRepository.GetByIdAsync(StudentId))?.FullName ?? string.Empty;
            return consents.Select(c => new CreateConsentResponse
            {
                ConsentId = c.ConsentId,
                ConsentPurposeId = c.ConsentPurposeId,
                DataAttributeId = c.DataAttributeId,
                StudentId = c.StudentId,
                AttributeName = attributes.First(a => a.DataAttributeId == c.DataAttributeId).AttributeName,
                PurposeName = purpose.PurposeName,
                StudentFullName = studentName,
                ExpiresAt = c.ExpiresAt,
                GrantedAt = c.GrantedAt,
                WithdrawnAt = c.WithdrawnAt,
                IsExpired = IsExpired(c, now),
                Status = c.Status
            }).ToList();
        }

        // Update Consent (the student changes the expiry date of an own granted consent)
        public async Task<UpdateConsentResponse> UpdateAsync(Guid StudentId, Guid ConsentId, UpdateConsentRequest request)
        {
            var consent = await _consentRepository.GetByIdAsyncInclude(ConsentId, c => c.ConsentPurpose, c => c.DataAttribute, c => c.Student);
            if (consent == null || consent.StudentId != StudentId)
                throw new NotFoundException("Consent not found.");
            if (consent.Status != "granted")
                throw new ConflictException("Only a granted consent can be changed. Grant it again instead.");

            var now = DateTime.UtcNow;
            if (request.ExpiresAt != null && request.ExpiresAt <= now)
                throw new ValidationException("The expiry date must be in the future.");

            consent.ExpiresAt = request.ExpiresAt;
            await _consentRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentExpiryChanged",
                $"Set the expiry of consent {consent.ConsentPurpose.PurposeName} / {consent.DataAttribute.AttributeName} to {(consent.ExpiresAt?.ToString("yyyy-MM-dd") ?? "none")}");

            return new UpdateConsentResponse
            {
                ConsentId = consent.ConsentId,
                ConsentPurposeId = consent.ConsentPurposeId,
                DataAttributeId = consent.DataAttributeId,
                StudentId = consent.StudentId,
                AttributeName = consent.DataAttribute.AttributeName,
                PurposeName = consent.ConsentPurpose.PurposeName,
                StudentFullName = consent.Student.FullName,
                ExpiresAt = consent.ExpiresAt,
                GrantedAt = consent.GrantedAt,
                WithdrawnAt = consent.WithdrawnAt,
                IsExpired = IsExpired(consent, now),
                Status = consent.Status
            };
        }

        // Withdraw one own Consent (access stops at once; an erasure request is queued when nothing else keeps the attribute shared)
        public async Task<bool> WithdrawAsync(Guid StudentId, Guid ConsentId)
        {
            var consent = await _consentRepository.GetByIdAsyncInclude(ConsentId, c => c.ConsentPurpose, c => c.DataAttribute);
            if (consent == null || consent.StudentId != StudentId)
                throw new NotFoundException("Consent not found.");
            if (consent.Status != "granted")
                throw new ConflictException("This consent is already withdrawn.");

            WithdrawConsent(consent, StudentId, DateTime.UtcNow);
            await _consentRepository.SaveAsync();
            await QueueErasureAsync(new List<Consent> { consent });

            await _activityLogService.LogCurrentAccountAsync("consentWithdrawn",
                $"Withdrew consent for {consent.ConsentPurpose.PurposeName}: {consent.DataAttribute.AttributeName}");
            return true;
        }

        // Withdraw every own granted Consent of one purpose; returns how many were withdrawn
        public async Task<int> WithdrawPurposeAsync(Guid StudentId, Guid ConsentPurposeId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsync(ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");
            var consents = await _consentRepository.SearchAsyncInclude(c =>
                c.StudentId == StudentId &&
                c.ConsentPurposeId == ConsentPurposeId &&
                c.Status == "granted",
                c => c.DataAttribute);
            if (consents.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            foreach (var consent in consents)
                WithdrawConsent(consent, StudentId, now);
            await _consentRepository.SaveAsync();
            await QueueErasureAsync(consents);

            await _activityLogService.LogCurrentAccountAsync("consentWithdrawn",
                $"Withdrew consent for {purpose.PurposeName}: {string.Join(", ", consents.Select(c => c.DataAttribute.AttributeName))}");
            return consents.Count;
        }

        // Get the consent page of a Student: every active purpose (and any purpose the student has consents under) with its attributes and the student's state per attribute
        public async Task<List<GetMyConsentResponse>> GetMyConsentsAsync(Guid StudentId)
        {
            var myConsents = await _consentRepository.SearchAsyncInclude(c => c.StudentId == StudentId, c => c.DataAttribute);
            var myPurposeIds = myConsents.Select(c => c.ConsentPurposeId).Distinct().ToList();
            var purposes = await _consentPurposeRepository.SearchAsyncIncludeOrderBy(p =>
                p.Status == "active" || myPurposeIds.Contains(p.ConsentPurposeId),
                p => p.PurposeName,
                true,
                p => p.DataAttributes);
            var now = DateTime.UtcNow;

            return purposes.Select(p =>
            {
                var purposeConsents = myConsents.Where(c => c.ConsentPurposeId == p.ConsentPurposeId).ToList();
                var attributes = p.DataAttributes
                    .Where(a => a.Status == "active" || purposeConsents.Any(c => c.DataAttributeId == a.DataAttributeId))
                    .Concat(purposeConsents.Select(c => c.DataAttribute))
                    .GroupBy(a => a.DataAttributeId)
                    .Select(g => g.First())
                    .OrderBy(a => a.AttributeName);

                return new GetMyConsentResponse
                {
                    ConsentPurposeId = p.ConsentPurposeId,
                    Description = p.Description ?? string.Empty,
                    PurposeName = p.PurposeName,
                    Attributes = attributes.Select(a =>
                    {
                        var latest = purposeConsents
                            .Where(c => c.DataAttributeId == a.DataAttributeId)
                            .OrderByDescending(c => c.Status == "granted")
                            .ThenByDescending(c => c.GrantedAt)
                            .FirstOrDefault();
                        return new GetMyConsentAttributeResponse
                        {
                            DataAttributeId = a.DataAttributeId,
                            ConsentId = latest?.ConsentId,
                            AttributeDescription = a.AttributeDescription ?? string.Empty,
                            AttributeName = a.AttributeName,
                            ExpiresAt = latest?.ExpiresAt,
                            GrantedAt = latest?.GrantedAt,
                            AttributeStatus = a.Status,
                            Status = latest == null ? "none" : IsExpired(latest, now) ? "expired" : latest.Status
                        };
                    }).ToList(),
                    PurposeStatus = p.Status
                };
            }).ToList();
        }

        // Get all Consent histories (CONSENTHISTORIES, insert-only)
        public async Task<IEnumerable<GetAllConsentHistoryResponse>> GetAllConsentHistoryAsync()
        {
            var histories = await GetHistoriesAsync(h => true);

            return histories.Select(h => new GetAllConsentHistoryResponse
            {
                ConsentHistoryId = h.ConsentHistoryId,
                ChangedBy = h.ChangedBy,
                ConsentId = h.ConsentId,
                AttributeName = h.Consent.DataAttribute.AttributeName,
                ChangedByFullName = h.ChangedByNavigation.FullName,
                PurposeName = h.Consent.ConsentPurpose.PurposeName,
                ChangedAt = h.ChangedAt,
                NewStatus = h.NewStatus,
                PreviousStatus = h.PreviousStatus ?? string.Empty
            });
        }

        // Get all Consent histories (CONSENTHISTORIES) with pagination
        public async Task<IEnumerable<GetAllConsentHistoryWithPaginationResponse>> GetAllConsentHistoryWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var histories = await GetHistoriesAsync(h => true);

            return histories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new GetAllConsentHistoryWithPaginationResponse
                {
                    ConsentHistoryId = h.ConsentHistoryId,
                    ChangedBy = h.ChangedBy,
                    ConsentId = h.ConsentId,
                    AttributeName = h.Consent.DataAttribute.AttributeName,
                    ChangedByFullName = h.ChangedByNavigation.FullName,
                    PurposeName = h.Consent.ConsentPurpose.PurposeName,
                    ChangedAt = h.ChangedAt,
                    NewStatus = h.NewStatus,
                    PreviousStatus = h.PreviousStatus ?? string.Empty
                });
        }

        // Get Consent history (CONSENTHISTORIES) by Id
        public async Task<GetConsentHistoryByIdResponse> GetConsentHistoryByIdAsync(Guid ConsentHistoryId)
        {
            var history = (await GetHistoriesAsync(h => h.ConsentHistoryId == ConsentHistoryId)).FirstOrDefault()
                ?? throw new NotFoundException("Consent history not found.");

            return new GetConsentHistoryByIdResponse
            {
                ConsentHistoryId = history.ConsentHistoryId,
                ChangedBy = history.ChangedBy,
                ConsentId = history.ConsentId,
                AttributeName = history.Consent.DataAttribute.AttributeName,
                ChangedByFullName = history.ChangedByNavigation.FullName,
                PurposeName = history.Consent.ConsentPurpose.PurposeName,
                ChangedAt = history.ChangedAt,
                NewStatus = history.NewStatus,
                PreviousStatus = history.PreviousStatus ?? string.Empty
            };
        }

        // Search Consent histories (CONSENTHISTORIES) by who changed it, consent or student
        public async Task<List<SearchConsentHistoryResponse>> SearchConsentHistoryAsync(SearchConsentHistoryRequest request)
        {
            var histories = await GetHistoriesAsync(h =>
                (request.ChangedBy == null || h.ChangedBy == request.ChangedBy) &&
                (request.ConsentId == null || h.ConsentId == request.ConsentId) &&
                (request.StudentId == null || h.Consent.StudentId == request.StudentId));

            return histories.Select(h => new SearchConsentHistoryResponse
            {
                ConsentHistoryId = h.ConsentHistoryId,
                ChangedBy = h.ChangedBy,
                ConsentId = h.ConsentId,
                AttributeName = h.Consent.DataAttribute.AttributeName,
                ChangedByFullName = h.ChangedByNavigation.FullName,
                PurposeName = h.Consent.ConsentPurpose.PurposeName,
                ChangedAt = h.ChangedAt,
                NewStatus = h.NewStatus,
                PreviousStatus = h.PreviousStatus ?? string.Empty
            }).ToList();
        }

        // Get the consent change history of a Student, newest first
        public async Task<List<GetMyConsentHistoryResponse>> GetMyConsentHistoryAsync(Guid StudentId)
        {
            var histories = await GetHistoriesAsync(h => h.Consent.StudentId == StudentId);

            return histories.Select(h => new GetMyConsentHistoryResponse
            {
                ConsentHistoryId = h.ConsentHistoryId,
                ChangedBy = h.ChangedBy,
                ConsentId = h.ConsentId,
                AttributeName = h.Consent.DataAttribute.AttributeName,
                ChangedByFullName = h.ChangedByNavigation.FullName,
                PurposeName = h.Consent.ConsentPurpose.PurposeName,
                ChangedAt = h.ChangedAt,
                NewStatus = h.NewStatus,
                PreviousStatus = h.PreviousStatus ?? string.Empty
            }).ToList();
        }

        // Helper: a granted consent whose expiry date has passed no longer gives access
        private static bool IsExpired(Consent consent, DateTime now)
        {
            return consent.Status == "granted" && consent.ExpiresAt != null && consent.ExpiresAt <= now;
        }

        // Helper: new Consent history row (insert-only)
        private static Consenthistory NewHistory(Guid ConsentId, Guid ChangedBy, string? previousStatus, string newStatus, DateTime changedAt)
        {
            return new Consenthistory
            {
                ConsentHistoryId = Guid.NewGuid(),
                ChangedBy = ChangedBy,
                ConsentId = ConsentId,
                ChangedAt = changedAt,
                NewStatus = newStatus,
                PreviousStatus = previousStatus
            };
        }

        // Helper: mark a consent withdrawn and record the change (saved by the caller)
        private void WithdrawConsent(Consent consent, Guid ChangedBy, DateTime now)
        {
            consent.Status = "withdrawn";
            consent.WithdrawnAt = now;
            _consentHistoryRepository.PrepareCreate(NewHistory(consent.ConsentId, ChangedBy, "granted", "withdrawn", now));
        }

        // Helper: queue an erasure request for each withdrawn consent whose attribute is no longer granted under any purpose
        // and whose values are protected by an active key; every Steward is notified (the key is per student per attribute,
        // so erasing it while another purpose still has the attribute granted would wipe data that is still shared)
        private async Task QueueErasureAsync(List<Consent> withdrawnConsents)
        {
            var queued = 0;
            foreach (var consent in withdrawnConsents.GroupBy(c => new { c.StudentId, c.DataAttributeId }).Select(g => g.First()))
            {
                if (await _consentRepository.ExistsAsync(c =>
                        c.StudentId == consent.StudentId &&
                        c.DataAttributeId == consent.DataAttributeId &&
                        c.Status == "granted"))
                    continue;

                var key = await _attributeKeyRepository.FirstOrDefaultAsync(k =>
                    k.StudentId == consent.StudentId &&
                    k.DataAttributeId == consent.DataAttributeId &&
                    k.Status == "active");
                if (key == null || await _erasureRequestRepository.ExistsAsync(e => e.AttributeKeyId == key.AttributeKeyId && e.Status == "pending"))
                    continue;

                _erasureRequestRepository.PrepareCreate(new Erasurerequest
                {
                    ErasureRequestId = Guid.NewGuid(),
                    AttributeKeyId = key.AttributeKeyId,
                    ConsentId = consent.ConsentId,
                    RequestedAt = DateTime.UtcNow,
                    Status = "pending"
                });
                queued++;
            }
            if (queued == 0)
                return;

            await _erasureRequestRepository.SaveAsync();

            var stewards = await _accountRoleRepository.SearchAsync(ar => ar.Role.RoleName == "Data Protection Steward" && ar.Account.Status == "active");
            foreach (var steward in stewards)
            {
                await _notificationService.NotifyAsync(steward.AccountId, "Erasure request queued",
                    $"{queued} withdrawn attribute(s) are waiting for verification and erasure.", "/erasure-requests");
            }
        }

        // Helper: load Consents with their purpose, attribute and student, newest first
        private async Task<List<Consent>> GetConsentsAsync(Expression<Func<Consent, bool>> predicate)
        {
            return await _consentRepository.SearchAsyncIncludeOrderBy(predicate, c => c.GrantedAt, false,
                c => c.ConsentPurpose, c => c.DataAttribute, c => c.Student);
        }

        // Helper: load Consent histories with the consent's purpose and attribute and who changed it, newest first
        private async Task<List<Consenthistory>> GetHistoriesAsync(Expression<Func<Consenthistory, bool>> predicate)
        {
            return await _consentHistoryRepository.SearchAsyncIncludeOrderBy(predicate, h => h.ChangedAt, false,
                h => h.ChangedByNavigation, h => h.Consent.ConsentPurpose, h => h.Consent.DataAttribute);
        }
    }
}
