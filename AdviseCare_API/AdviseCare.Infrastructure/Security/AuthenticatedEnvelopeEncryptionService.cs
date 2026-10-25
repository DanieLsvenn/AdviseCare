using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Domain.Models;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AdviseCare.Infrastructure.Security;

public sealed class AuthenticatedEnvelopeEncryptionService : IAuthenticatedEnvelopeEncryptionService
{
    private const int DataKeyLengthBytes = 32;
    private const int NonceLengthBytes = 12;
    private const int TagLengthBytes = 16;
    private const int HmacLengthBytes = 32;
    private const string ActiveKeyStatus = "active";
    private static readonly byte[] HmacDerivationInfo = Encoding.UTF8.GetBytes("AdviseCare/value-hmac/v1");
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HashSet<string> SupportedFields = new(StringComparer.Ordinal)
    {
        "indicatorValue",
        "currentValue",
        "proposedValue"
    };

    private readonly IKeyManagementService _keyManagementService;
    private readonly VaultTransitSettings _settings;

    public AuthenticatedEnvelopeEncryptionService(
        IKeyManagementService keyManagementService,
        IOptions<VaultTransitSettings> settings)
    {
        _keyManagementService = keyManagementService;
        _settings = settings.Value;
    }

    public async Task<ProvisionedEncryptedValue> ProvisionAndEncryptAsync(
        Guid studentId,
        Guid dataAttributeId,
        Guid payloadId,
        string fieldName,
        string plaintext,
        CancellationToken cancellationToken = default)
    {
        ValidatePayload(studentId, dataAttributeId, payloadId, fieldName, plaintext);

        var attributeKeyId = Guid.NewGuid();
        var keyManagementKeyId = $"{_settings.KeyNamePrefix}{attributeKeyId:N}";
        var dataKey = RandomNumberGenerator.GetBytes(DataKeyLengthBytes);
        var keyCreated = false;

        try
        {
            await _keyManagementService.CreateKeyAsync(keyManagementKeyId, cancellationToken);
            keyCreated = true;
            await _keyManagementService.EnsureKeyCanBeDestroyedAsync(
                keyManagementKeyId,
                cancellationToken);

            var wrappedKey = await _keyManagementService.WrapDataKeyAsync(
                keyManagementKeyId,
                dataKey,
                cancellationToken);

            var encryptedValue = EncryptWithDataKey(
                dataKey,
                attributeKeyId,
                studentId,
                dataAttributeId,
                payloadId,
                fieldName,
                plaintext);

            var attributeKey = new Attributekey
            {
                AttributeKeyId = attributeKeyId,
                StudentId = studentId,
                DataAttributeId = dataAttributeId,
                WrappedKey = wrappedKey,
                KeyManagementKeyId = keyManagementKeyId,
                CreatedAt = DateTime.UtcNow,
                Status = ActiveKeyStatus
            };

            return new ProvisionedEncryptedValue(
                attributeKey,
                encryptedValue.Ciphertext,
                encryptedValue.Hmac);
        }
        catch (Exception provisioningError)
        {
            if (keyCreated)
            {
                try
                {
                    await _keyManagementService.DestroyKeyAsync(
                        keyManagementKeyId,
                        CancellationToken.None);
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Vault key provisioning failed and the newly created Vault key could not be cleaned up.",
                        provisioningError,
                        cleanupError);
                }
            }

            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    public async Task<EncryptedValue> EncryptAsync(
        Attributekey attributeKey,
        Guid payloadId,
        string fieldName,
        string plaintext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attributeKey);
        ValidatePayload(attributeKey.StudentId, attributeKey.DataAttributeId, payloadId, fieldName, plaintext);

        if (attributeKey.Status != ActiveKeyStatus ||
            string.IsNullOrWhiteSpace(attributeKey.WrappedKey) ||
            string.IsNullOrWhiteSpace(attributeKey.KeyManagementKeyId))
        {
            throw new InvalidOperationException("Only an active attribute key with wrapped key material can encrypt values.");
        }

        var dataKey = await _keyManagementService.UnwrapDataKeyAsync(
            attributeKey.KeyManagementKeyId,
            attributeKey.WrappedKey,
            cancellationToken);
        try
        {
            return EncryptWithDataKey(
                dataKey,
                attributeKey.AttributeKeyId,
                attributeKey.StudentId,
                attributeKey.DataAttributeId,
                payloadId,
                fieldName,
                plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    public async Task<string> DecryptAsync(
        Attributekey attributeKey,
        Guid payloadId,
        string fieldName,
        ReadOnlyMemory<byte> ciphertext,
        ReadOnlyMemory<byte> expectedHmac,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attributeKey);
        ValidatePayload(attributeKey.StudentId, attributeKey.DataAttributeId, payloadId, fieldName, string.Empty);

        if (attributeKey.Status != ActiveKeyStatus ||
            string.IsNullOrWhiteSpace(attributeKey.WrappedKey) ||
            string.IsNullOrWhiteSpace(attributeKey.KeyManagementKeyId))
        {
            throw new InvalidOperationException("Only an active attribute key with wrapped key material can decrypt values.");
        }

        if (ciphertext.Length < NonceLengthBytes + TagLengthBytes)
        {
            throw new InvalidDataException("The encrypted payload is shorter than the AES-GCM nonce and tag.");
        }

        if (expectedHmac.Length != HmacLengthBytes)
        {
            throw new InvalidDataException("The stored value HMAC must be 256 bits.");
        }

        var dataKey = await _keyManagementService.UnwrapDataKeyAsync(
            attributeKey.KeyManagementKeyId,
            attributeKey.WrappedKey,
            cancellationToken);
        try
        {
            return DecryptWithDataKey(
                dataKey,
                attributeKey,
                payloadId,
                fieldName,
                ciphertext.Span,
                expectedHmac.Span);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    private static string DecryptWithDataKey(
        byte[] dataKey,
        Attributekey attributeKey,
        Guid payloadId,
        string fieldName,
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> expectedHmac)
    {
        if (dataKey.Length != DataKeyLengthBytes)
        {
            throw new CryptographicException("The unwrapped data-encryption key must be 256 bits.");
        }

        var nonce = payload[..NonceLengthBytes].ToArray();
        var tag = payload[^TagLengthBytes..].ToArray();
        var encryptedValueLength = payload.Length - NonceLengthBytes - TagLengthBytes;
        var plaintext = new byte[encryptedValueLength];
        var hmacKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            dataKey,
            DataKeyLengthBytes,
            attributeKey.AttributeKeyId.ToByteArray(),
            HmacDerivationInfo);
        var associatedData = Encoding.UTF8.GetBytes(
            $"AdviseCare:payload:v1:{attributeKey.StudentId:N}:{attributeKey.DataAttributeId:N}:{attributeKey.AttributeKeyId:N}:{payloadId:N}:{fieldName}");

        try
        {
            using (var aes = new AesGcm(dataKey, TagLengthBytes))
            {
                aes.Decrypt(
                    nonce,
                    payload.Slice(NonceLengthBytes, encryptedValueLength),
                    tag,
                    plaintext,
                    associatedData);
            }

            var actualHmac = HMACSHA256.HashData(hmacKey, plaintext);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(actualHmac, expectedHmac))
                {
                    throw new CryptographicException("The decrypted value does not match its stored HMAC.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actualHmac);
            }

            return StrictUtf8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(hmacKey);
            CryptographicOperations.ZeroMemory(associatedData);
        }
    }

    private static EncryptedValue EncryptWithDataKey(
        byte[] dataKey,
        Guid attributeKeyId,
        Guid studentId,
        Guid dataAttributeId,
        Guid payloadId,
        string fieldName,
        string plaintext)
    {
        if (dataKey.Length != DataKeyLengthBytes)
        {
            CryptographicOperations.ZeroMemory(dataKey);
            throw new CryptographicException("The unwrapped data-encryption key must be 256 bits.");
        }

        var normalizedPlaintext = Encoding.UTF8.GetBytes(plaintext.Normalize(NormalizationForm.FormC));
        var nonce = RandomNumberGenerator.GetBytes(NonceLengthBytes);
        var tag = new byte[TagLengthBytes];
        var ciphertext = new byte[NonceLengthBytes + normalizedPlaintext.Length + TagLengthBytes];
        byte[]? hmacKey = null;
        byte[]? associatedData = null;

        try
        {
            hmacKey = HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                dataKey,
                DataKeyLengthBytes,
                attributeKeyId.ToByteArray(),
                HmacDerivationInfo);
            associatedData = Encoding.UTF8.GetBytes(
                $"AdviseCare:payload:v1:{studentId:N}:{dataAttributeId:N}:{attributeKeyId:N}:{payloadId:N}:{fieldName}");
            nonce.CopyTo(ciphertext, 0);

            using (var aes = new AesGcm(dataKey, TagLengthBytes))
            {
                aes.Encrypt(
                    nonce,
                    normalizedPlaintext,
                    ciphertext.AsSpan(NonceLengthBytes, normalizedPlaintext.Length),
                    tag,
                    associatedData);
            }

            tag.CopyTo(ciphertext, NonceLengthBytes + normalizedPlaintext.Length);
            var hmac = HMACSHA256.HashData(hmacKey, normalizedPlaintext);
            return new EncryptedValue(ciphertext, hmac);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(normalizedPlaintext);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            if (hmacKey is not null)
            {
                CryptographicOperations.ZeroMemory(hmacKey);
            }

            if (associatedData is not null)
            {
                CryptographicOperations.ZeroMemory(associatedData);
            }
        }
    }

    private static void ValidatePayload(
        Guid studentId,
        Guid dataAttributeId,
        Guid payloadId,
        string fieldName,
        string plaintext)
    {
        if (studentId == Guid.Empty || dataAttributeId == Guid.Empty || payloadId == Guid.Empty)
        {
            throw new ArgumentException("Student, data attribute, and payload identifiers are required.");
        }

        if (!SupportedFields.Contains(fieldName))
        {
            throw new ArgumentException("The encrypted field is not supported.", nameof(fieldName));
        }

        ArgumentNullException.ThrowIfNull(plaintext);
    }
}
