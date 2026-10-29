using AdviseCare.Application.Contracts.Requests.AttributeKey;
using AdviseCare.Application.Contracts.Responses.AttributeKey;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace AdviseCare.Application.Services
{
    public class AttributeKeyService : IAttributeKeyService
    {
        private readonly IGenericRepository<Attributekey> _attributeKeyRepository;
        private readonly IDataEncryptor _dataEncryptor;
        private readonly IActivityLogService _activityLogService;
        // Keys loaded or created during this request (the service is scoped), so new keys staged for one import are reused
        private readonly Dictionary<(Guid StudentId, Guid DataAttributeId), Attributekey> _activeKeys = new();

        public AttributeKeyService(
            IGenericRepository<Attributekey> attributeKeyRepository,
            IDataEncryptor dataEncryptor,
            IActivityLogService activityLogService)
        {
            _attributeKeyRepository = attributeKeyRepository;
            _dataEncryptor = dataEncryptor;
            _activityLogService = activityLogService;
        }

        // Get all Attribute keys (the key itself is never returned)
        public async Task<IEnumerable<GetAllAttributeKeyResponse>> GetAll()
        {
            var keys = await GetKeysAsync(k => true);

            return keys.Select(k => new GetAllAttributeKeyResponse
            {
                AttributeKeyId = k.AttributeKeyId,
                DataAttributeId = k.DataAttributeId,
                StudentId = k.StudentId,
                AttributeName = k.DataAttribute.AttributeName,
                StudentFullName = k.Student.FullName,
                MasterKeyVersion = k.MasterKeyVersion,
                CreatedAt = k.CreatedAt,
                DestroyedAt = k.DestroyedAt,
                Status = k.Status
            });
        }

        // Get all Attribute keys with pagination (the key itself is never returned)
        public async Task<IEnumerable<GetAllAttributeKeyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var keys = await GetKeysAsync(k => true);

            return keys
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new GetAllAttributeKeyWithPaginationResponse
                {
                    AttributeKeyId = k.AttributeKeyId,
                    DataAttributeId = k.DataAttributeId,
                    StudentId = k.StudentId,
                    AttributeName = k.DataAttribute.AttributeName,
                    StudentFullName = k.Student.FullName,
                    MasterKeyVersion = k.MasterKeyVersion,
                    CreatedAt = k.CreatedAt,
                    DestroyedAt = k.DestroyedAt,
                    Status = k.Status
                });
        }

        // Get Attribute key by Id (the key itself is never returned)
        public async Task<GetAttributeKeyByIdResponse> GetByIdAsync(Guid AttributeKeyId)
        {
            var key = (await GetKeysAsync(k => k.AttributeKeyId == AttributeKeyId)).FirstOrDefault()
                ?? throw new NotFoundException("Attribute key not found.");

            return new GetAttributeKeyByIdResponse
            {
                AttributeKeyId = key.AttributeKeyId,
                DataAttributeId = key.DataAttributeId,
                StudentId = key.StudentId,
                AttributeName = key.DataAttribute.AttributeName,
                StudentFullName = key.Student.FullName,
                MasterKeyVersion = key.MasterKeyVersion,
                CreatedAt = key.CreatedAt,
                DestroyedAt = key.DestroyedAt,
                Status = key.Status
            };
        }

        // Search Attribute keys by attribute, student or status (the key itself is never returned)
        public async Task<List<SearchAttributeKeyResponse>> SearchAsync(SearchAttributeKeyRequest request)
        {
            var keys = await GetKeysAsync(k =>
                (request.DataAttributeId == null || k.DataAttributeId == request.DataAttributeId) &&
                (request.StudentId == null || k.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || k.Status == request.Status));

            return keys.Select(k => new SearchAttributeKeyResponse
            {
                AttributeKeyId = k.AttributeKeyId,
                DataAttributeId = k.DataAttributeId,
                StudentId = k.StudentId,
                AttributeName = k.DataAttribute.AttributeName,
                StudentFullName = k.Student.FullName,
                MasterKeyVersion = k.MasterKeyVersion,
                CreatedAt = k.CreatedAt,
                DestroyedAt = k.DestroyedAt,
                Status = k.Status
            }).ToList();
        }

        // Encrypt values for STUDENTDATA.dataValue or CORRECTIONREQUESTS.currentValue/proposedValue, in the same order; a student's first value for an attribute creates its key.
        // New keys are only staged: they are saved by the caller's SaveAsync together with the encrypted rows, so an import stays all-or-nothing
        public async Task<List<EncryptedAttributeValue>> EncryptValuesAsync(List<PlainAttributeValue> values)
        {
            var results = new List<EncryptedAttributeValue>();
            foreach (var value in values)
            {
                var key = await GetOrCreateActiveKeyAsync(value.StudentId, value.DataAttributeId);
                results.Add(new EncryptedAttributeValue
                {
                    AttributeKeyId = key.AttributeKeyId,
                    StudentId = value.StudentId,
                    DataAttributeId = value.DataAttributeId,
                    RowId = value.RowId,
                    FieldName = value.FieldName,
                    Payload = _dataEncryptor.Encrypt(key.WrappedKey, key.MasterKeyVersion, KeyContext(key), value.Value,
                        AssociatedData(key.AttributeKeyId, value.StudentId, value.DataAttributeId, value.RowId, value.FieldName))
                });
            }
            return results;
        }

        // Decrypt stored values, in the same order; a value cleared by an erasure (or whose key was destroyed) comes back with IsErased = true.
        // Call this only after the access guard has allowed the read (students reading their own data are always allowed)
        public async Task<List<DecryptedAttributeValue>> DecryptValuesAsync(List<EncryptedAttributeValue> values)
        {
            var keyIds = values.Select(v => v.AttributeKeyId).Distinct().ToList();
            var keys = (await _attributeKeyRepository.SearchAsync(k => keyIds.Contains(k.AttributeKeyId)))
                .ToDictionary(k => k.AttributeKeyId);

            return values.Select(v =>
            {
                if (!keys.TryGetValue(v.AttributeKeyId, out var key) || key.StudentId != v.StudentId || key.DataAttributeId != v.DataAttributeId)
                    throw new ConflictException("A stored value does not belong to this student and data attribute.");

                if (key.Status == "destroyed" || v.Payload == null)
                    return new DecryptedAttributeValue { RowId = v.RowId, FieldName = v.FieldName, Value = null, IsErased = true };

                try
                {
                    return new DecryptedAttributeValue
                    {
                        RowId = v.RowId,
                        FieldName = v.FieldName,
                        Value = _dataEncryptor.Decrypt(key.WrappedKey, key.MasterKeyVersion, KeyContext(key), v.Payload,
                            AssociatedData(key.AttributeKeyId, v.StudentId, v.DataAttributeId, v.RowId, v.FieldName)),
                        IsErased = false
                    };
                }
                catch (CryptographicException)
                {
                    throw new ConflictException("A stored value could not be decrypted (it was changed or moved, or its master key version is missing).");
                }
            }).ToList();
        }

        // Rewrap every active Attribute key under the current master key version (the data keys and the stored values do not change);
        // afterwards an old version that no active key uses can be removed from the configuration once no kept database backup needs it
        public async Task<RewrapAttributeKeyResponse> RewrapAsync()
        {
            var current = _dataEncryptor.CurrentMasterKeyVersion;
            var keys = await _attributeKeyRepository.SearchAsync(k => k.Status == "active" && k.MasterKeyVersion != current);
            foreach (var key in keys)
            {
                try
                {
                    key.WrappedKey = _dataEncryptor.RewrapKey(key.WrappedKey, key.MasterKeyVersion, KeyContext(key));
                }
                catch (CryptographicException)
                {
                    throw new ConflictException($"Attribute key {key.AttributeKeyId} could not be rewrapped: master key version {key.MasterKeyVersion} is missing or wrong.");
                }
                key.MasterKeyVersion = current;
            }
            await _attributeKeyRepository.SaveAsync();

            var usedVersions = (await _attributeKeyRepository.SearchAsync(k => k.Status == "active"))
                .Select(k => k.MasterKeyVersion).Distinct().ToList();
            await _activityLogService.LogCurrentAccountAsync("attributeKeysRewrapped", $"Rewrapped {keys.Count} attribute key(s) under master key version {current}");

            return new RewrapAttributeKeyResponse
            {
                CurrentMasterKeyVersion = current,
                RewrappedCount = keys.Count,
                ConfiguredVersions = _dataEncryptor.MasterKeyVersions.OrderBy(v => v).ToList(),
                UnusedVersions = _dataEncryptor.MasterKeyVersions.Where(v => v != current && !usedVersions.Contains(v)).OrderBy(v => v).ToList()
            };
        }

        // Helper: the student's active key for the attribute, created (and staged) under the current master key version when there is none yet
        private async Task<Attributekey> GetOrCreateActiveKeyAsync(Guid StudentId, Guid DataAttributeId)
        {
            if (_activeKeys.TryGetValue((StudentId, DataAttributeId), out var cached))
                return cached;

            var key = await _attributeKeyRepository.FirstOrDefaultAsync(k =>
                k.StudentId == StudentId &&
                k.DataAttributeId == DataAttributeId &&
                k.Status == "active");
            if (key == null)
            {
                key = new Attributekey
                {
                    AttributeKeyId = Guid.NewGuid(),
                    DataAttributeId = DataAttributeId,
                    StudentId = StudentId,
                    MasterKeyVersion = _dataEncryptor.CurrentMasterKeyVersion,
                    CreatedAt = DateTime.UtcNow,
                    Status = "active"
                };
                key.WrappedKey = _dataEncryptor.CreateWrappedKey(KeyContext(key));
                _attributeKeyRepository.PrepareCreate(key);
            }

            _activeKeys[(StudentId, DataAttributeId)] = key;
            return key;
        }

        // Helper: ties a wrapped key to its own ATTRIBUTEKEYS row (key id, student, attribute)
        private static string KeyContext(Attributekey key)
        {
            return $"{key.AttributeKeyId:N}:{key.StudentId:N}:{key.DataAttributeId:N}";
        }

        // Helper: ties an encrypted value to its key, student, attribute, row and field, so it cannot be copied or moved anywhere else
        private static string AssociatedData(Guid AttributeKeyId, Guid StudentId, Guid DataAttributeId, Guid RowId, string FieldName)
        {
            return $"AdviseCare:value:v1:{AttributeKeyId:N}:{StudentId:N}:{DataAttributeId:N}:{RowId:N}:{FieldName}";
        }

        // Helper: load Attribute keys with their attribute and student, newest first
        private async Task<List<Attributekey>> GetKeysAsync(Expression<Func<Attributekey, bool>> predicate)
        {
            return await _attributeKeyRepository.SearchAsyncIncludeOrderBy(predicate, k => k.CreatedAt, false,
                k => k.DataAttribute, k => k.Student);
        }
    }
}
