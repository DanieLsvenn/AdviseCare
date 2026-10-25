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
        // Prefix of every encrypted value; a new version (e.g. after a master key rotation) would get a new prefix
        private const string FormatVersion = "v1";

        private readonly IGenericRepository<Attributekey> _attributeKeyRepository;
        private readonly IDataEncryptor _dataEncryptor;
        // Keys loaded or created during this request (the service is scoped), so new keys staged for one import are reused
        private readonly Dictionary<(Guid StudentId, Guid DataAttributeId), Attributekey> _activeKeys = new();

        public AttributeKeyService(
            IGenericRepository<Attributekey> attributeKeyRepository,
            IDataEncryptor dataEncryptor)
        {
            _attributeKeyRepository = attributeKeyRepository;
            _dataEncryptor = dataEncryptor;
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
                CreatedAt = k.CreatedAt,
                DestroyedAt = k.DestroyedAt,
                Status = k.Status
            }).ToList();
        }

        // Encrypt values for STUDENTINDICATORS.indicatorValue, in the same order; a student's first value for an attribute creates its key.
        // New keys are only staged: they are saved by the caller's SaveAsync together with the encrypted rows, so an import stays all-or-nothing
        public async Task<List<string>> EncryptValuesAsync(List<PlainAttributeValue> values)
        {
            var results = new List<string>();
            foreach (var value in values)
            {
                var key = await GetOrCreateActiveKeyAsync(value.StudentId, value.DataAttributeId);
                var payload = _dataEncryptor.Encrypt(key.WrappedKey, value.Value, AssociatedData(value.StudentId, value.DataAttributeId));
                results.Add($"{FormatVersion}:{key.AttributeKeyId:N}:{payload}");
            }
            return results;
        }

        // Decrypt stored values, in the same order; a value whose key was destroyed by an erasure comes back with IsErased = true.
        // Call this only after the access guard has allowed the read (students reading their own data are always allowed)
        public async Task<List<DecryptedAttributeValue>> DecryptValuesAsync(List<EncryptedAttributeValue> values)
        {
            var parsed = values.Select(v => (Item: v, KeyId: ParseKeyId(v.CipherText), Payload: v.CipherText.Split(':', 3)[2])).ToList();
            var keyIds = parsed.Select(p => p.KeyId).Distinct().ToList();
            var keys = (await _attributeKeyRepository.SearchAsync(k => keyIds.Contains(k.AttributeKeyId)))
                .ToDictionary(k => k.AttributeKeyId);

            return parsed.Select(p =>
            {
                if (!keys.TryGetValue(p.KeyId, out var key) || key.StudentId != p.Item.StudentId || key.DataAttributeId != p.Item.DataAttributeId)
                    throw new ValidationException("The stored value does not belong to this student and data attribute.");

                if (key.Status == "destroyed")
                {
                    return new DecryptedAttributeValue
                    {
                        StudentId = p.Item.StudentId,
                        DataAttributeId = p.Item.DataAttributeId,
                        Value = null,
                        IsErased = true
                    };
                }

                try
                {
                    return new DecryptedAttributeValue
                    {
                        StudentId = p.Item.StudentId,
                        DataAttributeId = p.Item.DataAttributeId,
                        Value = _dataEncryptor.Decrypt(key.WrappedKey, p.Payload, AssociatedData(p.Item.StudentId, p.Item.DataAttributeId)),
                        IsErased = false
                    };
                }
                catch (CryptographicException)
                {
                    throw new ConflictException("A stored value could not be decrypted (it was changed or the master key is wrong).");
                }
            }).ToList();
        }

        // Helper: the student's active key for the attribute, created (and staged) when there is none yet
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
                    WrappedKey = _dataEncryptor.CreateWrappedKey(),
                    CreatedAt = DateTime.UtcNow,
                    Status = "active"
                };
                _attributeKeyRepository.PrepareCreate(key);
            }

            _activeKeys[(StudentId, DataAttributeId)] = key;
            return key;
        }

        // Helper: read the key id out of "v1:{attributeKeyId}:{payload}"
        private static Guid ParseKeyId(string cipherText)
        {
            var parts = (cipherText ?? string.Empty).Split(':', 3);
            if (parts.Length != 3 || parts[0] != FormatVersion || !Guid.TryParse(parts[1], out var keyId))
                throw new ValidationException("The stored value is not in the encrypted format (v1:{attributeKeyId}:{payload}).");
            return keyId;
        }

        // Helper: ties an encrypted value to its student and data attribute, so it cannot be copied onto another row
        private static string AssociatedData(Guid StudentId, Guid DataAttributeId)
        {
            return $"{StudentId:N}:{DataAttributeId:N}";
        }

        // Helper: load Attribute keys with their attribute and student, newest first
        private async Task<List<Attributekey>> GetKeysAsync(Expression<Func<Attributekey, bool>> predicate)
        {
            return await _attributeKeyRepository.SearchAsyncIncludeOrderBy(predicate, k => k.CreatedAt, false,
                k => k.DataAttribute, k => k.Student);
        }
    }
}
